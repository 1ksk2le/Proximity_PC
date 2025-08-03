using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Projectiles
{
    public class Fireball : Projectile
    {
        public Fireball(ContentManager contentManager, ParticleManager particleManager)
            : base(contentManager, particleManager)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            ID = 0;
            Name = "Fireball";
            Scale = 1f;
            Penetrate = 1;
        }

        public override void Update(float deltaTime, Player player)
        {
            base.Update(deltaTime, player);
            if (random.Next(2) == 0)
            {
                particle.NewParticle(
                4,
                Hitbox(),
                Vector2.Zero,
                0.4f,
                new Color(255, 70, 0, 0),
                Color.DimGray,
                random.NextFloat(0.2f, 1.5f) * Scale,
                random.NextFloat(2f, 4f),
                (int)DrawLayer.AbovePlayer,
                0,
                null,
                true,
                0f
                );
            }

            var p = particle.NewParticle(
                1,
                Hitbox(),
                Vector2.Zero,
                0.1f,
                new Color(100, 100, 100, 0),
                new Color(255, 70, 0, 0),
                random.NextFloat(0.8f, 1.4f) * Scale,
                1f,
                (int)DrawLayer.AbovePlayer,
                1,
                this,
                true,
                0f
            );
            particle.NewParticle(
                4,
                Hitbox(),
                Vector2.Zero,
                0.6f,
                new Color(255, 70, 0, 0),
                new Color(100, 100, 100, 0),
                random.NextFloat(0.8f, 1.2f) * Scale,
                1.5f,
                (int)DrawLayer.AbovePlayer,
                1,
                this,
                true,
                0f
            );
            particle.NewParticle(
                1,
                Hitbox(),
                Vector2.Zero,
                0.1f,
                new Color(255, 70, 0, 0),
                new Color(100, 100, 100, 0),
                random.NextFloat(0.4f, 0.6f) * Scale,
                1f,
                (int)DrawLayer.AbovePlayer,
                1,
                this,
                true,
                0f
            );
            particle.NewParticle(
                6,
                Hitbox(),
                Vector2.Zero,
                0.8f,
                new Color(255, 70, 0, 0),
                Color.DimGray,
                random.NextFloat(1.1f, 1.5f) * Scale,
                0.3f,
                (int)DrawLayer.AbovePlayer,
                0,
                this,
                true,
                0f
            );
        }

        public override void DrawShadow(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            return;
        }

        public override void Kill()
        {
            for (int i = 0; i < 10; i++)
            {
                particle.NewParticle(
                    4,
                    Hitbox(),
                    new Vector2(random.Next(-25, 25), random.Next(-25, 25)),
                    0.8f,
                    new Color(255, 70, 0, 0),
                    Color.DimGray,
                    random.NextFloat(0.2f, 1.5f) * Scale,
                    random.NextFloat(2f, 4f),
                    (int)DrawLayer.AbovePlayer,
                    0,
                    null,
                    true,
                    0f
                );
                particle.NewParticle(
                    6,
                    Hitbox(),
                    new Vector2(random.Next(-50, 50), random.Next(-50, 50)),
                    0.1f,
                    new Color(255, 70, 0, 0),
                    new Color(255, 100, 20, 0),
                    random.NextFloat(0.6f, 1.0f) * Scale,
                    0.8f,
                    (int)DrawLayer.AbovePlayer,
                    0,
                    null,
                    true,
                    0f
                );
            }
            base.Kill();
        }
    }
}