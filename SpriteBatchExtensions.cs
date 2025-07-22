using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Proximity.Content;
using System;

namespace Proximity
{
    public static class SpriteBatchExtensions
    {
        private const float DEFAULT_OUTLINE_THICKNESS = 1.5f;
        private const int DEFAULT_CIRCLE_SEGMENTS = 32;
        private const float DEFAULT_LINE_THICKNESS = 4f;

        public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, float layerDepth = 0f)
        {
            spriteBatch.Draw(Main.Pixel, new Vector2(rectangle.X, rectangle.Y), null, color, 0, Vector2.Zero,
                new Vector2(rectangle.Width, rectangle.Height), SpriteEffects.None, layerDepth);
        }

        public static void DrawRectangleBorder(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, Color borderColor,
            float layerDepth = 0f, float borderWidth = DEFAULT_LINE_THICKNESS, float rotation = 0f)
        {
            Vector2 center = rectangle.Center.ToVector2();
            Vector2 size = new Vector2(rectangle.Width, rectangle.Height);

            spriteBatch.Draw(Main.Pixel, center, null, color, rotation,
                new Vector2(0.5f, 0.5f), size, SpriteEffects.None, layerDepth);

            Vector2 topLeft = new Vector2(-size.X / 2, -size.Y / 2);
            Vector2 topRight = new Vector2(size.X / 2, -size.Y / 2);
            Vector2 bottomLeft = new Vector2(-size.X / 2, size.Y / 2);
            Vector2 bottomRight = new Vector2(size.X / 2, size.Y / 2);

            Matrix rotationMatrix = Matrix.CreateRotationZ(rotation);
            topLeft = Vector2.Transform(topLeft, rotationMatrix);
            topRight = Vector2.Transform(topRight, rotationMatrix);
            bottomLeft = Vector2.Transform(bottomLeft, rotationMatrix);
            bottomRight = Vector2.Transform(bottomRight, rotationMatrix);

            spriteBatch.DrawLine(center + topLeft, center + topRight, borderColor, layerDepth, borderWidth);
            spriteBatch.DrawLine(center + topRight, center + bottomRight, borderColor, layerDepth, borderWidth);
            spriteBatch.DrawLine(center + bottomRight, center + bottomLeft, borderColor, layerDepth, borderWidth);
            spriteBatch.DrawLine(center + bottomLeft, center + topLeft, borderColor, layerDepth, borderWidth);
        }

