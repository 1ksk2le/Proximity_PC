using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content.NPCs
{
    public class Slime : NPC
    {
        private int animationFrame;
        private float animationTimer;
        private const float FrameDuration = 0.12f;
        private float walkCycleTimer;

        private float aiTimer;
        private float dashDuration;
        private bool isDashing;
        private Vector2 dashDirection;

        private const float DashCooldown = 3f;
        private const float DashTime = 1f;
        private const float DashSpeed = 300f;
        private const float DashBounceHeight = 160f;

        private const float WalkSpeed = 50f;
        private const float WalkBounceHeight = 20f;
        private const float WalkAnimSpeed = 0.8f;

        private static readonly Random RandomColor = new Random();

        private float trailTimer; // Timer for particle trail

        public Slime(ContentManager contentManager, ParticleManager particleManager, FloatingTextManager floatingTextManager)
            : base(contentManager, particleManager, floatingTextManager)
        {
        }

        protected override void Initialize()
        {
            ID = 0;
            Name = "Slime";
            MaxHealth = 20;
            Damage = 10;
            Defense = 2;
            Knockback = 500f;
            KnockbackResistance = 0f;
            DetectRange = 600f;
            IsImmune = false;
            Color = new Color(RandomColor.Next(0, 255), RandomColor.Next(0, 255), RandomColor.Next(0, 255), 200) * 0.5f;
            TotalFrames = 6;
        }

        public override void Update(float deltaTime, Player player, IReadOnlyList<Projectile> projectiles)
        {
            base.Update(deltaTime, player, projectiles);
            animationTimer += deltaTime;
            if (animationTimer >= FrameDuration)
            {
                animationFrame = (animationFrame + 1) % TotalFrames;
                animationTimer -= FrameDuration;
            }

            aiTimer += deltaTime;
            bool isMoving = false;

            if (isDashing)
            {
                dashDuration += deltaTime;
                Position += dashDirection * DashSpeed * deltaTime;
                if (dashDuration >= DashTime)
                {
                    isDashing = false;
                    aiTimer = 0f;
                    particle.NewParticle(
                        7,
                        new Rectangle((int)TextureHitbox().Center.X, (int)TextureHitbox().Center.Y, 0, 0),
                        Vector2.Zero,
                        0.1f,
                        Color * 1f,
                        Color * 0.8f,
                        1.25f,
                        15f,
                        (int)DrawLayer.BelowPlayer,
                        false,
                        0,
                        null,
                        true,
                        0f
                    );
                }
                isMoving = true;
            }
            else if (aiTimer >= DashCooldown && player != null && (walkCycleTimer < 0.05f || walkCycleTimer > 0.95f))
            {
                Vector2 direction = player.Position - Position;
                if (direction != Vector2.Zero)
                    direction.Normalize();
                dashDirection = direction;
                isDashing = true;
                dashDuration = 0f;
            }
            else if (!isDashing && player != null)
            {
                float distance = Vector2.Distance(Position, player.Position);
                if (distance <= DetectRange)
                {
                    Vector2 direction = player.Position - Position;
                    if (direction != Vector2.Zero)
                        direction.Normalize();
                    Position += direction * WalkSpeed * deltaTime;
                    isMoving = true;
                    trailTimer += deltaTime;
                    if (trailTimer >= 0.75f)
                    {
                        trailTimer = 0f;
                        particle.NewParticle(
                            7,
                            new Rectangle((int)TextureHitbox().Center.X, (int)TextureHitbox().Center.Y, 0, 0),
                            Vector2.Zero,
                            0.1f,
                            Color * 1f,
                            Color * 0.8f,
                            1f,
                            15f,
                            (int)DrawLayer.BelowPlayer,
                            false,
                            0,
                            null,
                            true,
                            0f
                        );
                    }
                }
            }

            if (isDashing)
            {
                float dashProgress = dashDuration / DashTime;
                float dashBobOffset = (float)Math.Sin(Math.PI * dashProgress) * DashBounceHeight;
                TexturePosition = new Vector2(Position.X, Position.Y - dashBobOffset);
            }
            else if (isMoving)
            {
                walkCycleTimer = (walkCycleTimer + deltaTime * WalkAnimSpeed) % 1f;
                float walkBobOffset = Math.Abs((float)Math.Sin(walkCycleTimer * MathHelper.TwoPi)) * WalkBounceHeight;
                TexturePosition = new Vector2(Position.X, Position.Y - walkBobOffset);
            }
            else
            {
                walkCycleTimer = 0f;
                TexturePosition = Position;
                trailTimer = 0f;
            }
        }

        public override void DrawShadows(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.DrawShadows(spriteBatch, gameTime);

            int frameHeight = Texture.Height / TotalFrames;
            float jumpProgress = 0f;
            float walkBobOffset = 0f;
            if (isDashing)
            {
                float dashProgress = dashDuration / DashTime;
                walkBobOffset = (float)Math.Sin(Math.PI * dashProgress) * DashBounceHeight;
                jumpProgress = walkBobOffset / DashBounceHeight;
            }
            else if (walkCycleTimer > 0f)
            {
                walkBobOffset = Math.Abs((float)Math.Sin(walkCycleTimer * MathHelper.TwoPi)) * WalkBounceHeight;
                jumpProgress = walkBobOffset / WalkBounceHeight;
            }

            float shadowScale = 0.9f - 0.3f * jumpProgress;
            float shadowAlpha = 0.5f - (jumpProgress * 0.2f); ;
            int shadowWidth = (int)(Texture.Width * Scale * 1.2f * shadowScale);
            int shadowHeight = (int)(Texture.Width * Scale * 0.6f * shadowScale);
            Vector2 shadowPos = new Vector2(Position.X - shadowWidth / 2f, Position.Y + frameHeight / 2f - shadowHeight / 2f);
            spriteBatch.Draw(Main.Shadow, new Rectangle((int)shadowPos.X, (int)shadowPos.Y, shadowWidth, shadowHeight), null, Color.Black * shadowAlpha, 0f, Vector2.Zero, SpriteEffects.None, 0.01f);
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.PreDraw(spriteBatch, gameTime);
            int frameHeight = Texture.Height / TotalFrames;
            Rectangle sourceRect = new Rectangle(0, animationFrame * frameHeight, Texture.Width, frameHeight);
            Vector2 origin = new Vector2(Texture.Width / 2f, frameHeight / 2f);

            float walkBobOffset = 0f;
            float jumpProgress = 0f;
            float scale = 1f;
            if (isDashing)
            {
                float dashProgress = dashDuration / DashTime;
                walkBobOffset = (float)Math.Sin(Math.PI * dashProgress) * DashBounceHeight;
                jumpProgress = walkBobOffset / DashBounceHeight;
                scale = 1f + 0.65f * jumpProgress;
            }
            else if (walkCycleTimer > 0f)
            {
                walkBobOffset = Math.Abs((float)Math.Sin(walkCycleTimer * MathHelper.TwoPi)) * WalkBounceHeight;
                jumpProgress = walkBobOffset / WalkBounceHeight;
                scale = 1f + 0.15f * jumpProgress;
            }

            spriteBatch.Draw(Texture, TexturePosition, sourceRect, GetColor(), 0f, origin, scale * Scale, SpriteEffects.None, 0f);
        }

        protected override void Hurt(int damage, Vector2 knockbackDirection, float knockback = 0f, float knockbackResistance = 0f)
        {
            base.Hurt(damage, knockbackDirection, knockback, knockbackResistance);
            for (int i = 0; i < 2; i++)
            {
                Random num1 = new Random();
                particle.NewParticle(
                    7,
                    new Rectangle((int)TextureHitbox().Center.X - num1.Next(-Texture.Width, Texture.Width), (int)TextureHitbox().Center.Y - num1.Next(-Texture.Height / TotalFrames, Texture.Height / TotalFrames), 0, 0),
                    Vector2.Zero,
                    0.1f,
                    Color * 1f,
                    Color * 0.8f,
                    num1.NextFloat(0.6f, 0.8f),
                    25f,
                    (int)DrawLayer.BelowPlayer,
                    false,
                    0,
                    null,
                    true,
                    0f
                );
            }
        }

        public override void Kill()
        {
            base.Kill();
            for (int i = 0; i < 4; i++)
            {
                Random num1 = new Random();
                particle.NewParticle(
                    7,
                    new Rectangle((int)TextureHitbox().Center.X - num1.Next(-Texture.Width, Texture.Width), (int)TextureHitbox().Center.Y - num1.Next(-Texture.Height / TotalFrames, Texture.Height / TotalFrames), 0, 0),
                    Vector2.Zero,
                    0.1f,
                    Color * 1f,
                    Color * 0.8f,
                    num1.NextFloat(0.9f, 1.2f),
                    25f,
                    (int)DrawLayer.BelowPlayer,
                    false,
                    0,
                    null,
                    true,
                    0f
                );
            }
        }
    }
}