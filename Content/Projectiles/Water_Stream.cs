using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Projectiles
{
    public class Water_Stream : Projectile
    {
        public Water_Stream(ContentManager contentManager, ParticleManager particleManager)
            : base(contentManager, particleManager)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            ID = 3;
            Name = "Water Stream";
            Scale = 1f;
            Penetrate = 1;
        }

        public override void Update(float deltaTime, Player player)
        {
            base.Update(deltaTime, player);

            particle.NewParticle(
                4,
                new Rectangle(Hitbox().Center.X, Hitbox().Center.Y, 0, 0),
                Vector2.Zero,
                0.4f,
                Color.RoyalBlue,
                Color.RoyalBlue * 0.3f,
                random.NextFloat(0.5f, 1f) * Scale,
                random.NextFloat(0.5f, 1.2f),
                (int)DrawLayer.BelowPlayer,
                false,
                0,
                this,
                true,
                0f
                );
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
        }

        public override void DrawShadow(SpriteBatch spriteBatch, GameTime gameTime)
        {
            return;
        }

        public override void Kill()
        {
            const int killParticles = 3;
            float coneAngle = MathHelper.ToRadians(360f);
            Vector2 origin = Position;
            float baseAngle = (float)Math.Atan2(Direction.Y, Direction.X);

            for (int i = 0; i < killParticles; i++)
            {
                float angle = baseAngle + random.NextFloat(-coneAngle / 2f, coneAngle / 2f);
                float speed = random.NextFloat(10f, 35f);
                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * -speed;

                particle.NewParticle(
                    4,
                    new Rectangle((int)origin.X, (int)origin.Y, 0, 0),
                    velocity,
                    0.5f,
                    Color.RoyalBlue,
                    Color.RoyalBlue * 0.3f,
                    random.NextFloat(0.3f, 1.2f) * Scale,
                    random.NextFloat(1.5f, 4.5f),
                    (int)DrawLayer.BelowPlayer,
                    false,
                    4,
                    null,
                    true,
                    angle
                );
            }
            base.Kill();
        }
    }
}