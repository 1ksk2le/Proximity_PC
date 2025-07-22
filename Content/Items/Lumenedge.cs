using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Items
{
    public class Lumenedge : Item
    {
        public Lumenedge(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 9;
            Rarity = 3;
            Name = "Lumenedge";
            Lore = "'Souls spiral it's sharp edge'";
            Type = "[Weapon - Sword]";
            Info = "TBA";
            Value = 1200;
            Damage = 14;
            Knockback = 400f;
            UseTime = 0.6f;
            ShootSpeed = 200f;
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
            float swordLength = player.WeaponHitbox.Height * 0.7f;
            float angle = player.WeaponHitboxRotation;
            Vector2 dir = new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
            dir *= ((player.IsFacingLeft && player.IsAttacking) ? -1 : 1);
            float upwardOffset = 30f;
            Vector2 basePos = player.WeaponHitbox.Center.ToVector2() + dir * upwardOffset;

            int helixPoints = 26;
            float helixRadius = 25f;
            float time = (float)gameTime.TotalGameTime.TotalSeconds;

            for (int h = 0; h < 2; h++)
            {
                float phase = h == 0 ? 0 : MathF.PI;
                Color startColor = h == 0 ? new Color(0, 226, 189, 110) : Color.DarkTurquoise;
                Color endColor = h == 0 ? new Color(149, 33, 77, 220) : Color.LightPink;

                for (int i = 0; i < helixPoints; i++)
                {
                    float t = i / (float)(helixPoints - 1);
                    float along = t * swordLength;
                    float currentRadius = MathHelper.Lerp(helixRadius, helixRadius * 0.3f, t);
                    float helixAngle = t * MathF.PI * 3 + phase + time * 4f;
                    float x = (float)Math.Cos(helixAngle) * currentRadius;
                    float y = along - swordLength / 2f;
                    float z = (float)Math.Sin(helixAngle) * currentRadius * 0.5f;

                    Vector2 alongSword = basePos + dir * y;
                    Vector2 normal = new Vector2(dir.Y, -dir.X);
                    Vector2 pos = alongSword + normal * x + dir * z * 0.2f;

                    Color color = Color.Lerp(startColor, endColor, t);
                    float scale = MathHelper.Lerp(0.4f, 0.1f, t);

                    var p = particle.NewParticle(
                        1,
                        new Rectangle((int)pos.X, (int)pos.Y, 1, 1),
                        new Vector2(0, -100),
                        0.1f,
                        color,
                        color,
                        scale * player.CurrentScale,
                        0.25f + 0.1f * t,
                        (int)DrawLayer.BelowPlayer,
                        false,
                        1,
                        player,
                        true,
                        0f
                    );
                }
            }
        }
    }
}