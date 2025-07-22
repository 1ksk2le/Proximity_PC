using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content
{
    public class FloatingText
    {
        public string Text;
        public Vector2 Position;

        public Color StartColor;
        public Color EndColor;

        public float Lifetime;
        public float Alpha;
        public Vector2 Velocity;
        public float Scale;
        public bool Direct;

        private float totalLifetime;
        private Vector2 initialPosition;
        private Vector2 offset;

        public object Owner { get; set; }

        public FloatingText(string text, Vector2 position, Color startColor, Color endColor, float lifetime = 1.0f, object owner = null, Vector2? offset = null, bool direct = false)
        {
            this.offset = offset ?? Vector2.Zero;
            Text = text;
            Position = position + this.offset;
            initialPosition = position + this.offset;
            StartColor = startColor;
            EndColor = endColor;
            Lifetime = lifetime;
            Alpha = 1f;
            Velocity = new Vector2(0, -75f);
            totalLifetime = lifetime;
            Owner = owner;
            Direct = direct;
        }

        public void Update(float deltaTime)
        {
            Lifetime -= deltaTime;
            float t = 1f - MathHelper.Clamp(Lifetime / totalLifetime, 0f, 1f);

            Alpha = MathHelper.Clamp(1f - t * t, 0f, 1f);

            float popScale = MathHelper.Lerp(0.5f, 1.25f, (float)Math.Min(t, 1f));
            Scale = popScale;

            Color currentColor = Color.Lerp(StartColor, EndColor, t * 2);

            if (Owner != null)
            {
                var ownerType = Owner.GetType();
                var posProp = ownerType.GetProperty("Position");
                if (posProp != null)
                {
                    var ownerPos = (Vector2)posProp.GetValue(Owner);
                    initialPosition = ownerPos + (Direct ? Vector2.Zero : offset);
                }
            }
            Position = initialPosition + Velocity * (totalLifetime - Lifetime) * (1f - t * 0.2f);
            this.currentDrawColor = currentColor;
        }

        private Color currentDrawColor;

        public void Draw(SpriteBatch spriteBatch, BitmapFont font)
        {
            var drawColor = currentDrawColor * Alpha;

            Vector2 textSize = font.MeasureString(Text);
            Vector2 origin = textSize / 2;

            font.DrawString(spriteBatch, Text, Position, drawColor, Scale, origin);
        }

        public bool IsAlive => Lifetime > 0f;
    }

    public class FloatingTextManager
    {
        private readonly List<FloatingText> texts = new List<FloatingText>();
        private readonly Random random = new Random();
        private readonly BitmapFont font;

        public FloatingTextManager(BitmapFont font)
        {
            this.font = font;
        }

        public void Add(string text, Vector2 position, Color startColor, Color endColor, float lifetime, object owner, bool direct)
        {
            float maxRadius = 70f;
            int maxTries = 20;
            Vector2 offset = Vector2.Zero;
            Vector2 textSize = font.MeasureString(text);
            RectangleF newRect = default;

            for (int attempt = 0; attempt < maxTries; attempt++)
            {
                float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                float radius = maxRadius * (float)random.NextDouble();
                offset = new Vector2((float)System.Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                Vector2 candidatePos = position + offset;
                newRect = new RectangleF(candidatePos.X - textSize.X / 2, candidatePos.Y - textSize.Y / 2, textSize.X, textSize.Y);

                bool overlaps = false;
                foreach (var t in texts)
                {
                    Vector2 otherSize = font.MeasureString(t.Text);
                    RectangleF otherRect = new RectangleF(t.Position.X - otherSize.X / 2, t.Position.Y - otherSize.Y / 2, otherSize.X, otherSize.Y);
                    if (newRect.Intersects(otherRect))
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (!overlaps)
                    break;
            }
            texts.Add(new FloatingText(text, position, startColor, endColor, lifetime, owner, offset, direct));
        }

        public void Update(float deltaTime)
        {
            for (int i = texts.Count - 1; i >= 0; i--)
            {
                texts[i].Update(deltaTime);
                if (!texts[i].IsAlive)
                    texts.RemoveAt(i);
            }
        }

        public void Draw(SpriteBatch spriteBatch, BitmapFont font)
        {
            foreach (var text in texts)
                text.Draw(spriteBatch, font);
        }
    }

    public struct RectangleF
    {
        public float X, Y, Width, Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public bool Intersects(RectangleF other)
        {
            return !(other.X > X + Width ||
                     other.X + other.Width < X ||
                     other.Y > Y + Height ||
                     other.Y + other.Height < Y);
        }
    }
}