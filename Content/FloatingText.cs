using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private float totalLifetime;
        private Vector2 initialPosition;

        public object Owner { get; set; }

        public FloatingText(string text, Vector2 position, Color startColor, Color endColor, float lifetime = 1.0f, object owner = null)
        {
            Text = text;
            Position = position;
            initialPosition = position;
            StartColor = startColor;
            EndColor = endColor;
            Lifetime = lifetime;
            Alpha = 1f;
            Velocity = new Vector2(0, -75f);
            totalLifetime = lifetime;
            Owner = owner;
        }

        public void Update(float deltaTime)
        {
            Lifetime -= deltaTime;
            float t = 1f - MathHelper.Clamp(Lifetime / totalLifetime, 0f, 1f);

            Alpha = MathHelper.Clamp(1f - t * t, 0f, 1f);

            float popScale = MathHelper.Lerp(1f, 1.75f, (float)System.Math.Min(t, 1f));
            Scale = popScale;

            Color currentColor = Color.Lerp(StartColor, EndColor, t * 2);

            if (Owner != null)
            {
                var ownerType = Owner.GetType();
                var posProp = ownerType.GetProperty("Position");
                if (posProp != null)
                {
                    var ownerPos = (Vector2)posProp.GetValue(Owner);
                    initialPosition = ownerPos;
                }
            }
            Position = initialPosition + Velocity * (totalLifetime - Lifetime) * (1f - t * 0.2f);
            this.currentDrawColor = currentColor;
        }

        private Color currentDrawColor;

        public void Draw(SpriteBatch spriteBatch, BitmapFont font)
        {
            var drawColor = currentDrawColor * Alpha;

            // Calculate the size of the text to determine the center
            Vector2 textSize = font.MeasureString(Text);
            Vector2 origin = textSize / 2; // Set origin to the center of the text (unscaled)

            font.DrawString(spriteBatch, Text, Position, drawColor, Scale, origin);
        }

        public bool IsAlive => Lifetime > 0f;
    }

    public class FloatingTextManager
    {
        private readonly List<FloatingText> texts = new List<FloatingText>();

        public void Add(string text, Vector2 position, Color startColor, Color endColor, float lifetime = 1.0f, object owner = null)
        {
            texts.Add(new FloatingText(text, position, startColor, endColor, lifetime, owner));
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
}