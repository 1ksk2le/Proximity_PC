using Microsoft.Xna.Framework;
using System;

namespace Proximity
{
    public static class RandomExtensions
    {
        public static float NextFloat(this Random random, float minValue, float maxValue)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }

        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        public static float NextFloat(this Random random, float maxValue)
        {
            return (float)(random.NextDouble() * maxValue);
        }

        public static float NextGaussian(this Random random, float mean = 0f, float standardDeviation = 1f)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)(mean + standardDeviation * randStdNormal);
        }

        public static Vector2 NextVector2(this Random random, float minLength, float maxLength)
        {
            float angle = random.NextFloat(0, MathHelper.TwoPi);
            float length = random.NextFloat(minLength, maxLength);
            return new Vector2(
                (float)Math.Cos(angle) * length,
                (float)Math.Sin(angle) * length
            );
        }

        public static Vector2 NextVector2InCircle(this Random random, float radius)
        {
            float angle = random.NextFloat(0, MathHelper.TwoPi);
            float length = random.NextFloat(0, radius);
            return new Vector2(
                (float)Math.Cos(angle) * length,
                (float)Math.Sin(angle) * length
            );
        }

        public static Vector2 NextVector2InRectangle(this Random random, Rectangle rectangle)
        {
            return new Vector2(
                random.NextFloat(rectangle.X, rectangle.X + rectangle.Width),
                random.NextFloat(rectangle.Y, rectangle.Y + rectangle.Height)
            );
        }

        public static Color NextColor(this Random random, bool includeAlpha = false)
        {
            return new Color(
                random.Next(256),
                random.Next(256),
                random.Next(256),
                includeAlpha ? random.Next(256) : 255
            );
        }

        public static Color NextColorInRange(this Random random, Color minColor, Color maxColor)
        {
            return new Color(
                random.Next(minColor.R, maxColor.R + 1),
                random.Next(minColor.G, maxColor.G + 1),
                random.Next(minColor.B, maxColor.B + 1),
                random.Next(minColor.A, maxColor.A + 1)
            );
        }

        public static Color NextColorWithHue(this Random random, float hue, float saturation = 1f, float value = 1f)
        {
            float h = hue % 360f;
            float s = MathHelper.Clamp(saturation, 0f, 1f);
            float v = MathHelper.Clamp(value, 0f, 1f);

            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;

            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return new Color(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)
            );
        }

        public static T NextItem<T>(this Random random, T[] array)
        {
            if (array == null || array.Length == 0)
                throw new ArgumentException("Array cannot be null or empty");

            return array[random.Next(array.Length)];
        }

        public static void Shuffle<T>(this Random random, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
        }

        public static bool NextBool(this Random random, float probability = 0.5f)
        {
            return random.NextDouble() < probability;
        }

        public static int NextWeighted(this Random random, int[] weights)
        {
            if (weights == null || weights.Length == 0)
                throw new ArgumentException("Weights array cannot be null or empty");

            int totalWeight = 0;
            foreach (int weight in weights)
            {
                if (weight < 0)
                    throw new ArgumentException("Weights cannot be negative");
                totalWeight += weight;
            }

            int randomValue = random.Next(totalWeight);
            int currentWeight = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue < currentWeight)
                    return i;
            }

            return weights.Length - 1;
        }
    }
}