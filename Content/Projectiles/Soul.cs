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
                0.2f,
                new Color(0, 226, 189, 220),
                new Color(149, 33, 77, 220),
                0.9f * Scale,
                1.5f,
                (int)DrawLayer.AbovePlayer,
                false,
                1,
                this,
                true,
                0f
            );
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            return;
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            float pulseTime = Main.Paused ? 0f : (float)gameTime.TotalGameTime.TotalSeconds * 2f;
            float pulse = (float)(0.5 + 0.5 * Math.Sin(pulseTime));
            Color colorA = new Color(0, 226, 189, 110);
            Color colorB = Color.DarkTurquoise;
            Color bloomColor = Color.Lerp(colorA, colorB, pulse);
            spriteBatch.Draw(Main.Bloom, Position - new Vector2(Main.Bloom.Width / 2f * Scale, Main.Bloom.Height / 2f * Scale), null, bloomColor, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            base.PostDraw(spriteBatch, gameTime, player);
        }

        public override void Kill()
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                Vector2 velocity = new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 20f;
                var burst = particle.NewParticle(
                    4,
                    new Rectangle((int)Position.X - 5, (int)Position.Y - 5, 10, 10),
                    velocity,
                    0.75f,
                    new Color(149, 33, 77, 220),
                    new Color(0, 226, 189, 220),
                    0.6f * Scale,
                    2f,
                    (int)DrawLayer.AbovePlayer,
                    false,
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