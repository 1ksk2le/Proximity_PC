using Microsoft.Xna.Framework;
using System;

namespace Proximity
{
    public static class Vector2Extensions
    {
        public static float DistanceTo(this Vector2 point1, Vector2 point2)
        {
            return Vector2.Distance(point1, point2);
        }

        public static float DistanceSquaredTo(this Vector2 point1, Vector2 point2)
        {
            return Vector2.DistanceSquared(point1, point2);
        }

        public static float Length(this Vector2 vector)
        {
            return vector.Length();
        }

        public static float LengthSquared(this Vector2 vector)
        {
            return vector.LengthSquared();
        }

        public static Vector2 Rotate(this Vector2 vector, float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            return new Vector2(
                vector.X * cos - vector.Y * sin,
                vector.X * sin + vector.Y * cos);
        }

        public static float AngleTo(this Vector2 vector1, Vector2 vector2)
        {
            return (float)Math.Atan2(vector2.Y - vector1.Y, vector2.X - vector1.X);
        }

        public static float Angle(this Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }

        public static Vector2 Normalize(this Vector2 vector)
        {
            vector.Normalize();
            return vector;
        }

        public static Vector2 DirectionTo(this Vector2 from, Vector2 to)
        {
            return Vector2.Normalize(to - from);
        }

        public static Vector2 Perpendicular(this Vector2 vector)
        {
            return new Vector2(-vector.Y, vector.X);
        }

        public static Vector2 Clamp(this Vector2 vector, Rectangle bounds)
        {
            return new Vector2(
                MathHelper.Clamp(vector.X, bounds.X, bounds.X + bounds.Width),
                MathHelper.Clamp(vector.Y, bounds.Y, bounds.Y + bounds.Height)
            );
        }

        public static Vector2 Clamp(this Vector2 vector, float minX, float maxX, float minY, float maxY)
        {
            return new Vector2(
                MathHelper.Clamp(vector.X, minX, maxX),
                MathHelper.Clamp(vector.Y, minY, maxY)
            );
        }

        public static Vector2 Lerp(this Vector2 start, Vector2 end, float amount)
        {
            return Vector2.Lerp(start, end, amount);
        }

        public static Vector2 SmoothStep(this Vector2 start, Vector2 end, float amount)
        {
            amount = MathHelper.Clamp(amount, 0f, 1f);
            amount = amount * amount * (3f - 2f * amount);
            return Vector2.Lerp(start, end, amount);
        }

        public static bool IsInRectangle(this Vector2 point, Rectangle rectangle)
        {
            return rectangle.Contains(point);
        }

        public static bool IsInCircle(this Vector2 point, Vector2 center, float radius)
        {
            return Vector2.DistanceSquared(point, center) <= radius * radius;
        }

