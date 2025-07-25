using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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
                Color.CornflowerBlue,
                Color.CornflowerBlue * 0.3f,
                random.NextFloat(0.5f, 1f) * Scale,
                random.NextFloat(1f, 2f),
                (int)DrawLayer.BelowPlayer,
                0,
                player,
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
            for (int i = 0; i < 1; i++)
            {
                particle.NewParticle(
                    7,
                    new Rectangle((int)Hitbox().X, (int)Hitbox().Y, 0, 0),
                    Vector2.Zero,
                    0.5f,
                    Color.LightSkyBlue,
                    Color.LightSkyBlue * 0.8f,
                    random.NextFloat(0.3f, 0.8f) * Scale,
                    random.NextFloat(3f, 5f),
                    (int)DrawLayer.OnArena,
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