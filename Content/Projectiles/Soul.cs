using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content.Projectiles
{
    public class Soul : Projectile
    {
        private float distanceTraveled = 0f;
        private float wigglePhase = 0f;
        private bool phaseInitialized = false;
        private float waveAmplitude = 300f;
        private float waveFrequency = 0.01f;
        private static readonly Random waveRandom = new Random();
        private Vector2 waveDirection = Vector2.Zero;

        public Soul(ContentManager contentManager, ParticleManager particleManager)
            : base(contentManager, particleManager)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            AI = -1;
            ID = 2;
            Name = "Soul";
            Penetrate = 1;
        }

        public override void Update(float deltaTime, Player player)
        {
            base.Update(deltaTime, player);

            if (!phaseInitialized)
            {
                wigglePhase = (float)(waveRandom.NextDouble() * MathHelper.TwoPi);
                Vector2 mainDir = Vector2.Normalize(Direction);
                float sign = waveRandom.Next(0, 2) == 0 ? -1f : 1f;
                float angle = (float)(waveRandom.NextDouble() * Math.PI * 2);
                Vector2 perp = new Vector2(-mainDir.Y, mainDir.X) * sign;
                waveDirection = Vector2.Transform(perp, Matrix.CreateRotationZ(angle));
                phaseInitialized = true;
            }
            Vector2 mainDir2 = Vector2.Normalize(Direction);
            float speed = Speed * deltaTime;
            Position += mainDir2 * speed;
            distanceTraveled += speed;

            float sineOffset = (float)Math.Sin(distanceTraveled * waveFrequency + wigglePhase) * waveAmplitude;
            Position += waveDirection * sineOffset * deltaTime;

            var p = particle.NewParticle(
                1,
                Hitbox(),
                Vector2.Zero,
                0.12f,
                new Color(0, 226, 189, 220),
                new Color(149, 33, 77, 220),
                0.9f * Scale,
                3f,
                (int)DrawLayer.AbovePlayer,
                1,
                this,
                true,
                0f
            );
        }

        public override void DrawShadow(SpriteBatch spriteBatch, GameTime gameTime, float drawLayer)
        {
            return;
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            float pulseTime = Main.Paused ? 0f : (float)gameTime.TotalGameTime.TotalSeconds * 2f;
            float pulse = (float)(0.5 + 0.5 * Math.Sin(pulseTime));
            Color colorA = new Color(0, 226, 189, 110);
            Color colorB = Color.DarkTurquoise;
            Color bloomColor = Color.Lerp(colorA, colorB, pulse);
            spriteBatch.Draw(Main.Bloom, Position - new Vector2(Main.Bloom.Width / 2f * Scale, Main.Bloom.Height / 2f * Scale), null, bloomColor, 0f, Vector2.Zero, Scale, SpriteEffects.None, drawLayer);
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
                    0.4f,
                    new Color(149, 33, 77, 220),
                    new Color(0, 226, 189, 220),
                    1.8f * Scale,
                    5f,
                    (int)DrawLayer.AbovePlayer,
                    1,
                    null,
                    true,
                    angle
                );
            }
            base.Kill();
        }
    }
}