using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Proximity.Content
{
    public enum EquipmentSlot
    {
        Weapon,
        Chestplate,
        Offhand,
        Helmet
    }

    public class Player
    {
        public const float BASE_SCALE = 1.0f;
        public const float BASE_SPEED = 100f;
        public const float BASE_KNOCKBACK = 0f;
        public const int BASE_HEALTH = 100;
        public const int BASE_DAMAGE = 0;
        public const int BASE_DEFENSE = 0;
        public const float BASE_KNOCKBACK_RESISTANCE = 0f;
        public const float BASE_IMMUNITY_TIME = 3f;

        public const float KNOCKBACK_TIME_VALUE = 1f;
        public const float KNOCKBACK_DECAY = 0.9f;
        public const float KNOCKBACK_ROTATION = 90f;
        public const float KNOCKBACK_SHADOW_STRETCH_FACTOR = 0.5f;

        public const float JUMP_TIME_VALUE = 0.75f;
        public const float JUMP_BOUNCE_HEIGHT = 100f;
        public const float JUMP_SCALE = 1.3f;
        public const float JUMP_SCALE_RECOVERY_SPEED = 2f;

        public const float WALK_BOUNCE_HEIGHT = 15f;
        public const float WALK_HEAD_BOUNCE_HEIGHT = 7f;

        public const float SPEED_MULTIPLIER = 0.06f;

        public const float SHADOW_OPACITY = 0.6f;
        public const float SHADOW_SCALE = 0.75f;

        public const float EYE_BLINK_INTERVAL_MIN = 0.2f;
        public const float EYE_BLINK_INTERVAL_MAX = 3.0f;

        public const float HEAD_ROTATION_LIMIT = 60f;
        public const float HEAD_VERTICAL_OFFSET = 5f;

        public readonly Random random = new Random(Environment.TickCount + 2);
        private readonly ItemProperties itemProperties;
        private readonly Dictionary<int, Item> ItemDatabase;
        private readonly Dictionary<EquipmentSlot, Item> Equipment = new();
        public readonly Texture2D T_Head, T_Body, T_Eye, T_Shadow;
        private readonly Joystick Joystick_Movement, Joystick_Attack;
        public readonly ParticleManager particle;
        private readonly FloatingTextManager floatingText;
        private int currentHealth;
        public float jumpTime = JUMP_TIME_VALUE;
        public float CurrentScale = BASE_SCALE;
        public float walkTimer;
        public float KnockbackRotation;
        private float attackTimer;
        private float KnockbackTimer;
        private float EyeBlinkInterval, EyeTimer;
        private float hurtFlashTimer;
        private float immunityTimer;
        private bool IsEyeOpen;
        private Rectangle EyeRect_Open, EyeRect_Closed;
        private Vector2 MovementDirection;
        private Vector2 attackDirection;
        private Vector2 KnockbackVelocity;
        public Vector2 Center;

        public bool IsAttacking;
        public bool IsMoving;
        public bool IsFacingLeft;
        public bool IsKnocked;
        public bool IsImmune;

        public int MeleeAttackID { get; private set; } = 0;
        public Rectangle Hitbox { get; set; }
        public Rectangle WeaponHitbox { get; set; }
        public Rectangle OffhandHitbox { get; set; }
        public Rectangle PlayerSpriteHitbox { get; private set; }
        public float WeaponHitboxRotation { get; set; }
        public Vector2 Position { get; set; }
        public float SkinColor { get; private set; }
        public bool IsJumping { get; private set; }
        public IReadOnlyDictionary<EquipmentSlot, Item> EquippedItems => Equipment;
        public float AttackTimer => attackTimer;
        public Vector2 AttackDirection => attackDirection;
        public float JumpTime => jumpTime;
        public float WalkTimer => walkTimer;

        public float Speed => CalculateSpeed();
        public int MaxHealth => CalculateMaxHealth();
        public int CurrentHealth => currentHealth;
        public int Damage => CalculateDamage();
        public float Knockback => CalculateKnockback();
        public int Defense => CalculateDefense();
        public float KnockbackResistance => CalculateKnockbackResistance();
        public float ShootSpeed => CalculateShootSpeed();

        public Player(Joystick movementJoystick, Joystick attackJoystick, Texture2D playerHead, Texture2D playerBody,
            Texture2D playerEye, Texture2D shadowTexture, Vector2 position, ItemProperties itemProperties, ParticleManager particleManager, FloatingTextManager floatingTextManager, float skinColor)
        {
            Position = position;
            Joystick_Movement = movementJoystick;
            Joystick_Attack = attackJoystick;
            T_Head = playerHead;
            T_Body = playerBody;
            T_Eye = playerEye;
            T_Shadow = shadowTexture;
            particle = particleManager;
            floatingText = floatingTextManager;

            EyeRect_Open = new Rectangle(0, 0, T_Eye.Width, T_Eye.Height / 2);
            EyeRect_Closed = new Rectangle(0, T_Eye.Height / 2, T_Eye.Width, T_Eye.Height / 2);

            ItemDatabase = new Dictionary<int, Item>(itemProperties.GetItems());
            this.itemProperties = itemProperties;

            SkinColor = skinColor;
            currentHealth = MaxHealth;
        }

        public void Jump()
        {
            if (!IsKnocked)
            {
                IsJumping = true;
            }
        }

        public void Update(GameTime gameTime, Arena arena, KeyboardState keyboardState, MouseState mouseState, Camera camera = null)
        {
            if (gameTime == null || arena == null)
                throw new ArgumentNullException("GameTime and Arena cannot be null");
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            particle.Update(deltaTime);
            UpdateJump(deltaTime);
            UpdateKnockback(deltaTime);
            UpdateMovement(deltaTime, arena, keyboardState, mouseState);
            UpdateEyeBlink(deltaTime);
            UpdateAttack(deltaTime, camera);
            UpdateHurtEffect(deltaTime);
            UpdateHitboxes(gameTime);
            UpdateEquippedItems(deltaTime, gameTime, this);
            Center = Position + new Vector2(T_Body.Width / 2, T_Body.Height / 2);
        }

        public void EquipItem(int itemId, int prefixId = 0, int suffixId = 0)
        {
            if (!ItemDatabase.TryGetValue(itemId, out Item item))
            {
                return;
            }

            try
            {
                if (ItemModifier.Prefixes.TryGetValue(prefixId, out Prefix prefix))
                    item.Prefix = prefix.Name;
                if (ItemModifier.Suffixes.TryGetValue(suffixId, out Suffix suffix))
                    item.Suffix = suffix.Name;
                if (item.Type.Contains("[Weapon "))
                    Equipment[EquipmentSlot.Weapon] = item;
                if (item.Type.Contains("[Offhand]"))
                    Equipment[EquipmentSlot.Offhand] = item;
                if (item.Type.Contains("[Chestplate]"))
                    Equipment[EquipmentSlot.Chestplate] = item;
                if (item.Type.Contains("[Helmet]"))
                    Equipment[EquipmentSlot.Helmet] = item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to equip item {itemId}: {ex.Message}");
            }
        }

        public void Hurt(int damage, Vector2 direction, float speed)
        {
            if (!IsImmune)
            {
                floatingText.Add($"-{damage}", PlayerSpriteHitbox.Center.ToVector2() + new Vector2($"-{damage}".Length * -3, -T_Body.Height), Color.White, Color.Red, 1f, this, true);
                ApplyKnockback(direction, speed);
                int effectiveDamage = Math.Max(0, damage - Defense);
                currentHealth = Math.Max(0, currentHealth - effectiveDamage);
                hurtFlashTimer = KNOCKBACK_TIME_VALUE;
                immunityTimer = BASE_IMMUNITY_TIME;
                IsFacingLeft = direction.X < 0;
            }
            /*if (IsJumping)
            {
                floatingText.Add("Dodged !", PlayerSpriteHitbox.Center.ToVector2() + new Vector2($"-{damage}".Length * -3, -T_Body.Height), Color.LightSteelBlue, Color.LightSteelBlue, 1f, this);
            }
            if (IsImmune)
            {
                floatingText.Add("Immune !", PlayerSpriteHitbox.Center.ToVector2() + new Vector2($"-{damage}".Length * -3, -T_Body.Height), Color.LightSteelBlue, Color.LightSteelBlue, 1f, this);
            }*/
        }

        private void UpdateJump(float deltaTime)
        {
            if (IsJumping && jumpTime > 0)
            {
                walkTimer = 0f;
                jumpTime -= deltaTime;
                float progress = 1 - (jumpTime / JUMP_TIME_VALUE);
                CurrentScale = BASE_SCALE + (JUMP_SCALE - BASE_SCALE) * (float)Math.Sin(Math.PI * progress);
            }
            else
            {
                IsJumping = false;
                jumpTime = JUMP_TIME_VALUE;

                if (CurrentScale > BASE_SCALE)
                {
                    CurrentScale = Math.Max(BASE_SCALE, CurrentScale - deltaTime * JUMP_SCALE_RECOVERY_SPEED);
                }
            }
        }

        private void UpdateKnockback(float deltaTime)
        {
            if (KnockbackTimer > 0)
            {
                IsKnocked = true;
                float resistanceFactor = 1f - KnockbackResistance;
                Position += KnockbackVelocity * resistanceFactor * deltaTime;
                KnockbackTimer -= deltaTime;
                KnockbackVelocity *= KNOCKBACK_DECAY;

                float targetRotation = IsFacingLeft ? MathHelper.ToRadians(KNOCKBACK_ROTATION) : MathHelper.ToRadians(-KNOCKBACK_ROTATION);
                float rotationSpeed = 5f;
                KnockbackRotation = MathHelper.Lerp(KnockbackRotation, targetRotation, deltaTime * rotationSpeed);
            }
            else
            {
                IsKnocked = false;
                KnockbackRotation = MathHelper.Lerp(KnockbackRotation, 0f, deltaTime * 10f);
            }
        }

        private void UpdateMovement(float deltaTime, Arena arena, KeyboardState keyboardState, MouseState mouseState)
        {
            if (!IsKnocked)
            {
                // WASD input
                Vector2 wasdDirection = Vector2.Zero;
                if (keyboardState.IsKeyDown(Keys.W)) wasdDirection.Y -= 1;
                if (keyboardState.IsKeyDown(Keys.S)) wasdDirection.Y += 1;
                if (keyboardState.IsKeyDown(Keys.A)) wasdDirection.X -= 1;
                if (keyboardState.IsKeyDown(Keys.D)) wasdDirection.X += 1;

                // Combine joystick and WASD
                Vector2 moveDirection = Joystick_Movement.Direction + wasdDirection;

                if (moveDirection != Vector2.Zero)
                {
                    MovementDirection = moveDirection;
                    IsMoving = true;
                    float walkAnimSpeed = (Speed / 150f);
                    walkTimer = (walkTimer + deltaTime * walkAnimSpeed) % 0.5f;

                    Vector2 normalizedDirection = Vector2.Normalize(moveDirection);
                    Vector2 newPosition = Position + normalizedDirection * Speed * SPEED_MULTIPLIER;

                    if (CanWalk(newPosition, new Rectangle((int)Position.X, (int)Position.Y, T_Body.Width / 2, T_Body.Height / 2), arena))
                    {
                        newPosition.X = MathHelper.Clamp(newPosition.X, 0, (arena.SizeX * arena.TileSize) - T_Body.Width / 2);
                        newPosition.Y = MathHelper.Clamp(newPosition.Y, 0, (arena.SizeY * arena.TileSize) - T_Body.Height / 2);
                        Position = newPosition;
                    }
                    else
                    {
                        Hurt(0, normalizedDirection, 600f);
                    }

                    if (!IsAttacking)
                    {
                        IsFacingLeft = moveDirection.X < 0;
                    }
                }
                else
                {
                    MovementDirection = Vector2.Zero;
                    if (IsMoving)
                    {
                        float walkAnimSpeed = (Speed / 150f);
                        walkTimer = Math.Max(0, walkTimer - deltaTime * walkAnimSpeed);

                        if (walkTimer <= 0)
                        {
                            IsMoving = false;
                            walkTimer = 0;
                        }
                    }
                }
            }
            else
            {
                IsMoving = false;
            }
        }

        private void UpdateEquippedItems(float deltaTime, GameTime gameTime, Player player)
        {
            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    item.Update(deltaTime, gameTime, this);
                    item.UpdateParticles(deltaTime, gameTime, this);
                }
            }
        }

        private void UpdateEyeBlink(float deltaTime)
        {
            EyeTimer += deltaTime;
            if (EyeTimer >= EyeBlinkInterval)
            {
                IsEyeOpen = random.Next(2) == 0;
                EyeBlinkInterval = IsEyeOpen ?
                    (float)random.NextDouble() * (EYE_BLINK_INTERVAL_MAX - EYE_BLINK_INTERVAL_MIN) + EYE_BLINK_INTERVAL_MIN :
                    (float)random.NextDouble() * 0.3f + 0.2f;
                EyeTimer = 0f;
            }
        }

        private void UpdateAttack(float deltaTime, Camera camera = null)
        {
            if (IsKnocked)
            {
                IsAttacking = false;
                attackTimer = 0;
                return;
            }

            Vector2 mouseAttackDirection = Vector2.Zero;
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                Vector2 mouseScreen = new Vector2(mouseState.X, mouseState.Y);
                Vector2 mouseWorld = mouseScreen;
                if (camera != null)
                {
                    mouseWorld = camera.ScreenToWorld(mouseScreen);
                }
                mouseAttackDirection = mouseWorld - Center;
                if (mouseAttackDirection != Vector2.Zero)
                    mouseAttackDirection.Normalize();
            }

            Vector2 finalAttackDirection = Joystick_Attack.Direction;
            if (mouseAttackDirection != Vector2.Zero)
                finalAttackDirection = mouseAttackDirection;

            if (finalAttackDirection != Vector2.Zero && !IsJumping && !IsKnocked && Equipment.TryGetValue(EquipmentSlot.Weapon, out var weapon) && weapon != null)
            {
                IsFacingLeft = finalAttackDirection.X < 0;

                if (!IsAttacking)
                {
                    IsAttacking = true;
                    attackTimer = weapon.UseTime;
                    attackDirection = finalAttackDirection;
                    MeleeAttackID = (MeleeAttackID + 1) % 1000;
                    weapon.Use(deltaTime, this, attackDirection);
                }
            }

            if (IsAttacking)
            {
                attackTimer -= deltaTime;
                if (attackTimer <= 0f)
                {
                    if (finalAttackDirection != Vector2.Zero && !IsJumping && !IsKnocked && Equipment.TryGetValue(EquipmentSlot.Weapon, out var currentWeapon) && currentWeapon != null)
                    {
                        attackTimer = currentWeapon.UseTime;
                        attackDirection = finalAttackDirection;
                        MeleeAttackID = (MeleeAttackID + 1) % 1000;
                        foreach (var item in Equipment.Values)
                        {
                            if (item != null)
                            {
                                item.Use(deltaTime, this, attackDirection);
                            }
                        }
                    }
                    else
                    {
                        IsAttacking = false;
                    }
                }
            }
        }

        private void UpdateHurtEffect(float deltaTime)
        {
            if (hurtFlashTimer > 0)
            {
                hurtFlashTimer = Math.Max(0, hurtFlashTimer - deltaTime);
            }
            if (immunityTimer > 0)
            {
                immunityTimer = Math.Max(0, immunityTimer - deltaTime);
                IsImmune = true;
            }
            else
            {
                if (!Main.DebugMode)
                {
                    IsImmune = false;
                }
            }
        }

        public void UpdateHitboxes(GameTime gameTime)
        {
            float playerHitboxWidth = T_Body.Width * BASE_SCALE;
            float playerHitboxHeight = T_Body.Height * BASE_SCALE;

            if (!IsKnocked)
            {
                Hitbox = new Rectangle(
                    (int)(Position.X - playerHitboxWidth / 2),
                    (int)(Position.Y - playerHitboxHeight / 2),
                    (int)playerHitboxWidth,
                    (int)playerHitboxHeight
                );
            }
            else
            {
                Hitbox = Rectangle.Empty;
            }

            float jumpOffset = IsJumping ? -JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (jumpTime / JUMP_TIME_VALUE))) : 0f;
            float walkBobOffset = IsMoving ? Math.Abs((float)Math.Sin(walkTimer * MathHelper.TwoPi)) * WALK_BOUNCE_HEIGHT : 0f;
            float headWobble = IsMoving ? (float)Math.Sin(walkTimer * MathHelper.TwoPi) * 7f : 0f;

            Vector2 visualPosition = Position + new Vector2(0, jumpOffset - walkBobOffset);

            float visualWidth = T_Body.Width * CurrentScale;
            float visualHeight = T_Body.Height * CurrentScale;

            PlayerSpriteHitbox = new Rectangle(
                (int)(visualPosition.X - visualWidth / 2),
                (int)(visualPosition.Y - visualHeight / 2),
                (int)visualWidth,
                (int)visualHeight
            );

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    item.UpdateHitboxes(this, gameTime);
                }
            }
        }

        private Color GetSkinColor(float progress)
        {
            Color black = new Color(94, 54, 33);
            Color white = new Color(255, 220, 185);
            return Color.Lerp(black, white, progress);
        }

        public Color GetColor()
        {
            if (hurtFlashTimer > 0)
            {
                float t = MathHelper.Clamp(hurtFlashTimer / KNOCKBACK_TIME_VALUE, 0f, 1f);
                byte a = GetSkinColor(SkinColor).A;
                byte r = (byte)MathHelper.Lerp(255, GetSkinColor(SkinColor).R, 1f - t);
                byte g = (byte)MathHelper.Lerp(0, GetSkinColor(SkinColor).G, 1f - t);
                byte b = (byte)MathHelper.Lerp(0, GetSkinColor(SkinColor).B, 1f - t);
                return new Color(r, g, b, a);
            }
            return GetSkinColor(SkinColor);
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            DrawHitbox(spriteBatch);
            DrawShadow(spriteBatch, 0.3f);
            DrawBaseBody(spriteBatch, gameTime, 0.31f);
            DrawEquipments(spriteBatch, gameTime, 0.31f);
        }

        public void DrawShadow(SpriteBatch spriteBatch, float drawLayer)
        {
            float jumpProgress = IsJumping ? 1f - (jumpTime / JUMP_TIME_VALUE) : 0f;
            float jumpShrink = (float)Math.Sin(Math.PI * jumpProgress) * 0.4f;
            float walkBobOffset = IsMoving ? Math.Abs((float)Math.Sin(walkTimer * MathHelper.TwoPi)) * WALK_BOUNCE_HEIGHT : 0f;
            float walkShrink = IsMoving ? walkBobOffset / WALK_BOUNCE_HEIGHT * 0.2f : 0f;
            float totalShrink = jumpShrink + walkShrink;
            float shadowScale = SHADOW_SCALE - totalShrink;

            int shadowWidth = (int)(T_Body.Width * 1.5f * shadowScale);
            int shadowHeight = (int)(T_Body.Width * shadowScale);
            Vector2 shadowPosition = Position + new Vector2(0, T_Body.Height / 4f);

            if (IsKnocked)
            {
                float adjustedRotation = KnockbackRotation * (IsFacingLeft ? -1 : 1);
                float stretchFactor = 1f + Math.Abs((float)Math.Sin(adjustedRotation)) * KNOCKBACK_SHADOW_STRETCH_FACTOR;
                shadowWidth = (int)(shadowWidth * stretchFactor);
            }

            Rectangle shadowRect = new Rectangle(
                (int)(shadowPosition.X - shadowWidth / 2f),
                (int)shadowPosition.Y,
                shadowWidth,
                shadowHeight
            );

            float opacity = SHADOW_OPACITY - (jumpProgress * 0.3f);
            if (IsKnocked)
                opacity *= SHADOW_OPACITY;

            spriteBatch.Draw(T_Shadow, shadowRect, null, Color.White * opacity, 0f, Vector2.Zero, SpriteEffects.None, drawLayer);
        }

        public void DrawBaseBody(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            float jumpOffset = IsJumping ? -JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * (1 - (jumpTime / JUMP_TIME_VALUE))) : 0f;
            float walkBobOffset = IsMoving ? Math.Abs((float)Math.Sin(walkTimer * MathHelper.TwoPi)) * WALK_BOUNCE_HEIGHT : 0f;
            Vector2 drawPosition = Position + new Vector2(0, jumpOffset - walkBobOffset);
            Vector2 bodyOrigin = new Vector2(T_Body.Width / 2, T_Body.Height / 2);

            spriteBatch.Draw(T_Body, drawPosition + CalculateKnockbackOffset(), null, GetColor(), KnockbackRotation, bodyOrigin, CurrentScale,
                IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer);

            float headWobble = IsMoving ? (float)Math.Sin(walkTimer * MathHelper.TwoPi) * WALK_HEAD_BOUNCE_HEIGHT : 0f;
            Vector2 headOffset = new Vector2(headWobble, HEAD_VERTICAL_OFFSET);
            Vector2 headEyeOrigin = new Vector2(T_Head.Width / 2, T_Head.Height);
            float finalRotation = CalculateHeadRotation() + KnockbackRotation;

            spriteBatch.Draw(T_Head, drawPosition + CalculateKnockbackOffset() + headOffset, null, GetColor(), finalRotation, headEyeOrigin, CurrentScale,
                IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer + 0.002f);

            float eyeBounceOffset = IsMoving ? Math.Abs((float)Math.Sin(walkTimer * MathHelper.TwoPi)) * -10f : 0f;
            Vector2 eyeOffset = new Vector2((float)Math.Sin(CalculateHeadRotation()), HEAD_VERTICAL_OFFSET + eyeBounceOffset);
            eyeOffset = Vector2.Transform(eyeOffset + headOffset, Matrix.CreateRotationZ(CalculateHeadRotation()));
            spriteBatch.Draw(T_Eye, drawPosition + CalculateKnockbackOffset() + eyeOffset, IsEyeOpen ? EyeRect_Open : EyeRect_Closed,
                Color.White, finalRotation, new Vector2(T_Eye.Width / 2, T_Eye.Height / 2),
                CurrentScale, IsFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, drawLayer + 0.003f);
        }

        public void DrawEquipments(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            foreach (var item in Equipment.Values)
            {
                if (item != null)//0.311 head
                {
                    if (item.DrawSlot == DrawSlot.BelowBody)
                    {
                        item.PreDraw(spriteBatch, gameTime, this, drawLayer - 0.009f);
                        item.PostDraw(spriteBatch, gameTime, this, drawLayer - 0.009f);
                    }
                    if (item.DrawSlot == DrawSlot.AboveBody)
                    {
                        item.PreDraw(spriteBatch, gameTime, this, drawLayer + 0.001f);
                        item.PostDraw(spriteBatch, gameTime, this, drawLayer + 0.001f);
                    }
                    if (item.DrawSlot == DrawSlot.BelowHead)
                    {
                        item.PreDraw(spriteBatch, gameTime, this, drawLayer - 0.008f);
                        item.PostDraw(spriteBatch, gameTime, this, drawLayer - 0.008f);
                    }
                    if (item.DrawSlot == DrawSlot.AboveHead)
                    {
                        item.PreDraw(spriteBatch, gameTime, this, drawLayer + 0.004f);
                        item.PostDraw(spriteBatch, gameTime, this, drawLayer + 0.004f);
                    }
                    if (item.DrawSlot == DrawSlot.Offhand)
                    {
                        item.PreDraw(spriteBatch, gameTime, this, drawLayer + 0.005f);
                        item.PostDraw(spriteBatch, gameTime, this, drawLayer + 0.005f);
                    }
                }
            }
        }

        private void DrawHitbox(SpriteBatch spriteBatch)
        {
            if (Main.DebugMode)
            {
                if (PlayerSpriteHitbox != Rectangle.Empty)
                {
                    spriteBatch.DrawRectangleBorder(PlayerSpriteHitbox, Color.Lime * 0.2f, Color.Lime * 0.8f, 1f);
                }
                if (Hitbox != Rectangle.Empty)
                {
                    spriteBatch.DrawRectangleBorder(Hitbox, Color.Green * 0.2f, Color.Green * 0.8f, 1f);
                }
                if (WeaponHitbox != Rectangle.Empty)
                {
                    spriteBatch.DrawRectangleBorder(WeaponHitbox, Color.Lime * 0.2f, Color.Lime * 0.8f, 1f, 4f, WeaponHitboxRotation);
                }
                if (OffhandHitbox != Rectangle.Empty)
                {
                    spriteBatch.DrawRectangleBorder(OffhandHitbox, Color.Lime * 0.2f, Color.Lime * 0.8f, 1f);
                }
                if (IsAttacking && EquippedItems.TryGetValue(EquipmentSlot.Weapon, out var weapon) && weapon.Type.Contains("Sword"))
                {
                    float attackRadius = weapon.Texture.Height * 2f;
                    Vector2 playerCenter = Hitbox.Center.ToVector2();
                    Vector2 attackDir = AttackDirection;
                    if (attackDir == Vector2.Zero) attackDir = Vector2.UnitX;
                    float attackAngle = (float)Math.Atan2(attackDir.Y, attackDir.X);
                    float swingRange = weapon.SwingRange / 1.25f;
                    float minAngle = attackAngle - MathHelper.ToRadians(swingRange / 2f);
                    float maxAngle = attackAngle + MathHelper.ToRadians(swingRange / 2f);
                    int arcSegments = 48;
                    float angleStep = (maxAngle - minAngle) / arcSegments;
                    Vector2[] arcPoints = new Vector2[arcSegments + 2];
                    arcPoints[0] = playerCenter;
                    for (int i = 0; i <= arcSegments; i++)
                    {
                        float angle = minAngle + i * angleStep;
                        arcPoints[i + 1] = playerCenter + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * attackRadius;
                    }
                    for (int i = 1; i < arcPoints.Length - 1; i++)
                    {
                        spriteBatch.DrawLine(arcPoints[i], arcPoints[i + 1], Color.Lime, 1f, 3f);
                    }
                    spriteBatch.DrawLine(playerCenter, arcPoints[1], Color.Lime, 1f, 2f);
                    spriteBatch.DrawLine(playerCenter, arcPoints[arcPoints.Length - 1], Color.Lime, 1f, 2f);
                }
            }
        }

        private bool CanWalk(Vector2 newPosition, Rectangle playerRectangle, Arena arena)
        {
            int tileSize = arena.TileSize;
            int leftTile = (int)newPosition.X / tileSize;
            int rightTile = ((int)newPosition.X + playerRectangle.Width) / tileSize;
            int topTile = (int)newPosition.Y / tileSize;
            int bottomTile = ((int)newPosition.Y + playerRectangle.Height) / tileSize;

            for (int x = leftTile; x <= rightTile; x++)
            {
                for (int y = topTile; y <= bottomTile; y++)
                {
                    Tile tile = arena.GetTile(x, y);
                    if (tile != null && !Tile.CanWalk(tile.ID))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void ApplyKnockback(Vector2 normalizedDirection, float speed)
        {
            float resistanceFactor = 1f - KnockbackResistance;
            KnockbackVelocity = -normalizedDirection * speed * resistanceFactor;
            KnockbackTimer = KNOCKBACK_TIME_VALUE;
        }

        private float CalculateSpeed()
        {
            float speed = BASE_SPEED;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    speed += itemProperties.GetSpeedModifier(item.ID, prefixId, suffixId);
                }
            }

            return Math.Max(BASE_SPEED, speed);
        }

        private int CalculateMaxHealth()
        {
            int health = BASE_HEALTH;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    health += (int)itemProperties.GetHealthModifier(item.ID, prefixId, suffixId);
                }
            }

            return Math.Max(BASE_HEALTH, health);
        }

        private int CalculateDamage()
        {
            int damage = BASE_DAMAGE;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    damage += (int)itemProperties.GetDamageModifier(item.ID, prefixId, suffixId);
                }
            }

            return Math.Max(BASE_DAMAGE, damage);
        }

        private int CalculateShootSpeed()
        {
            int range = 0;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    range += (int)itemProperties.GetShotSpeedModifier(item.ID, prefixId, suffixId);
                }
            }

            return Math.Max(0, range);
        }

        private int CalculateDefense()
        {
            int defense = BASE_DEFENSE;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    defense += (int)itemProperties.GetDefenseModifier(item.ID, prefixId, suffixId);
                }
            }

            return Math.Max(BASE_DEFENSE, defense);
        }

        private float CalculateKnockback()
        {
            float knockback = BASE_KNOCKBACK;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    knockback += (int)itemProperties.GetKnockbackModifier(item.ID, prefixId, suffixId);
                }
            }

            return Math.Max(BASE_KNOCKBACK, knockback);
        }

        private float CalculateKnockbackResistance()
        {
            float resistance = BASE_KNOCKBACK_RESISTANCE;

            foreach (var item in Equipment.Values)
            {
                if (item != null)
                {
                    int prefixId = GetModifierIdFromName(item.Prefix, ItemModifier.Prefixes);
                    int suffixId = GetModifierIdFromName(item.Suffix, ItemModifier.Suffixes);

                    resistance += itemProperties.GetKnockbackResistanceModifier(item.ID, prefixId, suffixId);
                }
            }

            return MathHelper.Clamp(resistance, 0f, 1f);
        }

        public float CalculateHeadRotation()
        {
            if (!IsKnocked)
            {
                Vector2 direction = Joystick_Attack.Direction != Vector2.Zero ? Joystick_Attack.Direction : MovementDirection;
                if (direction != Vector2.Zero)
                {
                    float rawRotation = (float)Math.Atan2(direction.Y, Math.Abs(direction.X));
                    float rotationDegrees = MathHelper.ToDegrees(rawRotation);
                    rotationDegrees = MathHelper.Clamp(rotationDegrees, -HEAD_ROTATION_LIMIT, HEAD_ROTATION_LIMIT);
                    return MathHelper.ToRadians(rotationDegrees) * (IsFacingLeft ? -1 : 1);
                }
            }
            return 0f;
        }

        public Vector2 CalculateKnockbackOffset()
        {
            if (IsKnocked)
            {
                float adjustedRotation = KnockbackRotation * (IsFacingLeft ? -1 : 1);
                float knockbackHeight = -(float)Math.Sin(adjustedRotation) * (T_Body.Height / 3f);
                return new Vector2(0, knockbackHeight);
            }
            return Vector2.Zero;
        }

        private int GetModifierIdFromName(string name, Dictionary<int, Prefix> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier.Value.Name == name)
                    return modifier.Key;
            }
            return 0;
        }

        private int GetModifierIdFromName(string name, Dictionary<int, Suffix> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                if (modifier.Value.Name == name)
                    return modifier.Key;
            }
            return 0;
        }
    }
}