        public static void DrawRoundedRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color,
            float cornerRadius, int cornerSegments = 8, float layerDepth = 0f)
        {
            spriteBatch.DrawRectangle(rectangle, color, layerDepth);

            Vector2[] corners = new Vector2[]
            {
                new Vector2(rectangle.X + cornerRadius, rectangle.Y + cornerRadius),
                new Vector2(rectangle.Right - cornerRadius, rectangle.Y + cornerRadius),
                new Vector2(rectangle.Right - cornerRadius, rectangle.Bottom - cornerRadius),
                new Vector2(rectangle.X + cornerRadius, rectangle.Bottom - cornerRadius)
            };

            for (int i = 0; i < 4; i++)
            {
                float startAngle = i * MathHelper.PiOver2;
                float endAngle = startAngle + MathHelper.PiOver2;
                DrawArc(spriteBatch, corners[i], cornerRadius, startAngle, endAngle, color, cornerSegments, layerDepth);
            }
        }

        public static void DrawCircle(this SpriteBatch spriteBatch, Vector2 center, float radius, Color color,
            int segments = DEFAULT_CIRCLE_SEGMENTS, float layerDepth = 0f, float borderWidth = DEFAULT_LINE_THICKNESS)
        {
            float angleIncrement = MathHelper.TwoPi / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleIncrement;
                float angle2 = (i + 1) * angleIncrement;

                Vector2 point1 = center + new Vector2((float)Math.Cos(angle1) * radius, (float)Math.Sin(angle1) * radius);
                Vector2 point2 = center + new Vector2((float)Math.Cos(angle2) * radius, (float)Math.Sin(angle2) * radius);

                spriteBatch.DrawLine(point1, point2, color, layerDepth, borderWidth);
            }
        }

        public static void DrawFilledCircle(this SpriteBatch spriteBatch, Vector2 center, float radius, Color color,
            int segments = DEFAULT_CIRCLE_SEGMENTS, float layerDepth = 0f)
        {
            float angleIncrement = MathHelper.TwoPi / segments;
            Vector2[] points = new Vector2[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleIncrement;
                points[i] = center + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
            }

            for (int i = 0; i < segments; i++)
            {
                spriteBatch.DrawTriangle(center, points[i], points[i + 1], color, layerDepth);
            }
        }

        private static void DrawArc(this SpriteBatch spriteBatch, Vector2 center, float radius, float startAngle,
            float endAngle, Color color, int segments, float layerDepth)
        {
            float angleIncrement = (endAngle - startAngle) / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = startAngle + i * angleIncrement;
                float angle2 = startAngle + (i + 1) * angleIncrement;

                Vector2 point1 = center + new Vector2((float)Math.Cos(angle1) * radius, (float)Math.Sin(angle1) * radius);
                Vector2 point2 = center + new Vector2((float)Math.Cos(angle2) * radius, (float)Math.Sin(angle2) * radius);

                spriteBatch.DrawLine(point1, point2, color, layerDepth, DEFAULT_LINE_THICKNESS);
            }
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color,
            float layerDepth = 0f, float thickness = DEFAULT_LINE_THICKNESS)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            spriteBatch.Draw(Main.Pixel, start, null, color, angle, Vector2.Zero,
                new Vector2(edge.Length(), thickness), SpriteEffects.None, layerDepth);
        }

        public static void DrawGradientLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end,
            Color startColor, Color endColor, float layerDepth = 0f, float thickness = DEFAULT_LINE_THICKNESS)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();

            for (int i = 0; i <= (int)length; i++)
            {
                float lerpAmount = i / length;
                Vector2 pixelPosition = Vector2.Lerp(start, end, lerpAmount);
                Color pixelColor = Color.Lerp(startColor, endColor, lerpAmount);

                spriteBatch.Draw(Main.Pixel, pixelPosition, null, pixelColor, angle, Vector2.Zero,
                    new Vector2(1, thickness), SpriteEffects.None, layerDepth);
            }
        }

        public static void DrawDashedLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color,
            float dashLength = 10f, float gapLength = 5f, float layerDepth = 0f, float thickness = DEFAULT_LINE_THICKNESS)
        {
            Vector2 direction = Vector2.Normalize(end - start);
            float totalLength = Vector2.Distance(start, end);
            float currentLength = 0f;

            while (currentLength < totalLength)
            {
                float remainingLength = totalLength - currentLength;
                float currentDashLength = Math.Min(dashLength, remainingLength);

                Vector2 dashStart = start + direction * currentLength;
                Vector2 dashEnd = dashStart + direction * currentDashLength;

                spriteBatch.DrawLine(dashStart, dashEnd, color, layerDepth, thickness);

                currentLength += dashLength + gapLength;
            }
        }

        public static void DrawStringWithShadow(this SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, Color textColor, Color shadowColor, Vector2 shadowOffset, float scale = 1f,
            float layerDepth = 0f)
        {
            spriteBatch.DrawString(font, text, position + shadowOffset, shadowColor, 0f, Vector2.Zero,
                scale, SpriteEffects.None, layerDepth - 0.000001f);
            spriteBatch.DrawString(font, text, position, textColor, 0f, Vector2.Zero,
                scale, SpriteEffects.None, layerDepth);
        }

        private static void DrawTriangle(this SpriteBatch spriteBatch, Vector2 point1, Vector2 point2,
            Vector2 point3, Color color, float layerDepth = 0f)
        {
            float minX = Math.Min(Math.Min(point1.X, point2.X), point3.X);
            float minY = Math.Min(Math.Min(point1.Y, point2.Y), point3.Y);
            float maxX = Math.Max(Math.Max(point1.X, point2.X), point3.X);
            float maxY = Math.Max(Math.Max(point1.Y, point2.Y), point3.Y);

            for (float x = minX; x <= maxX; x++)
            {
                for (float y = minY; y <= maxY; y++)
                {
                    Vector2 point = new Vector2(x, y);
                    if (IsPointInTriangle(point, point1, point2, point3))
                    {
                        spriteBatch.Draw(Main.Pixel, point, null, color, 0f, Vector2.Zero,
                            1f, SpriteEffects.None, layerDepth);
                    }
                }
            }
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(point, a, b);
            float d2 = Sign(point, b, c);
            float d3 = Sign(point, c, a);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }
    }
}