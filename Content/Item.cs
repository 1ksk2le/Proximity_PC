using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content
{
    public enum DrawSlot
    {
        BelowBody,
        AboveBody,
        BelowHead,
        AboveHead,
        Offhand
    }

    public abstract class Item
    {
        public static bool FreezeGameWorldAnimations { get; set; } = false;
        public static bool IsRenderingPortrait { get; set; } = false;

        private readonly ContentManager contentManager;
        public readonly ParticleManager particle;
        public readonly ProjectileProperties projectile;
        public readonly Random random = new Random(Environment.TickCount + 3);

        public Texture2D Texture { get; protected set; }
        public int ID { get; protected set; }
        public int Rarity { get; protected set; }
        public int Value { get; protected set; }
        public int Damage { get; protected set; }
        public int Defense { get; protected set; }
        public int CurrentStack { get; set; }
        public int MaxStack { get; protected set; }
        public DrawSlot DrawSlot { get; protected set; }
        public float UseTime { get; protected set; }
        public float ShootSpeed { get; set; }
        public float Recoil { get; set; }
        public float Knockback { get; set; }
        public float KnockbackResistance { get; set; }
        public string Name { get; protected set; }
        public string Type { get; protected set; }
        public string Lore { get; protected set; }
        public string Info { get; protected set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public float SpeedBonus { get; protected set; }
        public float HealthBonus { get; protected set; }
        public float DamageBonus { get; protected set; }
        public float DefenseBonus { get; protected set; }
        public float KnockbackBonus { get; protected set; }
        public float KnockbackResistanceBonus { get; protected set; }
        public bool IsStackable { get; protected set; }

        protected Item(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            this.particle = particleManager ?? throw new ArgumentNullException(nameof(particleManager));
            this.projectile = projectileProperties ?? throw new ArgumentNullException(nameof(projectileProperties));

            Initialize();
            LoadDrawSlots();
            LoadTexture();
        }

        protected abstract void Initialize();

        private void LoadDrawSlots()
        {
            if (Type.Contains("[Weapon "))
                DrawSlot = DrawSlot.BelowBody;
            if (Type.Contains("[Offhand]"))
                DrawSlot = DrawSlot.Offhand;
            if (Type.Contains("[Chestplate]"))
                DrawSlot = DrawSlot.AboveBody;
            if (Type.Contains("[Helmet]"))
                DrawSlot = DrawSlot.AboveHead;
        }

        private void LoadTexture()
        {
            try
            {
                string texturePath = $"Textures/Items/t_Item_{ID}";
                Texture = contentManager.Load<Texture2D>(texturePath);
            }
            catch (Exception ex)
            {
                // Log texture loading failure for debugging
                System.Diagnostics.Debug.WriteLine("Failed to load texture for item " + ID + ": " + ex.Message);
            }
        }

        public string GetName()
        {
            if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
                return Name;

            return $"{Prefix} {Name} {Suffix}".Trim();
        }

        public virtual void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
        }

        public virtual void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
        }

        public virtual void Use(float deltaTime, Player player, Vector2 direction)
        {
            if (player.IsKnocked) return;
        }

        public virtual void Update(float deltaTime, GameTime gameTime, Player player)
        {
            if (player == null || player.IsKnocked || player.WeaponHitbox == Rectangle.Empty)
                return;
        }

        public static readonly (string Name, Color Color)[] Rarities = new (string, Color)[]
        {
            ("Rubbish", Color.Silver),
            ("Common", Color.White),
            ("Uncommon", Color.PaleGreen),
            ("Rare", Color.CornflowerBlue),
            ("Epic", Color.MediumOrchid),
            ("Legendary", Color.Coral),
            ("Mythical", Color.Turquoise),
        };

        public static (string Name, Color Color) GetRarityInfo(int rarity)
        {
            if (rarity < 0 || rarity >= Rarities.Length)
                return ("Unknown", Color.White);
            return Rarities[rarity];
        }

        public virtual void UpdateHitboxes(Player player, GameTime gameTime)
        {
            if (FreezeGameWorldAnimations && !IsRenderingPortrait)
                return;

            if (Type.StartsWith("[Weapon"))
            {
                var (weaponHitbox, weaponRotation) = CalculateWeaponHitbox(player, gameTime);
                player.WeaponHitbox = weaponHitbox;
                player.WeaponHitboxRotation = weaponRotation;
            }
            else if (Type.StartsWith("[Offhand"))
            {
                player.OffhandHitbox = CalculateOffhandHitbox(player, gameTime);
            }
        }

        protected virtual (Rectangle hitbox, float rotation) CalculateWeaponHitbox(Player player, GameTime gameTime, float windUpRatio = 0.3f)
        {
            if (player.IsKnocked)
            {
                return (Rectangle.Empty, 0f);
            }

            if (Type == "[Weapon - Staff]")
            {
                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * jumpProgress) : 0f;
                Vector2 drawPosition = player.Position + new Vector2(0, jumpOffset);

                float time = ((FreezeGameWorldAnimations && !IsRenderingPortrait) || (Main.Paused && !IsRenderingPortrait)) ? 0f : (float)gameTime.TotalGameTime.TotalSeconds;

                float sideOffset = (player.T_Body.Width / 2f + Texture.Width / 2) * (player.IsFacingLeft ? -1f : 1f);
                float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;
                float jumpScale = player.IsJumping ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress) : Player.BASE_SCALE;
                float jumpRaise = player.IsJumping ? Texture.Height / 1.75f * (float)Math.Sin(Math.PI * jumpProgress) : 0f;

                if (player.IsAttacking)
                {
                    float figure8X = (float)Math.Sin(time * 6f) * 12f;
                    float figure8Y = (float)Math.Sin(time * 12f) * 6f;
                    float orbitalX = (float)Math.Cos(time * 3.6f) * 6f;
                    float orbitalY = (float)Math.Sin(time * 3.6f) * 4f;
                    float finalRotation = (float)Math.Sin(time * 6f) * 0.3f +
                                        (float)Math.Sin(time * 18f) * 0.1f;

                    Vector2 basePosition = drawPosition + new Vector2(
                        sideOffset + figure8X + orbitalX,
                        -walkBobOffset + figure8Y + orbitalY + (Texture.Height * 0.25f) - jumpRaise
                    );

                    float staffWidth = Texture.Width * jumpScale;
                    float staffHeight = Texture.Height * jumpScale;

                    Matrix rotationMatrix = Matrix.CreateRotationZ(finalRotation);
                    Vector2 hitboxOrigin = new Vector2(0, -staffHeight / 2f);
                    Vector2 rotatedOffset = Vector2.Transform(hitboxOrigin, rotationMatrix);
                    Vector2 hitboxPosition = basePosition + rotatedOffset;

                    return (new Rectangle(
                        (int)(hitboxPosition.X - staffWidth / 2f),
                        (int)(hitboxPosition.Y - staffHeight / 2f),
                        (int)staffWidth,
                        (int)staffHeight
                    ), finalRotation);
                }
                else
                {
                    float figure8X = (float)Math.Sin(time * 4f) * 6f;
                    float figure8Y = (float)Math.Sin(time * 8f) * 3f;
                    float orbitalX = (float)Math.Cos(time * 2.4f) * 3f;
                    float orbitalY = (float)Math.Sin(time * 2.4f) * 2f;
                    float finalRotation = (float)Math.Sin(time * 4f) * 0.15f +
                                        (float)Math.Sin(time * 12f) * 0.05f;

                    Vector2 basePosition = drawPosition + new Vector2(
                        sideOffset + figure8X + orbitalX,
                        -walkBobOffset + figure8Y + orbitalY + (Texture.Height * 0.25f) - jumpRaise
                    );

                    if (drawPosition == Vector2.Zero) return (Rectangle.Empty, 0f);

                    float staffWidth = Texture.Width * jumpScale;
                    float staffHeight = Texture.Height * jumpScale;

                    Matrix rotationMatrix = Matrix.CreateRotationZ(finalRotation);
                    Vector2 hitboxOrigin = new Vector2(0, -staffHeight / 2f);
                    Vector2 rotatedOffset = Vector2.Transform(hitboxOrigin, rotationMatrix);
                    Vector2 hitboxPosition = basePosition + rotatedOffset;

                    return (new Rectangle(
                        (int)(hitboxPosition.X - staffWidth / 2f),
                        (int)(hitboxPosition.Y - staffHeight / 2f),
                        (int)staffWidth,
                        (int)staffHeight
                    ), finalRotation);
                }
            }
            else if (Type == "[Weapon - Sword]")
            {
                float weaponJumpOffset = player.IsJumping ? -90f * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                Vector2 drawPosition = player.Position + new Vector2(0, weaponJumpOffset);

                if (player.IsAttacking)
                {
                    float attackProgress = 1 - (player.AttackTimer / UseTime);
                    float swingOffset;

                    if (attackProgress < windUpRatio)
                    {
                        float windUpProgress = attackProgress / windUpRatio;
                        swingOffset = -MathHelper.ToRadians(ShootSpeed / 2) * (1 - (float)Math.Cos(windUpProgress * MathHelper.PiOver2));
                    }
                    else
                    {
                        float swingProgress = (attackProgress - windUpRatio) / (1 - windUpRatio);
                        swingOffset = MathHelper.ToRadians(ShootSpeed) * (float)Math.Sin(swingProgress * MathHelper.PiOver2);
                    }

                    float baseAngle = (float)Math.Atan2(player.AttackDirection.Y, player.AttackDirection.X);
                    float finalAngle = baseAngle + (player.IsFacingLeft ? -swingOffset : swingOffset);
                    float swingRadius = Texture.Height / 2;
                    Vector2 weaponOffset = new Vector2((float)Math.Cos(finalAngle), (float)Math.Sin(finalAngle)) * swingRadius;
                    Vector2 itemPosition = drawPosition + weaponOffset;

                    float scaleEffect = 1f;
                    if (attackProgress < windUpRatio)
                    {
                        float windUpProgress = attackProgress / windUpRatio;
                        scaleEffect = MathHelper.Lerp(0.85f, 1.2f, windUpProgress);
                    }
                    else
                    {
                        float windDownProgress = (attackProgress - windUpRatio) / (1f - windUpRatio);
                        scaleEffect = MathHelper.Lerp(1.2f, 0.85f, windDownProgress);
                    }

                    float weaponHitboxWidth = Texture.Width * Player.BASE_SCALE;
                    float weaponHitboxLength = Texture.Height * Player.BASE_SCALE;

                    Matrix rotationMatrix = Matrix.CreateRotationZ(finalAngle);
                    Vector2 hitboxOrigin = new Vector2(0, -weaponHitboxLength / (player.IsFacingLeft ? -2f : 2f));
                    Vector2 rotatedOffset = Vector2.Transform(hitboxOrigin, rotationMatrix);
                    Vector2 weaponCenter = itemPosition + rotatedOffset;

                    return (new Rectangle(
                        (int)(weaponCenter.X - weaponHitboxWidth / 2f),
                        (int)(weaponCenter.Y - weaponHitboxLength / 2f),
                        (int)weaponHitboxWidth,
                        (int)weaponHitboxLength
                    ), finalAngle);
                }
                else if (!player.IsKnocked)
                {
                    float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                    float jumpScale = player.IsJumping ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress) : Player.BASE_SCALE;

                    float weaponWalkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;
                    float sideOffset = (player.T_Body.Width / 2f + 5f) * (player.IsFacingLeft ? -1f : 1f);
                    float walkBobAmount = player.IsMoving ? (float)Math.Sin(player.WalkTimer * MathHelper.TwoPi * 2) * 12f : 0f;
                    Vector2 weaponOffset = new Vector2(sideOffset, -weaponWalkBobOffset + 15 + walkBobAmount);
                    Vector2 itemPosition = drawPosition + weaponOffset;

                    // Use the same angle as the animation!
                    float idleAngle = player.IsFacingLeft ? MathHelper.ToRadians(15) : MathHelper.ToRadians(-15);
                    float jumpAngle = player.IsFacingLeft ? MathHelper.ToRadians(-30) : MathHelper.ToRadians(30);
                    float targetAngle = player.IsJumping ? jumpAngle : idleAngle;
                    float lerpSpeed = 5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if ((!FreezeGameWorldAnimations && !Main.Paused) || IsRenderingPortrait)
                        currentSwordAngle = MathHelper.Lerp(currentSwordAngle, targetAngle, lerpSpeed);

                    float idleWave = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.2f;
                    float walkTilt = player.IsMoving ? (float)Math.Sin(player.WalkTimer * MathHelper.TwoPi * 2) * 0.7f : 0f;
                    float angle = currentSwordAngle + idleWave + walkTilt;

                    float weaponHitboxWidth = Texture.Width * jumpScale;
                    float weaponHitboxLength = Texture.Height * jumpScale;
                    Matrix rotationMatrix = Matrix.CreateRotationZ(angle);
                    Vector2 hitboxOrigin = new Vector2(0, -weaponHitboxLength / 2f);
                    Vector2 rotatedOffset = Vector2.Transform(hitboxOrigin, rotationMatrix);
                    Vector2 weaponCenter = itemPosition + rotatedOffset;

                    return (new Rectangle(
                        (int)(weaponCenter.X - weaponHitboxWidth / 2f),
                        (int)(weaponCenter.Y - weaponHitboxLength / 2f),
                        (int)weaponHitboxWidth,
                        (int)weaponHitboxLength
                    ), angle);
                }
            }
            else if (Type == "[Weapon - Rifle]" || Type == "[Weapon - Pistol]")
            {
                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;

                float offsetX = (player.T_Body.Width * (Type == "[Weapon - Rifle]" ? 0.2f : 0.5f)) * (player.IsFacingLeft ? -1f : 1f);
                float offsetY = player.T_Body.Height * 0.3f - Texture.Height * 0.5f;
                Vector2 offset = new Vector2(offsetX, offsetY);

                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping
                    ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress)
                    : Player.BASE_SCALE;

                float rifleMoveX = 0f;
                float attackProgress = 0f;
                if (player.IsAttacking)
                {
                    float animationDuration = UseTime * 0.95f;
                    float elapsed = UseTime - player.AttackTimer;
                    attackProgress = MathHelper.Clamp(elapsed / animationDuration, 0f, 1f);

                    if (Type == "[Weapon - Rifle]")
                    {
                        rifleMoveX = -Texture.Width / 5f * attackProgress * (player.IsFacingLeft ? -1f : 1f);
                    }
                }

                Vector2 drawPos = player.Position + offset * jumpScale + new Vector2(rifleMoveX, jumpOffset - walkBobOffset);

                float angle;
                if (player.IsAttacking)
                {
                    float baseAngle = (float)Math.Atan2(player.AttackDirection.Y, player.AttackDirection.X);
                    float recoil = Recoil * (float)Math.Sin(attackProgress * MathHelper.Pi);
                    angle = player.IsFacingLeft ? MathHelper.Pi + baseAngle + recoil : baseAngle - recoil;
                }
                else
                {
                    float breath = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2f) * 0.20f;
                    float targetAngle = MathHelper.ToRadians(30);
                    if (player.IsMoving || player.IsJumping)
                        targetAngle = MathHelper.ToRadians(-60);
                    float lerpSpeed = 5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if ((!FreezeGameWorldAnimations && !Main.Paused) || IsRenderingPortrait)
                        currentGunAngle = MathHelper.Lerp(currentGunAngle, targetAngle, lerpSpeed);
                    angle = player.IsFacingLeft ? -currentGunAngle + breath : currentGunAngle + breath;
                }

                Vector2 origin = new Vector2(player.IsFacingLeft ? Texture.Width : 0, Texture.Height / 2f);

                float gunLength = Texture.Width * jumpScale;
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (player.IsFacingLeft ? -1f : 1f);
                Vector2 barrelTip = drawPos + direction * gunLength;

                int hitboxWidth = (int)(Texture.Width * jumpScale);
                int hitboxHeight = (int)(Texture.Height * jumpScale);

                Vector2 hitboxCenter = barrelTip - direction * (hitboxWidth / 2f);

                Rectangle hitbox = new Rectangle(
                    (int)(hitboxCenter.X - hitboxWidth / 2f),
                    (int)(hitboxCenter.Y - hitboxHeight / 2f),
                    hitboxWidth,
                    hitboxHeight
                );

                return (hitbox, angle);
            }
            return (Rectangle.Empty, 0f);
        }

        protected virtual Rectangle CalculateOffhandHitbox(Player player, GameTime gameTime)
        {
            if (player.IsKnocked)
            {
                return Rectangle.Empty;
            }

            float offhandJumpOffset = player.IsJumping ? -90f * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
            float offhandWalkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT * 1.5f : 0f;
            Vector2 itemPosition = player.Position + new Vector2(-Texture.Width / 2 * (player.IsFacingLeft ? -1 : 1), -offhandWalkBobOffset + Texture.Height / 2 + offhandJumpOffset);

            return new Rectangle(
                (int)(itemPosition.X - Texture.Width / 2),
                (int)(itemPosition.Y - Texture.Height / 2),
                Texture.Width,
                Texture.Height
            );
        }

        protected void DrawSwordAttack(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer, float windUpRatio = 0.3f)
        {
            if (player.IsAttacking)
            {
                var pos = player.Position;
                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                Vector2 drawPosition = pos + new Vector2(0, jumpOffset);

                float attackProgress = 1 - (player.AttackTimer / UseTime);
                float swingOffset;
                if (attackProgress < windUpRatio)
                {
                    float windUpProgress = attackProgress / windUpRatio;
                    swingOffset = -MathHelper.ToRadians(ShootSpeed / 2) * (1 - (float)Math.Cos(windUpProgress * MathHelper.PiOver2));
                }
                else
                {
                    float swingProgress = (attackProgress - windUpRatio) / (1 - windUpRatio);
                    swingOffset = MathHelper.ToRadians(ShootSpeed) * (float)Math.Sin(swingProgress * MathHelper.PiOver2);
                }

                float baseAngle = (float)Math.Atan2(player.AttackDirection.Y, player.AttackDirection.X);
                float finalAngle = baseAngle + (player.IsFacingLeft ? -swingOffset : swingOffset);
                float swingRadius = Texture.Height / 2;
                Vector2 weaponOffset = new Vector2((float)Math.Cos(finalAngle), (float)Math.Sin(finalAngle)) * swingRadius;
                Vector2 itemPosition = drawPosition + weaponOffset;
                Vector2 origin = new Vector2(Texture.Width / 2f, player.IsFacingLeft ? 0 : Texture.Height);
                float scaleEffect = 1f;
                if (attackProgress < windUpRatio)
                {
                    float windUpProgress = attackProgress / windUpRatio;
                    scaleEffect = MathHelper.Lerp(0.85f, 1.2f, windUpProgress);
                }
                else
                {
                    float windDownProgress = (attackProgress - windUpRatio) / (1f - windUpRatio);
                    scaleEffect = MathHelper.Lerp(1.2f, 0.85f, windDownProgress);
                }

                spriteBatch.Draw(Texture, itemPosition, null, Color.White, finalAngle, origin, Player.BASE_SCALE * scaleEffect,
                player.IsFacingLeft ? SpriteEffects.FlipVertically : SpriteEffects.None, drawLayer);
            }
        }

        protected float currentSwordAngle = MathHelper.ToRadians(15);

        protected void DrawSwordIdle(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            if (!player.IsAttacking && !player.IsKnocked)
            {
                var pos = player.Position;
                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                Vector2 drawPosition = pos + new Vector2(0, jumpOffset);

                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress) : Player.BASE_SCALE;

                float weaponWalkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;
                float sideOffset = (player.T_Body.Width / 2f + 5f) * (player.IsFacingLeft ? -1f : 1f);
                float walkBobAmount = player.IsMoving ? (float)Math.Sin(player.WalkTimer * MathHelper.TwoPi * 2) * 12f : 0f;
                Vector2 weaponOffset = new Vector2(sideOffset, -weaponWalkBobOffset + 15 + walkBobAmount);
                Vector2 itemPosition = drawPosition + weaponOffset;

                Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height);

                // Smoothly transition between idle and jump angles
                float idleAngle = player.IsFacingLeft ? MathHelper.ToRadians(15) : MathHelper.ToRadians(-15);
                float jumpAngle = player.IsFacingLeft ? MathHelper.ToRadians(-30) : MathHelper.ToRadians(30);

                float targetAngle = player.IsJumping ? jumpAngle : idleAngle;
                float lerpSpeed = 5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                currentSwordAngle = MathHelper.Lerp(currentSwordAngle, targetAngle, lerpSpeed);

                float idleWave = ((FreezeGameWorldAnimations && !IsRenderingPortrait) || (Main.Paused && !IsRenderingPortrait)) ? 0f : (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.2f;
                float walkTilt = player.IsMoving ? (float)Math.Sin(player.WalkTimer * MathHelper.TwoPi * 2) * 0.7f : 0f;
                float finalAngle = currentSwordAngle + idleWave + walkTilt;

                spriteBatch.Draw(Texture, itemPosition, null, Color.White, finalAngle, origin, jumpScale,
                    player.IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);
            }
        }

        protected void DrawChestplate(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
            float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.walkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;
            Vector2 drawPosition = player.Position + new Vector2(0, jumpOffset - walkBobOffset);

            Vector2 bodyOrigin = new Vector2(player.T_Body.Width / 2f, player.T_Body.Height / 2f - player.T_Head.Height);

            spriteBatch.Draw(Texture, drawPosition + player.CalculateKnockbackOffset() + new Vector2((player.IsFacingLeft ? 1 : 6), 0), null, Color.White, player.KnockbackRotation, bodyOrigin, player.CurrentScale,
                player.IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);
        }

        protected void DrawHelmet(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
            float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.walkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;
            Vector2 drawPosition = player.Position + new Vector2(0, jumpOffset - walkBobOffset);

            float headWobble = player.IsMoving ? (float)Math.Sin(player.walkTimer * MathHelper.TwoPi) * Player.WALK_HEAD_BOUNCE_HEIGHT : 0f;
            Vector2 headOffset = new Vector2(headWobble, Player.HEAD_VERTICAL_OFFSET);
            float finalRotation = player.CalculateHeadRotation() + player.KnockbackRotation;
            Vector2 headEyeOrigin = new Vector2(Texture.Width / 2, Texture.Height);

            spriteBatch.Draw(Texture, drawPosition + player.CalculateKnockbackOffset() + headOffset, null, Color.White, finalRotation, headEyeOrigin, player.CurrentScale,
                player.IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);
        }

        protected void DrawOffhandIdle(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            var pos = player.Position;
            float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
            Vector2 drawPosition = pos + new Vector2(0, jumpOffset);
            if (!player.IsKnocked)
            {
                float weaponWalkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT * 1.5f : 0f;
                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress) : Player.BASE_SCALE;

                Vector2 itemPosition = drawPosition + new Vector2(-Texture.Width / 2 * (player.IsFacingLeft ? -1 : 1), -weaponWalkBobOffset + Texture.Height / 2);

                Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);

                spriteBatch.Draw(Texture, itemPosition, null, Color.White, 0f, origin, jumpScale,
                    player.IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);
            }
        }

        protected void DrawStaffIdle(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            if (!player.IsKnocked && !player.IsAttacking)
            {
                var pos = player.Position;
                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                Vector2 drawPosition = pos + new Vector2(0, jumpOffset);

                float time = ((FreezeGameWorldAnimations && !IsRenderingPortrait) || (Main.Paused && !IsRenderingPortrait)) ? 0f : (float)gameTime.TotalGameTime.TotalSeconds;

                float sideOffset = (player.T_Body.Width / 2f + Texture.Width / 2) * (player.IsFacingLeft ? -1f : 1f);
                float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;

                float figure8X = (float)Math.Sin(time * 4f) * 6f;
                float figure8Y = (float)Math.Sin(time * 8f) * 3f;
                float orbitalX = (float)Math.Cos(time * 2.4f) * 3f;
                float orbitalY = (float)Math.Sin(time * 2.4f) * 2f;
                float finalRotation = (float)Math.Sin(time * 4f) * 0.15f +
                                    (float)Math.Sin(time * 12f) * 0.05f;

                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress) : Player.BASE_SCALE;

                float jumpRaise = player.IsJumping ? Texture.Height / 1.75f * (float)Math.Sin(Math.PI * jumpProgress) : 0f;

                Vector2 basePosition = drawPosition + new Vector2(
                    sideOffset + figure8X + orbitalX,
                    -walkBobOffset + figure8Y + orbitalY + (Texture.Height * 0.25f) - jumpRaise
                );

                Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height);

                spriteBatch.Draw(Texture, basePosition, null, Color.White, finalRotation, origin, jumpScale,
                    player.IsFacingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally, drawLayer);
            }
        }

        protected void DrawStaffAttack(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            if (player.IsAttacking)
            {
                var pos = player.Position;
                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                Vector2 drawPosition = pos + new Vector2(0, jumpOffset);

                float time = ((FreezeGameWorldAnimations && !IsRenderingPortrait) || (Main.Paused && !IsRenderingPortrait)) ? 0f : (float)gameTime.TotalGameTime.TotalSeconds;

                float sideOffset = (player.T_Body.Width / 2f + Texture.Width / 2) * (player.IsFacingLeft ? -1f : 1f);
                float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;

                float figure8X = (float)Math.Sin(time * 6f) * 12f;
                float figure8Y = (float)Math.Sin(time * 12f) * 6f;
                float orbitalX = (float)Math.Cos(time * 3.6f) * 6f;
                float orbitalY = (float)Math.Sin(time * 3.6f) * 4f;
                float finalRotation = (float)Math.Sin(time * 6f) * 0.3f +
                                    (float)Math.Sin(time * 18f) * 0.1f;

                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress) : Player.BASE_SCALE;

                Vector2 basePosition = drawPosition + new Vector2(
                    sideOffset + figure8X + orbitalX,
                    -walkBobOffset + figure8Y + orbitalY + (Texture.Height * 0.25f)
                );

                Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height);

                spriteBatch.Draw(Texture, basePosition, null, Color.White, finalRotation, origin, jumpScale,
                    player.IsFacingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally, drawLayer);
            }
        }

        protected float currentGunAngle = MathHelper.ToRadians(30);

        protected void DrawGunIdle(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            if (!player.IsAttacking && !player.IsKnocked)
            {
                float breath = ((FreezeGameWorldAnimations && !IsRenderingPortrait) || (Main.Paused && !IsRenderingPortrait)) ? 0f : (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2f) * 0.20f;

                float targetAngle = MathHelper.ToRadians(30);
                if (player.IsMoving || player.IsJumping)
                    targetAngle = MathHelper.ToRadians(-60);

                float lerpSpeed = 5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                currentGunAngle = MathHelper.Lerp(currentGunAngle, ((FreezeGameWorldAnimations && !IsRenderingPortrait) || (Main.Paused && !IsRenderingPortrait)) ? currentGunAngle : targetAngle, lerpSpeed);

                float idleRotation = player.IsFacingLeft ? -currentGunAngle + breath : currentGunAngle + breath;

                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;

                float offsetX = (player.T_Body.Width * (Type == "[Weapon - Rifle]" ? 0.2f : 0.5f)) * (player.IsFacingLeft ? -1f : 1f);
                float offsetY = player.T_Body.Height * 0.3f - Texture.Height * 0.5f;
                Vector2 offset = new Vector2(offsetX, offsetY);

                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping
                    ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress)
                    : Player.BASE_SCALE;

                Vector2 drawPos = player.Position + offset * jumpScale + new Vector2(0, jumpOffset - walkBobOffset);
                Vector2 origin = new Vector2(player.IsFacingLeft ? Texture.Width : 0, Texture.Height / 2f);

                spriteBatch.Draw(Texture, drawPos, null, Color.White, idleRotation, origin, jumpScale,
                    player.IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);
            }
        }

        protected void DrawGunAttack(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            if (player.IsAttacking)
            {
                float animationDuration = UseTime * 0.95f;
                float elapsed = UseTime - player.AttackTimer;
                float animProgress = MathHelper.Clamp(elapsed / animationDuration, 0f, 1f);

                float baseAngle = (float)Math.Atan2(player.AttackDirection.Y, player.AttackDirection.X);

                float recoil = Recoil * (float)Math.Sin(animProgress * MathHelper.Pi);

                float attackRotation = player.IsFacingLeft ? MathHelper.Pi + baseAngle + recoil : baseAngle - recoil;

                float jumpOffset = player.IsJumping ? -Player.JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (player.jumpTime / Player.JUMP_TIME_VALUE))) : 0f;
                float walkBobOffset = player.IsMoving ? Math.Abs((float)Math.Sin(player.WalkTimer * MathHelper.TwoPi)) * Player.WALK_BOUNCE_HEIGHT : 0f;

                float offsetX = (player.T_Body.Width * (Type == "[Weapon - Rifle]" ? 0.2f : 0.5f)) * (player.IsFacingLeft ? -1f : 1f);
                float offsetY = player.T_Body.Height * 0.3f - Texture.Height * 0.5f;
                Vector2 offset = new Vector2(offsetX, offsetY);

                float jumpProgress = player.IsJumping ? 1f - (player.jumpTime / Player.JUMP_TIME_VALUE) : 0f;
                float jumpScale = player.IsJumping
                    ? Player.BASE_SCALE + (Player.JUMP_SCALE - Player.BASE_SCALE) * (float)Math.Sin(Math.PI * jumpProgress)
                    : Player.BASE_SCALE;

                float rifleMoveX = 0f;
                if (Type == "[Weapon - Rifle]")
                {
                    rifleMoveX = -Texture.Width / 5f * animProgress * (player.IsFacingLeft ? -1f : 1f);
                }

                Vector2 drawPos = player.Position + offset * jumpScale + new Vector2(rifleMoveX, jumpOffset - walkBobOffset);

                Vector2 origin = new Vector2(player.IsFacingLeft ? Texture.Width : 0, Texture.Height / 2f);

                spriteBatch.Draw(Texture, drawPos, null, Color.White, attackRotation, origin, jumpScale,
                    player.IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);
            }
        }
    }
}