using Microsoft.Xna.Framework;

namespace Proximity
{
    public static class RectangleExtensions
    {
        public static Microsoft.Xna.Framework.Rectangle Inflate(this Microsoft.Xna.Framework.Rectangle rect, int amount)
        {
            return new Microsoft.Xna.Framework.Rectangle(
                rect.X - amount,
                rect.Y - amount,
                rect.Width + amount * 2,
                rect.Height + amount * 2
            );
        }

        public static Microsoft.Xna.Framework.Rectangle Deflate(this Microsoft.Xna.Framework.Rectangle rect, int amount)
        {
            return new Microsoft.Xna.Framework.Rectangle(
                rect.X + amount,
                rect.Y + amount,
                rect.Width - amount * 2,
                rect.Height - amount * 2
            );
        }

        public static Microsoft.Xna.Framework.Rectangle CenterAt(this Microsoft.Xna.Framework.Rectangle rect, Vector2 position)
        {
            return new Microsoft.Xna.Framework.Rectangle(
                (int)(position.X - rect.Width / 2),
                (int)(position.Y - rect.Height / 2),
                rect.Width,
                rect.Height
            );
        }

        public static Vector2 GetCenter(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Vector2(
                rect.X + rect.Width / 2f,
                rect.Y + rect.Height / 2f
            );
        }

        public static Microsoft.Xna.Framework.Rectangle Scale(this Microsoft.Xna.Framework.Rectangle rect, float scale)
        {
            Vector2 center = rect.GetCenter();
            int newWidth = (int)(rect.Width * scale);
            int newHeight = (int)(rect.Height * scale);
            return new Microsoft.Xna.Framework.Rectangle(
                (int)(center.X - newWidth / 2),
                (int)(center.Y - newHeight / 2),
                newWidth,
                newHeight
            );
        }

        public static bool Contains(this Microsoft.Xna.Framework.Rectangle rect, Vector2 point)
        {
            return rect.Contains(point);
        }

        public static bool Intersects(this Microsoft.Xna.Framework.Rectangle rect1, Microsoft.Xna.Framework.Rectangle rect2)
        {
            return rect1.Intersects(rect2);
        }

        public static Microsoft.Xna.Framework.Rectangle GetIntersection(this Microsoft.Xna.Framework.Rectangle rect1, Microsoft.Xna.Framework.Rectangle rect2)
        {
            Microsoft.Xna.Framework.Rectangle result;
            Microsoft.Xna.Framework.Rectangle.Intersect(ref rect1, ref rect2, out result);
            return result;
        }

        public static bool Touches(this Microsoft.Xna.Framework.Rectangle rect1, Microsoft.Xna.Framework.Rectangle rect2)
        {
            return rect1.Intersects(rect2) ||
                   rect1.X == rect2.X + rect2.Width ||
                   rect1.X + rect1.Width == rect2.X ||
                   rect1.Y == rect2.Y + rect2.Height ||
                   rect1.Y + rect1.Height == rect2.Y;
        }

        public static Vector2 GetTopLeft(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Vector2(rect.X, rect.Y);
        }

        public static Vector2 GetTopRight(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Vector2(rect.X + rect.Width, rect.Y);
        }

        public static Vector2 GetBottomLeft(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Vector2(rect.X, rect.Y + rect.Height);
        }

        public static Vector2 GetBottomRight(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Vector2(rect.X + rect.Width, rect.Y + rect.Height);
        }

        public static Vector2[] GetCorners(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Vector2[]
            {
                rect.GetTopLeft(),
                rect.GetTopRight(),
                rect.GetBottomRight(),
                rect.GetBottomLeft()
            };
        }

        public static Microsoft.Xna.Framework.Rectangle ToRectangle(this System.Drawing.RectangleF rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(
                (int)rect.X,
                (int)rect.Y,
                (int)rect.Width,
                (int)rect.Height
            );
        }

        public static System.Drawing.RectangleF ToRectangleF(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new System.Drawing.RectangleF(
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height
            );
        }

        public static Microsoft.Xna.Framework.Rectangle Offset(this Microsoft.Xna.Framework.Rectangle rect, int x, int y)
        {
            return new Microsoft.Xna.Framework.Rectangle(
                rect.X + x,
                rect.Y + y,
                rect.Width,
                rect.Height
            );
        }

        public static Microsoft.Xna.Framework.Rectangle SetSize(this Microsoft.Xna.Framework.Rectangle rect, int width, int height)
        {
            return new Microsoft.Xna.Framework.Rectangle(
                rect.X,
                rect.Y,
                width,
                height
            );
        }
    }
}