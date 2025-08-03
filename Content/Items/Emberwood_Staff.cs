using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Items
{
    public class Emberwood_Staff : Item
    {
        public Emberwood_Staff(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 6;
            Rarity = 2;
            Name = "Emberwood Staff";
            Lore = "'Sizzle coming from this staff is music to your ears'";
            Type = "[Weapon - Staff]";
            Info = "Casts a fiery bolt";
            Value = 450;
            Damage = 10;
            UseTime = 0.8f;
            Knockback = 300f;
            ShootSpeed = 900f;
        }

        public override void Update(float deltaTime, GameTime gameTime, Player player)
        {
            base.Update(deltaTime, gameTime, player);
            float weaponRotation = player.WeaponHitboxRotation - MathHelper.PiOver2;
            Vector2 velocityDir = new Vector2(
                (float)Math.Cos(weaponRotation),
                (float)Math.Sin(weaponRotation)
            );
            Rectangle spawnArea = player.WeaponHitbox;
            spawnArea = new Rectangle(
                spawnArea.X,
                spawnArea.Y,
                spawnArea.Width,
                (int)(spawnArea.Height * 0.25f)
            );
            if (player.IsAttacking && random.Next(100) < 50)
            {
                Vector2 weaponTip = spawnArea.Center.ToVector2() + velocityDir * spawnArea.Height * 0.5f;
                for (int i = 0; i < 1; i++)
                {
                    var p = particle.NewParticle(
                            4,
                            new Rectangle((int)weaponTip.X - 5, (int)weaponTip.Y + 10, 10, 10),
                            new Vector2(random.Next(-5, 5), random.Next(-70, -40)),
                            0.1f,
                            new Color(255, 70, 0, 0),
                            new Color(255, 100, 20, 0),
                            0.8f * player.CurrentScale,
                            2f,
                            (int)DrawLayer.BelowPlayer
                    );
                }
            }
            if (random.Next(100) < 20)
            {
                Vector2 weaponTip = spawnArea.Center.ToVector2() + velocityDir * spawnArea.Height * 0.5f;
                for (int i = 0; i < 2; i++)
                {
                    var p = particle.NewParticle(
                    4,
                    new Rectangle((int)weaponTip.X - 5, (int)weaponTip.Y + 10, 10, 10),
                    new Vector2(random.Next(-10, 10), random.Next(-70, -40)),
                    0.5f,
                    new Color(255, 70, 0, 0),
                    new Color(255, 100, 20, 0),
                    random.NextFloat(.4f, .8f) * player.CurrentScale,
                    2f,
                    (int)DrawLayer.BelowPlayer
                    );
                    p.NoGravity = false;
                }
            }
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            base.PreDraw(spriteBatch, gameTime, player, drawLayer);
            DrawStaffAttack(spriteBatch, gameTime, player, drawLayer);
            DrawStaffIdle(spriteBatch, gameTime, player, drawLayer);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            base.PostDraw(spriteBatch, gameTime, player, drawLayer);
            float pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.1f + 0.4f;
            float scalePulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.1f + 0.8f;

            Rectangle bloomRect = new Rectangle(
                (int)((player.WeaponHitbox.X + (player.WeaponHitbox.Width / 2) - (int)(50 * scalePulse))),
                (int)((player.WeaponHitbox.Y - (int)(30 * scalePulse))),
                (int)(100 * scalePulse * player.CurrentScale),
                (int)(100 * scalePulse * player.CurrentScale)
            );
            spriteBatch.Draw(Main.Bloom, bloomRect, null, new Color(200, 100, 0, 0) * pulse, 0f, Vector2.Zero, SpriteEffects.None, drawLayer);
        }

        public override void Use(float deltaTime, Player player, Vector2 direction)
        {
            base.Use(deltaTime, player, direction);
            float weaponRotation = player.WeaponHitboxRotation - MathHelper.PiOver2;
            Vector2 velocityDir = new Vector2(
                (float)Math.Cos(weaponRotation),
                (float)Math.Sin(weaponRotation)
            );

            Vector2 spawnPosition = player.WeaponHitbox.Center.ToVector2() + velocityDir * player.WeaponHitbox.Height * 0.5f;

            Vector2 playerCenter = player.Hitbox.Center.ToVector2();
            Vector2 intendedTarget = playerCenter + direction * 1000f;
            Vector2 correctedDirection = Vector2.Normalize(intendedTarget - spawnPosition);

            projectile.NewProjectile(0, 0, Damage, Knockback, ShootSpeed, 1f, spawnPosition, correctedDirection);
        }
    }
}