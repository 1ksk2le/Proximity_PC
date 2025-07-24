using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Items
{
    public class Squirt_Toy : Item
    {
        private float smokeTimer = 0f;
        private Vector2 lastMuzzlePosition;

        public Squirt_Toy(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 15;
            Rarity = 2;
            Name = "Squirt Toy";
            Lore = "'It can be deadly when wielded by the one who lives in Polatli'";
            Type = "[Weapon - Pistol]";
            Info = "Shoots a bullet";
            Value = 2000;
            Damage = 2;
            UseTime = 0.1f;
            Knockback = 50f;
            ShootSpeed = 500f;
            Recoil = 0f;
        }

        public override void Update(float deltaTime, GameTime gameTime, Player player)
        {
            base.Update(deltaTime, gameTime, player);
            Vector2 muzzlePosition = player.WeaponHitbox.GetCenter() + new Vector2(
                (float)Math.Cos(player.WeaponHitboxRotation),
                (float)Math.Sin(player.WeaponHitboxRotation)
            ) * (player.WeaponHitbox.Width * 0.5f * (player.IsFacingLeft ? -1f : 1f));
            lastMuzzlePosition = muzzlePosition;

            if (smokeTimer > 0f && !player.IsAttacking && random.Next(10) == 0)
            {
                smokeTimer -= deltaTime;
                const int smokeParticles = 1;
                for (int i = 0; i < smokeParticles; i++)
                {
                    particle.NewParticle(
                        4,
                        new Rectangle((int)lastMuzzlePosition.X, (int)lastMuzzlePosition.Y, 0, 0),
                        new Vector2(0, random.NextFloat(75f, 30f)),
                        0.5f,
                        Color.CornflowerBlue,
                        Color.CornflowerBlue * 0.3f,
                        random.NextFloat(0.2f, 0.8f),
                        random.NextFloat(4f, 8f),
                        (int)DrawLayer.BelowPlayer,
                        false,
                        4,
                        player,
                        true,
                        0f
                    );
                }
            }
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
            DrawGunAttack(spriteBatch, gameTime, player);
            DrawGunIdle(spriteBatch, gameTime, player);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PostDraw(spriteBatch, gameTime, player);
        }

        public override void Use(float deltaTime, Player player, Vector2 direction)
        {
            base.Use(deltaTime, player, direction);
            float weaponRotation = player.WeaponHitboxRotation - MathHelper.PiOver2;
            Vector2 velocityDir = new Vector2(
                (float)Math.Cos(weaponRotation),
                (float)Math.Sin(weaponRotation)
            );
            Vector2 spawnPosition = player.WeaponHitbox.Center.ToVector2() + new Vector2(
                (float)Math.Cos(player.WeaponHitboxRotation),
                (float)Math.Sin(player.WeaponHitboxRotation)
            ) * (player.WeaponHitbox.Width * 0.5f * (player.IsFacingLeft ? -1f : 1f));

            Vector2 playerCenter = player.Hitbox.Center.ToVector2();
            Vector2 intendedTarget = playerCenter + direction * 1000f;
            Vector2 correctedDirection = Vector2.Normalize(intendedTarget - spawnPosition);
            float maxAngle = MathHelper.ToRadians(30f);
            float randomAngle = (float)(random.NextDouble() * 2 * maxAngle - maxAngle);
            float cos = (float)Math.Cos(randomAngle);
            float sin = (float)Math.Sin(randomAngle);
            Vector2 rotatedDirection = new Vector2(
                correctedDirection.X * cos - correctedDirection.Y * sin,
                correctedDirection.X * sin + correctedDirection.Y * cos
            );
            rotatedDirection.Normalize();

            projectile.NewProjectile(3, 1, Damage, Knockback, ShootSpeed * random.NextFloat(0.8f, 1.2f), random.NextFloat(0.3f, 1.2f), spawnPosition, rotatedDirection);

            const int muzzleParticles = 5;
            float muzzleConeAngle = MathHelper.ToRadians(20f);
            Vector2 muzzleOrigin = spawnPosition;
            float baseAngle = (float)Math.Atan2(correctedDirection.Y, correctedDirection.X);

            for (int i = 0; i < muzzleParticles; i++)
            {
                float angle = baseAngle + random.NextFloat(-muzzleConeAngle / 2f, muzzleConeAngle / 2f);
                float speed = random.NextFloat(30f, 70f);
                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

                particle.NewParticle(
                    4,
                    new Rectangle((int)muzzleOrigin.X, (int)muzzleOrigin.Y, 0, 0),
                    velocity + new Vector2(0, -25),
                    0.4f,
                    Color.CornflowerBlue,
                    Color.CornflowerBlue * 0.3f,
                    random.NextFloat(0.2f, 0.8f),
                    random.NextFloat(8f, 10f),
                    (int)DrawLayer.BelowPlayer,
                    false,
                    3,
                    player,
                    true,
                    angle
                );
            }

            lastMuzzlePosition = muzzleOrigin;
            smokeTimer = 2f;
        }
    }
}