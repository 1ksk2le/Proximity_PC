using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;

namespace Proximity
{
    public static class TouchInputExtensions
    {
        private const float SWIPE_THRESHOLD = 50f;
        private const float DOUBLE_TAP_THRESHOLD = 0.3f;
        private const float LONG_PRESS_THRESHOLD = 0.5f;

        public static bool IsTouching(this TouchCollection touches, Rectangle area)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    if (area.Contains(touch.Position))
                        return true;
                }
            }
            return false;
        }

        public static bool IsTouching(this TouchCollection touches, Vector2 position, float radius)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    if (Vector2.Distance(touch.Position, position) <= radius)
                        return true;
                }
            }
            return false;
        }

        public static bool IsTouching(this TouchCollection touches, Vector2[] polygon)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    if (touch.Position.IsInPolygon(polygon))
                        return true;
                }
            }
            return false;
        }

        public static Vector2 GetFirstTouchPosition(this TouchCollection touches)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    return touch.Position;
                }
            }
            return Vector2.Zero;
        }

        public static List<Vector2> GetAllTouchPositions(this TouchCollection touches)
        {
            List<Vector2> positions = new List<Vector2>();
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    positions.Add(touch.Position);
                }
            }
            return positions;
        }

        public static Vector2 GetAverageTouchPosition(this TouchCollection touches)
        {
            Vector2 sum = Vector2.Zero;
            int count = 0;
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    sum += touch.Position;
                    count++;
                }
            }
            return count > 0 ? sum / count : Vector2.Zero;
        }

        public static bool IsPinching(this TouchCollection touches, out float scale)
        {
            scale = 1f;
            if (touches.Count != 2) return false;

            TouchLocation touch1, touch2;
            if (!touches[0].TryGetPreviousLocation(out touch1) ||
                !touches[1].TryGetPreviousLocation(out touch2))
                return false;

            float previousDistance = Vector2.Distance(touch1.Position, touch2.Position);
            float currentDistance = Vector2.Distance(touches[0].Position, touches[1].Position);

            scale = currentDistance / previousDistance;
            return true;
        }

        public static bool IsRotating(this TouchCollection touches, out float angle)
        {
            angle = 0f;
            if (touches.Count != 2) return false;

            TouchLocation touch1, touch2;
            if (!touches[0].TryGetPreviousLocation(out touch1) ||
                !touches[1].TryGetPreviousLocation(out touch2))
                return false;

            Vector2 previousVector = touch2.Position - touch1.Position;
            Vector2 currentVector = touches[1].Position - touches[0].Position;

            angle = (float)Math.Atan2(currentVector.Y, currentVector.X) -
                   (float)Math.Atan2(previousVector.Y, previousVector.X);
            return true;
        }

        public static bool IsDragging(this TouchCollection touches, out Vector2 delta)
        {
            delta = Vector2.Zero;
            if (touches.Count != 1) return false;

            TouchLocation previous;
            if (!touches[0].TryGetPreviousLocation(out previous))
                return false;

            delta = touches[0].Position - previous.Position;
            return true;
        }

        public static bool IsSwipe(this TouchCollection touches, out Vector2 direction, out float speed)
        {
            direction = Vector2.Zero;
            speed = 0f;
            if (touches.Count != 1) return false;

            TouchLocation previous;
            if (!touches[0].TryGetPreviousLocation(out previous))
                return false;

            Vector2 delta = touches[0].Position - previous.Position;
            float distance = delta.Length();

            if (distance >= SWIPE_THRESHOLD)
            {
                direction = Vector2.Normalize(delta);
                speed = distance;
                return true;
            }

            return false;
        }

        public static bool IsDoubleTap(this TouchCollection touches, out Vector2 position)
        {
            position = Vector2.Zero;
            if (touches.Count != 1) return false;

            TouchLocation previous;
            if (!touches[0].TryGetPreviousLocation(out previous))
                return false;

            if (touches[0].State == TouchLocationState.Pressed &&
                previous.State == TouchLocationState.Released)
            {
                position = touches[0].Position;
                return true;
            }

            return false;
        }

        public static bool IsLongPress(this TouchCollection touches, out Vector2 position)
        {
            position = Vector2.Zero;
            if (touches.Count != 1) return false;

            TouchLocation previous;
            if (!touches[0].TryGetPreviousLocation(out previous))
                return false;

            if (touches[0].State == TouchLocationState.Moved)
            {
                position = touches[0].Position;
                return true;
            }

            return false;
        }

        public static bool IsFlick(this TouchCollection touches, out Vector2 direction, out float speed)
        {
            direction = Vector2.Zero;
            speed = 0f;
            if (touches.Count != 1) return false;

            TouchLocation previous;
            if (!touches[0].TryGetPreviousLocation(out previous))
                return false;

            if (touches[0].State == TouchLocationState.Released)
            {
                Vector2 delta = touches[0].Position - previous.Position;
                float distance = delta.Length();

                if (distance >= SWIPE_THRESHOLD)
                {
                    direction = Vector2.Normalize(delta);
                    speed = distance;
                    return true;
                }
            }

            return false;
        }

        public static bool IsMultiTouchDrag(this TouchCollection touches, out Vector2 delta)
        {
            delta = Vector2.Zero;
            if (touches.Count < 2) return false;

            Vector2 currentCenter = GetAverageTouchPosition(touches);
            TouchCollection previousTouches = TouchPanel.GetState();
            Vector2 previousCenter = GetAverageTouchPosition(previousTouches);

            delta = currentCenter - previousCenter;
            return true;
        }

        public static bool IsNewTouch(this TouchCollection touches)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed)
                    return true;
            }
            return false;
        }

        public static bool IsTouchReleased(this TouchCollection touches)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Released)
                    return true;
            }
            return false;
        }

        public static int GetActiveTouchCount(this TouchCollection touches)
        {
            int count = 0;
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                    count++;
            }
            return count;
        }

        public static Rectangle GetTouchBounds(this TouchCollection touches)
        {
            if (touches.Count == 0) return Rectangle.Empty;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
                {
                    minX = Math.Min(minX, touch.Position.X);
                    minY = Math.Min(minY, touch.Position.Y);
                    maxX = Math.Max(maxX, touch.Position.X);
                    maxY = Math.Max(maxY, touch.Position.Y);
                }
            }

            return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }
    }
}