        public static bool IsInPolygon(this Vector2 point, Vector2[] polygon)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < point.Y && polygon[j].Y >= point.Y ||
                    polygon[j].Y < point.Y && polygon[i].Y >= point.Y)
                {
                    if (polygon[i].X + (point.Y - polygon[i].Y) /
                        (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < point.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static Vector2 Round(this Vector2 vector)
        {
            return new Vector2(
                (float)Math.Round(vector.X),
                (float)Math.Round(vector.Y)
            );
        }

        public static Vector2 Floor(this Vector2 vector)
        {
            return new Vector2(
                (float)Math.Floor(vector.X),
                (float)Math.Floor(vector.Y)
            );
        }

        public static Vector2 Ceiling(this Vector2 vector)
        {
            return new Vector2(
                (float)Math.Ceiling(vector.X),
                (float)Math.Ceiling(vector.Y)
            );
        }

        public static Point ToPoint(this Vector2 vector)
        {
            return new Point((int)vector.X, (int)vector.Y);
        }

        public static float ToAngle(this Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }

        public static Vector2 FromAngle(float angle)
        {
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        public static Vector2 Normalized(this Vector2 vector)
        {
            if (vector == Vector2.Zero)
                return Vector2.Zero;

            return Vector2.Normalize(vector);
        }

        public static float Cross(this Vector2 vector1, Vector2 vector2)
        {
            return vector1.X * vector2.Y - vector1.Y * vector2.X;
        }

        public static float Dot(this Vector2 vector1, Vector2 vector2)
        {
            return Vector2.Dot(vector1, vector2);
        }

        public static Vector2 Reflect(this Vector2 vector, Vector2 normal)
        {
            normal = normal.Normalized();
            return vector - 2 * Vector2.Dot(vector, normal) * normal;
        }

        public static bool IsNaN(this Vector2 vector)
        {
            return float.IsNaN(vector.X) || float.IsNaN(vector.Y);
        }

        public static bool IsInfinity(this Vector2 vector)
        {
            return float.IsInfinity(vector.X) || float.IsInfinity(vector.Y);
        }

        public static bool IsValid(this Vector2 vector)
        {
            return !vector.IsNaN() && !vector.IsInfinity();
        }

        public static bool IsZero(this Vector2 vector)
        {
            return vector.X == 0 && vector.Y == 0;
        }

        public static bool IsNearZero(this Vector2 vector, float epsilon = 0.00001f)
        {
            return Math.Abs(vector.X) < epsilon && Math.Abs(vector.Y) < epsilon;
        }

        public static bool IsNearlyEqual(this Vector2 vector1, Vector2 vector2, float epsilon = 0.00001f)
        {
            return Math.Abs(vector1.X - vector2.X) < epsilon && Math.Abs(vector1.Y - vector2.Y) < epsilon;
        }

        public static Vector2 WithX(this Vector2 vector, float x)
        {
            return new Vector2(x, vector.Y);
        }

        public static Vector2 WithY(this Vector2 vector, float y)
        {
            return new Vector2(vector.X, y);
        }

        public static bool IsInTriangle(this Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = point - a;

            float dot00 = v0.Dot(v0);
            float dot01 = v0.Dot(v1);
            float dot02 = v0.Dot(v2);
            float dot11 = v1.Dot(v1);
            float dot12 = v1.Dot(v2);

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }

        /*public static Vector2 RotateAround(this Vector2 point, Vector2 origin, float angle)
        {
            Vector2 translatedPoint = point - origin;
            Vector2 rotatedPoint = translatedPoint.Rotate(angle);
            return rotatedPoint + origin;
        }*/

        public static Vector2 ScaleAround(this Vector2 point, Vector2 origin, float scale)
        {
            Vector2 translatedPoint = point - origin;
            Vector2 scaledPoint = translatedPoint * scale;
            return scaledPoint + origin;
        }

        public static Vector2 ScaleAround(this Vector2 point, Vector2 origin, Vector2 scale)
        {
            Vector2 translatedPoint = point - origin;
            Vector2 scaledPoint = new Vector2(translatedPoint.X * scale.X, translatedPoint.Y * scale.Y);
            return scaledPoint + origin;
        }

        public static Vector2 Abs(this Vector2 vector)
        {
            return new Vector2(Math.Abs(vector.X), Math.Abs(vector.Y));
        }

        public static Vector2 Sign(this Vector2 vector)
        {
            return new Vector2(Math.Sign(vector.X), Math.Sign(vector.Y));
        }

        public static float AngleBetween(this Vector2 vector1, Vector2 vector2)
        {
            return (float)Math.Acos(MathHelper.Clamp(Vector2.Dot(vector1.Normalized(), vector2.Normalized()), -1f, 1f));
        }

        public static float SignedAngleBetween(this Vector2 vector1, Vector2 vector2)
        {
            return (float)Math.Atan2(vector1.Cross(vector2), vector1.Dot(vector2));
        }

        public static Vector2 MoveTowards(this Vector2 current, Vector2 target, float maxDistanceDelta)
        {
            Vector2 direction = target - current;
            float distance = direction.Length();

            if (distance <= maxDistanceDelta || distance == 0f)
                return target;

            return current + direction / distance * maxDistanceDelta;
        }

        public static Vector2 SnapToGrid(this Vector2 position, float gridSize)
        {
            return new Vector2(
                (float)Math.Round(position.X / gridSize) * gridSize,
                (float)Math.Round(position.Y / gridSize) * gridSize);
        }
    }
}