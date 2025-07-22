using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Items
{
    public class Jewel_of_Nile : Item
    {
        public Jewel_of_Nile(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 8;
            Rarity = 2;
            Name = "Jewel of Nile";
            Lore = "'Beloved sword of the King Milliath himself'";
            Type = "[Weapon - Sword]";
            Info = "TBA";
            Value = 450;
            Damage = 12;
            UseTime = 0.8f;
            ShootSpeed = 180f;
            Knockback = 300f;
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
            DrawSwordAttack(spriteBatch, gameTime, player);
            DrawSwordIdle(spriteBatch, gameTime, player);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PostDraw(spriteBatch, gameTime, player);
        }

        public override void Update(float deltaTime, GameTime gameTime, Player player)
        {
            base.Update(deltaTime, gameTime, player);
            float weaponRotation = player.WeaponHitboxRotation - MathHelper.PiOver2;
            Vector2 velocityDir = new Vector2(
                (float)Math.Cos(weaponRotation),
                (float)Math.Sin(weaponRotation)
            );

            velocityDir *= ((player.IsFacingLeft && player.IsAttacking) ? -1 : 1);

            Rectangle spawnArea = player.WeaponHitbox;
            spawnArea = new Rectangle(
                spawnArea.X,
                spawnArea.Y,
                spawnArea.Width,
                (int)(spawnArea.Height * 0.75f)
            );

            if (random.Next(100) < 10)
            {
                var a = particle.NewParticle(
                    4,
                    spawnArea,
                    velocityDir * 200f,
                    0.1f,
                    new Color(181, 99, 33, 200),
                    new Color(181, 99, 33, 200),
                    random.NextFloat(0.2f, 0.8f) * player.CurrentScale,
                    2f,
                    (int)DrawLayer.BelowPlayer,
                    true,
                    1,
                    player,
                    true,
                    weaponRotation
                );
            }
            if (random.Next(100) < 10)
            {
                for (int i = 0; i < 1; i++)
                {
                    var p = particle.NewParticle(
                    4,
                    spawnArea,
                    velocityDir * 200f,
                    0.1f,
                    new Color(98, 158, 146, 200),
                    new Color(98, 158, 146, 200),
                    random.NextFloat(0.2f, 0.8f) * player.CurrentScale,
                    2f,
                    (int)DrawLayer.BelowPlayer,
                    true,
                    1,
                    player,
                    true,
                    weaponRotation
                    );
                }
            }
        }
    }
}