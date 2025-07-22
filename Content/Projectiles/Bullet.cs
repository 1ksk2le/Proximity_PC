using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content.Projectiles
{
    public class Bullet : Projectile
    {
        private readonly List<Vector2> positionHistory = new();
        private const int MaxAfterimages = 35;
        private const float FadeOutStartTime = 1f;

        public Color startColor = Color.Khaki;
        public Color endColor = Color.OrangeRed;

        private Vector2 previousPosition;
        private const int AfterimageSpacing = 5;

        public Bullet(ContentManager contentManager, ParticleManager particleManager)
            : base(contentManager, particleManager)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            ID = 1;
            Name = "Bullet";
            positionHistory.Clear();
            previousPosition = Vector2.Zero; // Reset to neutral state
            Scale = 1f;
        }

        public override void Update(float deltaTime, Player player)
        {
            base.Update(deltaTime, player);

            if (previousPosition == Vector2.Zero)
            {
                previousPosition = Position;
                return;
            }

            float distance = Vector2.Distance(previousPosition, Position);
            if (distance > AfterimageSpacing)
            {
                int steps = (int)(distance / AfterimageSpacing);
                for (int s = 1; s <= steps; s++)
                {
                    float lerp = (float)s / (steps + 1);
                    Vector2 interpPos = Vector2.Lerp(previousPosition, Position, lerp);
                    positionHistory.Add(interpPos);
                }
            }
            positionHistory.Add(Position);
            previousPosition = Position;
            if (positionHistory.Count > MaxAfterimages)
                positionHistory.RemoveAt(0);
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            if (Texture != null && positionHistory.Count > 0)
            {
                float lifeTimeRatio = CurrentLifeTime / LifeTime;
                int startIdx = 0;
                if (lifeTimeRatio > FadeOutStartTime)
                {
                    float progress = (lifeTimeRatio - FadeOutStartTime) / (1f - FadeOutStartTime);
                    int removeCount = (int)(progress * positionHistory.Count);
                    startIdx = Math.Min(removeCount, positionHistory.Count - 1);
                }

                for (int i = positionHistory.Count - 1; i >= startIdx; i--)
                {
                    var pos = positionHistory[i];
                    float t = (float)(positionHistory.Count - 1 - i) / positionHistory.Count;
                    float alpha = 1f - (0.1f + 0.9f * t);
                    float scale = (1f - (0.5f + 0.5f * t)) * Scale;
                    Color trailColor = Color.Lerp(startColor, endColor, t) * alpha;
                    spriteBatch.Draw(Texture,
                        pos,
                        null,
                        trailColor,
                        Rotation,
                        new Vector2(Texture.Width / 2f, Texture.Height / 2f),
                        scale,
                        SpriteEffects.None,
                        0f);
                }
            }
        }

        public override void DrawShadow(SpriteBatch spriteBatch, GameTime gameTime)
        {
            return;
        }

        public override void Kill()
        {
            const int killParticles = 10;
            float coneAngle = MathHelper.ToRadians(30f);
            Vector2 origin = Position;
            float baseAngle = (float)Math.Atan2(Direction.Y, Direction.X);

            for (int i = 0; i < killParticles; i++)
            {
                float angle = baseAngle + random.NextFloat(-coneAngle / 2f, coneAngle / 2f);
                float speed = random.NextFloat(10f, 75f);
                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * -speed;

                particle.NewParticle(
                    4,
                    new Rectangle((int)origin.X, (int)origin.Y, 0, 0),
                    velocity,
                    0.1f,
                    Color.Khaki,
                    Color.OrangeRed,
                    random.NextFloat(0.5f, 0.8f) * Scale,
                    random.NextFloat(1.5f, 25f),
                    (int)DrawLayer.AbovePlayer,
                    true,
                    1,
                    null,
                    true,
                    angle
                );
            }
            positionHistory.Clear();
            previousPosition = Vector2.Zero; // Reset to neutral state
            base.Kill();
        }
    }
}