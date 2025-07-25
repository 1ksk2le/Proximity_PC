using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Items
{
    public class Makeshift_Shotgun : Item
    {
        private float smokeTimer = 0f;
        private Vector2 lastMuzzlePosition;

        public Makeshift_Shotgun(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 14;
            Rarity = 3;
            Name = "Makeshift Shotgun";
            Lore = "'It's a miracle that this thing is working'";
            Type = "[Weapon - Rifle]";
            Info = "Shoots a range of bullets that will knock you back";
            Value = 1150;
            Damage = 8;
            UseTime = 1f;
            Knockback = 250f;
            ShootSpeed = 1500f;
            Recoil = 0.6f;
        }

        public override void Update(float deltaTime, GameTime gameTime, Player player)
        {
            base.Update(deltaTime, gameTime, player);
            Vector2 muzzlePosition = player.WeaponHitbox.GetCenter() + new Vector2(
                (float)Math.Cos(player.WeaponHitboxRotation),
                (float)Math.Sin(player.WeaponHitboxRotation)
            ) * (player.WeaponHitbox.Width * 0.5f * (player.IsFacingLeft ? -1f : 1f));
            lastMuzzlePosition = muzzlePosition;

            if (smokeTimer > 0f && !player.IsAttacking && random.Next(2) == 0)
            {
                smokeTimer -= deltaTime;
                const int smokeParticles = 1;
                for (int i = 0; i < smokeParticles; i++)
                {
                    particle.NewParticle(
                        4,
                        new Rectangle((int)lastMuzzlePosition.X, (int)lastMuzzlePosition.Y, 0, 0),
                        new Vector2(0, random.NextFloat(-50f, -10f)),
                        0.5f,
                        Color.WhiteSmoke * 0.5f,
                        Color.DimGray,
                        random.NextFloat(0.2f, 0.8f),
                        random.NextFloat(2f, 4f),
                        (int)DrawLayer.AbovePlayer,
                        0,
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

            const int projectileCount = 4;
            float spreadAngle = MathHelper.ToRadians(15f);

            for (int i = 0; i < projectileCount; i++)
            {
                float randomAngle = random.NextFloat(-spreadAngle, spreadAngle);
                Vector2 spreadDirection = Vector2.Normalize(new Vector2(
                    (float)Math.Cos(randomAngle) * correctedDirection.X - (float)Math.Sin(randomAngle) * correctedDirection.Y,
                    (float)Math.Sin(randomAngle) * correctedDirection.X + (float)Math.Cos(randomAngle) * correctedDirection.Y
                ));

                projectile.NewProjectile(1, 0, Damage, Knockback, ShootSpeed + random.NextFloat(-150f, 150f), 0.5f, spawnPosition, spreadDirection);
            }

            const int muzzleParticles = 20;
            float muzzleConeAngle = MathHelper.ToRadians(30f);
            Vector2 muzzleOrigin = spawnPosition;
            float baseAngle = (float)Math.Atan2(correctedDirection.Y, correctedDirection.X);

            for (int i = 0; i < muzzleParticles; i++)
            {
                float angle = baseAngle + random.NextFloat(-muzzleConeAngle / 2f, muzzleConeAngle / 2f);
                float speed = random.NextFloat(10f, 100f);
                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

                particle.NewParticle(
                    4,
                    new Rectangle((int)muzzleOrigin.X, (int)muzzleOrigin.Y, 0, 0),
                    velocity,
                    0.1f,
                    Color.Khaki,
                    Color.OrangeRed,
                    random.NextFloat(0.5f, 0.8f),
                    random.NextFloat(1.5f, 25f),
                    (int)DrawLayer.AbovePlayer,
                    1,
                    player,
                    true,
                    angle
                );
                particle.NewParticle(
                    4,
                    new Rectangle((int)muzzleOrigin.X, (int)muzzleOrigin.Y, 0, 0),
                    velocity,
                    0.4f,
                    new Color(255, 70, 0, 100),
                    Color.DimGray,
                    random.NextFloat(0.2f, 1.5f),
                    random.NextFloat(2f, 4f),
                    (int)DrawLayer.AbovePlayer,
                    0,
                    player,
                    true,
                    angle
                );
            }
            particle.NewParticle(
                    8,
                    new Rectangle(player.WeaponHitbox.Center.X, player.WeaponHitbox.Center.Y, 0, 0),
                    Vector2.Zero,
                    1f,
                    Color.White,
                    Color.White,
                    0.6f,
                    30f,
                    (int)DrawLayer.BelowPlayer,
                    2,
                    player,
                    true,
                    0f
                );
            lastMuzzlePosition = muzzleOrigin;
            smokeTimer = 2f;
        }
    }
}