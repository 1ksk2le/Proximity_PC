using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Items
{
    public class Flintlock_Pistol : Item
    {
        private float smokeTimer = 0f;
        private Vector2 lastMuzzlePosition;

        public Flintlock_Pistol(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 5;
            Rarity = 2;
            Name = "Flintlock Pistol";
            Lore = "'Wild... wild... west...'";
            Type = "[Weapon - Gun]";
            Info = "Shoots a bullet";
            Value = 600;
            Damage = 6;
            UseTime = 0.55f;
            Knockback = 200f;
            ShootSpeed = 1200f;
        }

        public override void Update(float deltaTime, GameTime gameTime, Player player)
        {
            base.Update(deltaTime, gameTime, player);
            float weaponTip = 1.6f;
            Vector2 muzzlePosition = player.WeaponHitbox.Center.ToVector2() + new Vector2(
                (float)Math.Cos(player.WeaponHitboxRotation),
                (float)Math.Sin(player.WeaponHitboxRotation)
            ) * (player.WeaponHitbox.Height * weaponTip * (player.IsFacingLeft ? -1f : 1f));
            Vector2 playerCenter = player.Hitbox.Center.ToVector2();
            Vector2 intendedTarget = playerCenter + player.AttackDirection * 1000f;
            Vector2 correctedDirection = Vector2.Normalize(intendedTarget - muzzlePosition);
            float baseAngle = (float)Math.Atan2(correctedDirection.Y, correctedDirection.X);
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
                        false,
                        0,
                        null,
                        true,
                        0f
                    );
                }
            }
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
            DrawGunAttack(spriteBatch, gameTime, player, ShootSpeed / 1500f);
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
            float weaponTip = 1.25f;
            Vector2 spawnPosition = player.WeaponHitbox.Center.ToVector2() + new Vector2(
                (float)Math.Cos(player.WeaponHitboxRotation),
                (float)Math.Sin(player.WeaponHitboxRotation)
            ) * (player.WeaponHitbox.Height * weaponTip * (player.IsFacingLeft ? -1f : 1f));

            Vector2 playerCenter = player.Hitbox.Center.ToVector2();
            Vector2 intendedTarget = playerCenter + direction * 1000f;
            Vector2 correctedDirection = Vector2.Normalize(intendedTarget - spawnPosition);

            projectile.NewProjectile(1, 0, Damage, Knockback, ShootSpeed, 1f, spawnPosition, correctedDirection);

            const int muzzleParticles = 10;
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
                    true,
                    1,
                    null,
                    true,
                    angle
                );
            }

            lastMuzzlePosition = muzzleOrigin;
            smokeTimer = 2f;
        }
    }
}