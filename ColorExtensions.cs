using Microsoft.Xna.Framework;
using System;

namespace Proximity
{
    public static class ColorExtensions
    {
        public static Color Lerp(this Color color1, Color color2, float amount)
        {
            return new Color(
                (byte)MathHelper.Lerp(color1.R, color2.R, amount),
                (byte)MathHelper.Lerp(color1.G, color2.G, amount),
                (byte)MathHelper.Lerp(color1.B, color2.B, amount),
                (byte)MathHelper.Lerp(color1.A, color2.A, amount)
            );
        }

        public static Color Multiply(this Color color, float factor)
        {
            return new Color(
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor),
                color.A
            );
        }

        public static Color Add(this Color color1, Color color2)
        {
            return new Color(
                (byte)Math.Min(color1.R + color2.R, 255),
                (byte)Math.Min(color1.G + color2.G, 255),
                (byte)Math.Min(color1.B + color2.B, 255),
                (byte)Math.Min(color1.A + color2.A, 255)
            );
        }

        public static Color Subtract(this Color color1, Color color2)
        {
            return new Color(
                (byte)Math.Max(color1.R - color2.R, 0),
                (byte)Math.Max(color1.G - color2.G, 0),
                (byte)Math.Max(color1.B - color2.B, 0),
                (byte)Math.Max(color1.A - color2.A, 0)
            );
        }

        public static Color FromHSV(float hue, float saturation, float value)
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

        public static void ToHSV(this Color color, out float hue, out float saturation, out float value)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            // Calculate hue
            if (delta == 0)
                hue = 0;
            else if (max == r)
                hue = 60 * ((g - b) / delta % 6);
            else if (max == g)
                hue = 60 * ((b - r) / delta + 2);
            else
                hue = 60 * ((r - g) / delta + 4);

            if (hue < 0)
                hue += 360;

            // Calculate saturation
            saturation = max == 0 ? 0 : delta / max;

            // Calculate value
            value = max;
        }

        public static Color WithAlpha(this Color color, byte alpha)
        {
            return new Color(color.R, color.G, color.B, alpha);
        }

        public static Color WithRed(this Color color, byte red)
        {
            return new Color(red, color.G, color.B, color.A);
        }

        public static Color WithGreen(this Color color, byte green)
        {
            return new Color(color.R, green, color.B, color.A);
        }

        public static Color WithBlue(this Color color, byte blue)
        {
            return new Color(color.R, color.G, blue, color.A);
        }

        public static Color Invert(this Color color)
        {
            return new Color(
                (byte)(255 - color.R),
                (byte)(255 - color.G),
                (byte)(255 - color.B),
                color.A
            );
        }

        public static Color Darken(this Color color, float amount)
        {
            return color.Multiply(1 - amount);
        }

        public static Color Lighten(this Color color, float amount)
        {
            return color.Multiply(1 + amount);
        }

        public static Color Grayscale(this Color color)
        {
            byte gray = (byte)((color.R * 0.299f + color.G * 0.587f + color.B * 0.114f));
            return new Color(gray, gray, gray, color.A);
        }

        public static float GetBrightness(this Color color)
        {
            return (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255f;
        }
    }
}