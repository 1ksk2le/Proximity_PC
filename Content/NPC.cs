using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content
{
    public abstract class NPC
    {
        private const string TEXTURE_PATH_FORMAT = "Textures/NPCs/t_NPC_{0}";
        private const string HEALTHBAR_TEXTURE_PATH = "Textures/UI/t_Healthbar";
        public static Texture2D HealthbarTexture;
        protected readonly ContentManager contentManager;
        public readonly ParticleManager particle;
        public readonly FloatingTextManager floatingText;
        public readonly Random random = new Random(Environment.TickCount + 8);

        public int ID { get; protected set; }
        public int AI { get; protected set; }
        public int Damage { get; protected set; }
        public int Defense { get; protected set; }
        public string Name { get; protected set; }
        public Texture2D Texture { get; protected set; }
        public Vector2 Position { get; set; }
        public Vector2 TexturePosition { get; set; }
        public float DetectRange { get; protected set; }
        public float Scale { get; protected set; } = 1f;
        public float Rotation { get; protected set; }
        public float Knockback { get; protected set; }
        public float KnockbackResistance { get; protected set; }
        public bool IsActive { get; set; }
        public bool IsImmune { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; protected set; }
        public int TotalFrames { get; protected set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string DisplayName => $"{Prefix} {Name} {Suffix}".Trim();

        public const float HURT_TIME_VALUE = 1f;
        private const float KNOCKBACK_TIME_VALUE = 0.3f;

        public Color Color;
        private float hurtFlashTimer;
        private Vector2 knockbackVelocity;
        private float KnockbackTimer;

        private float displayedHealthPercent = 1f;
        private float healthBarVisibilityTimer = 0f;
        private float healthBarAlpha = 0f;
        private const float HEALTH_BAR_SHOW_TIME = 4f;
        private const float HEALTH_BAR_FADE_TIME = 0.5f;
        private const float HEALTH_BAR_DRAIN_SPEED = 0.33f;

        private readonly Dictionary<int, int> lastHit = new();
        private readonly HashSet<int> hitProjectiles = new();

        protected NPC(ContentManager contentManager, ParticleManager particleManager, FloatingTextManager floatingTextManager)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            this.particle = particleManager ?? throw new ArgumentNullException(nameof(particleManager));
            this.floatingText = floatingTextManager ?? throw new ArgumentNullException(nameof(floatingTextManager));
            if (HealthbarTexture == null)
            {
                try { HealthbarTexture = contentManager.Load<Texture2D>(HEALTHBAR_TEXTURE_PATH); } catch { }
            }
            Initialize();
            LoadTexture();
        }

        protected virtual void Initialize()
        {
            IsActive = true;
            Prefix = string.Empty;
            Suffix = string.Empty;
        }

        protected virtual void LoadTexture()
        {
            try
            {
                string texturePath = string.Format(TEXTURE_PATH_FORMAT, ID);
                Texture = contentManager.Load<Texture2D>(texturePath);
            }
            catch (Exception)
            {
            }
        }

        public virtual void Update(float deltaTime, Player player, IReadOnlyList<Projectile> projectiles)
        {
            if (!IsActive) return;
            UpdateHurtEffect(deltaTime);
            UpdateHealthbar(deltaTime);

            if (Prefix == "Fiery")
            {
                /*Color.R = (byte)random.Next(0, 255);
                Color.G = (byte)random.Next(0, 255);
                Color.B = (byte)random.Next(0, 255);*/
            }

            if (KnockbackTimer > 0)
            {
                Position += knockbackVelocity * deltaTime / KNOCKBACK_TIME_VALUE;
                KnockbackTimer -= deltaTime;
                if (KnockbackTimer <= 0)
                {
                    knockbackVelocity = Vector2.Zero;
                    KnockbackTimer = 0;
                }
            }

            if (Collides(player.Hitbox) && !player.IsKnocked)
            {
                Vector2 direction = Hitbox().Center.ToVector2() - player.Hitbox.Center.ToVector2();
                if (direction != Vector2.Zero)
                    direction.Normalize();
                else
                    direction = Vector2.UnitY;

                int damage = Damage <= player.Defense ? 1 : Damage;
                //player.Hurt(damage, direction, Knockback);
            }

            if (projectiles != null)
            {
                foreach (var proj in projectiles)
                {
                    if (!proj.IsActive) continue;
                    int projId = proj.UniqueId;
                    if (hitProjectiles.Contains(projId)) continue;
                    if (Collides(proj.Hitbox()) && !IsImmune)
                    {
                        hitProjectiles.Add(projId);
                        OnProjectileHit(proj);
                    }
                }
            }

            int playerId = player.GetHashCode();
            int currentAttackId = player.MeleeAttackID;
            if (player.IsAttacking && !player.IsJumping && !IsImmune && player.EquippedItems.TryGetValue(EquipmentSlot.Weapon, out var weapon) && weapon.Type.Contains("Sword"))
            {
                float attackProgress = 1f - (player.AttackTimer / weapon.UseTime);
                if (attackProgress >= 0.35f)
                {
                    if (!lastHit.TryGetValue(playerId, out int lastAttackId) || lastAttackId != currentAttackId)
                    {
                        lastHit[playerId] = currentAttackId;
                        float attackRadius = weapon.Texture.Height * 2f;
                        Vector2 playerCenter = player.Hitbox.Center.ToVector2();
                        Vector2 npcCenter = Hitbox().Center.ToVector2();
                        Vector2 toNPC = npcCenter - playerCenter;
                        float distance = toNPC.Length();
                        if (distance <= attackRadius)
                        {
                            float swingRange = weapon.ShootSpeed / 1.25f;
                            Vector2 attackDir = player.AttackDirection;
                            if (attackDir == Vector2.Zero) attackDir = Vector2.UnitX;
                            float attackAngle = (float)Math.Atan2(attackDir.Y, attackDir.X);
                            float npcAngle = (float)Math.Atan2(toNPC.Y, toNPC.X);
                            float minAngle = attackAngle - MathHelper.ToRadians(swingRange / 2f);
                            float maxAngle = attackAngle + MathHelper.ToRadians(swingRange / 2f);
                            bool inArc = false;
                            float wrappedNpc = MathHelper.WrapAngle(npcAngle);
                            float wrappedMin = MathHelper.WrapAngle(minAngle);
                            float wrappedMax = MathHelper.WrapAngle(maxAngle);
                            if (wrappedMin < wrappedMax)
                                inArc = wrappedNpc >= wrappedMin && wrappedNpc <= wrappedMax;
                            else
                                inArc = wrappedNpc >= wrappedMin || wrappedNpc <= wrappedMax;
                            if (inArc)
                            {
                                Vector2 knockbackDir = npcCenter - playerCenter;
                                if (knockbackDir != Vector2.Zero) knockbackDir.Normalize();
                                else knockbackDir = Vector2.UnitY;
                                int damage = player.Damage <= Defense ? 1 : player.Damage - Defense;
                                Hurt(damage, knockbackDir, player.Knockback, KnockbackResistance);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void OnProjectileHit(Projectile projectile)
        {
            Vector2 knockbackDir = projectile.Direction;
            if (knockbackDir != Vector2.Zero) knockbackDir.Normalize();
            else knockbackDir = Vector2.UnitY;
            int damage = projectile.Damage <= Defense ? 1 : projectile.Damage - Defense;
            projectile.Penetrate -= 1;
            if (projectile.Penetrate <= 0)
            {
                projectile.Kill();
            }

            Hurt(damage, knockbackDir, projectile.Knockback, KnockbackResistance);
        }

        public virtual void DrawShadow(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
        }

        public virtual void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            if (!IsActive || Texture == null) return;
            {
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            if (!IsActive || Texture == null) return;
        }

        public virtual void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            if (!IsActive || Texture == null) return;
            {
                DrawHealthbar(spriteBatch);

                if (Main.DebugMode)
                {
                    spriteBatch.DrawRectangleBorder(TextureHitbox(), Color.DarkRed * 0.2f, Color.DarkRed * 0.8f, 1f);
                    spriteBatch.DrawRectangleBorder(Hitbox(), Color.Red * 0.2f, Color.Red * 0.8f, 1f);
                    float width = Texture.Width;
                    float height = Texture.Height / TotalFrames;
                    Vector2 origin = new Vector2(0, height / 2);
                    Vector2 center;
                    center = Position + origin;
                    spriteBatch.DrawCircle(center, DetectRange, Color.Red, 32, 1f);
                    spriteBatch.DrawCircle(Hitbox().Center.ToVector2(), 16f, Color.Black, 32, 1f);
                }
                /*if (Prefix == "Fiery")
                {
                    float colorPulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.1f + 0.4f;
                    float scalePulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 0.2f + 1.75f;
                    int frameHeight = Texture.Height / TotalFrames;
                    int bloomWidth = (int)(Texture.Width * scalePulse * Scale);
                    int bloomHeight = (int)(frameHeight * scalePulse * Scale);
                    Rectangle bloomRect = new Rectangle(
                        (int)(TexturePosition.X - bloomWidth / 2f),
                        (int)(TexturePosition.Y - bloomHeight / 2f),
                        bloomWidth,
                        bloomHeight
                    );
                    spriteBatch.Draw(Main.Bloom, bloomRect, new Color(255, 70, 0, 150) * colorPulse);
                }*/
            }
        }

        protected virtual void Hurt(int damage, Vector2 knockbackDirection, float knockback = 0f, float knockbackResistance = 0f)
        {
            if (Health > 0)
            {
                Health = Math.Max(0, Health - damage);
                hurtFlashTimer = HURT_TIME_VALUE;
                healthBarVisibilityTimer = HEALTH_BAR_SHOW_TIME;
                float resistanceFactor = 1f - MathHelper.Clamp(knockbackResistance, 0f, 1f);
                if (knockback > 0f && knockbackDirection != Vector2.Zero)
                {
                    knockbackDirection.Normalize();
                    knockbackVelocity = knockbackDirection * knockback * resistanceFactor;
                    KnockbackTimer = KNOCKBACK_TIME_VALUE;
                }

                if (damage > 0 && floatingText != null)
                {
                    floatingText.Add($"-{damage}", TextureHitbox().Center.ToVector2() - new Vector2($"-{damage}".Length * -3, TextureHitbox().Height), Color.White, Color.Red, 1f, this, false);
                }
            }
        }

        public virtual void Kill()
        {
            IsActive = false;
        }

        private void UpdateHurtEffect(float deltaTime)
        {
            if (hurtFlashTimer > 0)
            {
                hurtFlashTimer = Math.Max(0, hurtFlashTimer - deltaTime);
            }
        }

        private void UpdateHealthbar(float deltaTime)
        {
            if (healthBarVisibilityTimer > 0)
            {
                healthBarVisibilityTimer = Math.Max(0, healthBarVisibilityTimer - deltaTime);

                if (healthBarVisibilityTimer > HEALTH_BAR_FADE_TIME)
                {
                    healthBarAlpha = 1f;
                }
                else if (healthBarVisibilityTimer > 0)
                {
                    healthBarAlpha = healthBarVisibilityTimer / HEALTH_BAR_FADE_TIME;
                }
                else
                {
                    healthBarAlpha = 0f;
                }
            }

            float currentHealthPercent = (float)Health / MaxHealth;
            if (displayedHealthPercent > currentHealthPercent)
            {
                displayedHealthPercent = Math.Max(currentHealthPercent, displayedHealthPercent - deltaTime * HEALTH_BAR_DRAIN_SPEED);
            }
            else
            {
                displayedHealthPercent = currentHealthPercent;
            }
        }

        private void DrawHealthbar(SpriteBatch spriteBatch)
        {
            if (HealthbarTexture != null && MaxHealth > 0 && healthBarAlpha > 0)
            {
                const int BORDER_FRAME_WIDTH = 6;
                const int FILL_FRAME_WIDTH = 1;
                const int FRAME_HEIGHT = 15;

                const int NAME_PADDING = 5;

                const int LEFT_FRAME = 0;
                const int MID_FRAME = 1;
                const int RIGHT_FRAME = 2;

                int npcWidth = (int)(Texture.Width * Scale);
                int barWidth = npcWidth;
                int leftWidth = BORDER_FRAME_WIDTH;
                int rightWidth = BORDER_FRAME_WIDTH;
                int midWidth = Math.Max(0, barWidth - leftWidth - rightWidth);

                int offSetX = 0;
                int offSetY = -40;
                int barY = (int)(TexturePosition.Y - (Texture.Height / TotalFrames) * Scale / 2 + offSetY);
                int barX = (int)(TexturePosition.X - barWidth / 2 + offSetX);

                if (!string.IsNullOrEmpty(DisplayName))
                {
                    Vector2 nameSize = Main.Font.MeasureString(DisplayName);
                    float nameScale = 1f;

                    Vector2 namePos = new Vector2(
                        barX + barWidth / 2 - (nameSize.X * nameScale / 2),
                        barY - NAME_PADDING - nameSize.Y * nameScale
                    );
                    Main.Font.DrawString(spriteBatch, DisplayName, namePos, Color.White * healthBarAlpha, nameScale);
                }

                float healthPercent = MathHelper.Clamp((float)Health / MaxHealth, 0f, 1f);
                int fillTotalWidth = barWidth - 4;

                Color drawColor = Color.White * healthBarAlpha;

                Rectangle srcRedFill = new Rectangle(3 * BORDER_FRAME_WIDTH + FILL_FRAME_WIDTH, 0, FILL_FRAME_WIDTH, FRAME_HEIGHT);
                spriteBatch.Draw(HealthbarTexture, new Rectangle(barX + 2, barY, fillTotalWidth, FRAME_HEIGHT), srcRedFill, drawColor, 0f, Vector2.Zero, SpriteEffects.None, 0.91f);

                int whiteWidth = (int)(fillTotalWidth * displayedHealthPercent);
                if (whiteWidth > 0)
                {
                    Rectangle srcWhiteFill = new Rectangle(3 * BORDER_FRAME_WIDTH + 2 * FILL_FRAME_WIDTH, 0, FILL_FRAME_WIDTH, FRAME_HEIGHT);
                    spriteBatch.Draw(HealthbarTexture, new Rectangle(barX + 2, barY, whiteWidth, FRAME_HEIGHT), srcWhiteFill, drawColor, 0f, Vector2.Zero, SpriteEffects.None, 0.92f);
                }

                int fillWidth = (int)(fillTotalWidth * healthPercent);
                if (fillWidth > 0)
                {
                    Rectangle srcFill = new Rectangle(3 * BORDER_FRAME_WIDTH, 0, FILL_FRAME_WIDTH, FRAME_HEIGHT);
                    spriteBatch.Draw(HealthbarTexture, new Rectangle(barX + 2, barY, fillWidth, FRAME_HEIGHT), srcFill, drawColor, 0f, Vector2.Zero, SpriteEffects.None, 0.93f);
                }

                Rectangle srcLeft = new Rectangle(LEFT_FRAME * BORDER_FRAME_WIDTH, 0, BORDER_FRAME_WIDTH, FRAME_HEIGHT);
                spriteBatch.Draw(HealthbarTexture, new Rectangle(barX, barY, leftWidth, FRAME_HEIGHT), srcLeft, drawColor, 0f, Vector2.Zero, SpriteEffects.None, 0.94f);

                if (midWidth > 0)
                {
                    Rectangle srcMid = new Rectangle(MID_FRAME * BORDER_FRAME_WIDTH, 0, BORDER_FRAME_WIDTH, FRAME_HEIGHT);
                    spriteBatch.Draw(HealthbarTexture, new Rectangle(barX + leftWidth, barY, midWidth, FRAME_HEIGHT), srcMid, drawColor, 0f, Vector2.Zero, SpriteEffects.None, 0.95f);
                }

                Rectangle srcRight = new Rectangle(RIGHT_FRAME * BORDER_FRAME_WIDTH, 0, BORDER_FRAME_WIDTH, FRAME_HEIGHT);
                spriteBatch.Draw(HealthbarTexture, new Rectangle(barX + leftWidth + midWidth, barY, rightWidth, FRAME_HEIGHT), srcRight, drawColor, 0f, Vector2.Zero, SpriteEffects.None, 0.96f);
            }
        }

        public bool Collides(Rectangle otherHitbox)
        {
            Rectangle bounds = Hitbox();

            if (!bounds.Intersects(otherHitbox))
                return false;

            return true;
        }

        public Rectangle TextureHitbox()
        {
            int frameHeight = Texture.Height / TotalFrames;
            Vector2 origin = new Vector2(Texture.Width / 2f, frameHeight / 2f);
            float width = Texture.Width * Scale;
            float height = frameHeight * Scale;
            Vector2 center = TexturePosition;
            float cos = (float)Math.Cos(Rotation);
            float sin = (float)Math.Sin(Rotation);
            Vector2[] corners = new Vector2[4];
            corners[0] = new Vector2(-width / 2, -height / 2);
            corners[1] = new Vector2(width / 2, -height / 2);
            corners[2] = new Vector2(width / 2, height / 2);
            corners[3] = new Vector2(-width / 2, height / 2);
            for (int i = 0; i < 4; i++)
            {
                float x = corners[i].X * cos - corners[i].Y * sin;
                float y = corners[i].X * sin + corners[i].Y * cos;
                corners[i] = new Vector2(x, y) + center;
            }
            float minX = corners[0].X, maxX = corners[0].X, minY = corners[0].Y, maxY = corners[0].Y;
            for (int i = 1; i < 4; i++)
            {
                minX = Math.Min(minX, corners[i].X);
                maxX = Math.Max(maxX, corners[i].X);
                minY = Math.Min(minY, corners[i].Y);
                maxY = Math.Max(maxY, corners[i].Y);
            }
            return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }

        private Vector2[] GetRotatedRectangleCorners(Vector2 center, float width, float height, float rotation)
        {
            Vector2[] corners = new Vector2[4];

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            corners[0] = new Vector2(-halfWidth, -halfHeight);
            corners[1] = new Vector2(halfWidth, -halfHeight);
            corners[2] = new Vector2(halfWidth, halfHeight);
            corners[3] = new Vector2(-halfWidth, halfHeight);

            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);

            for (int i = 0; i < 4; i++)
            {
                Vector2 rotated = new Vector2(
                    corners[i].X * cos - corners[i].Y * sin,
                    corners[i].X * sin + corners[i].Y * cos
                );
                corners[i] = rotated + center;
            }

            return corners;
        }

        public Rectangle Hitbox()
        {
            Vector2 center = new Vector2(
                Position.X,
                Position.Y
            );

            Vector2[] corners = GetRotatedRectangleCorners(
                center,
                Texture.Width * Scale, // Apply Scale to width
                (Texture.Height / TotalFrames) * Scale, // Apply Scale to height
                Rotation
            );

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (Vector2 corner in corners)
            {
                minX = Math.Min(minX, corner.X);
                minY = Math.Min(minY, corner.Y);
                maxX = Math.Max(maxX, corner.X);
                maxY = Math.Max(maxY, corner.Y);
            }

            return new Rectangle(
                (int)minX,
                (int)minY,
                (int)(maxX - minX),
                (int)(maxY - minY)
            );
        }

        public Color GetColor()
        {
            if (hurtFlashTimer > 0)
            {
                float t = MathHelper.Clamp(hurtFlashTimer / HURT_TIME_VALUE, 0f, 1f);
                byte a = Color.A;
                byte r = (byte)MathHelper.Lerp(255, Color.R, 1f - t);
                byte g = (byte)MathHelper.Lerp(0, Color.G, 1f - t);
                byte b = (byte)MathHelper.Lerp(0, Color.B, 1f - t);
                return new Color(r, g, b, a);
            }
            return Color;
        }
    }
}