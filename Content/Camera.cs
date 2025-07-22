using Microsoft.Xna.Framework;
using System;

namespace Proximity.Content
{
    public class Camera
    {
        private const float MIN_ZOOM = 1f;
        private const float MAX_ZOOM = 3f;
        private const float DEFAULT_ZOOM = 1.0f;
        private const float ZOOM_SCALE = 1.0f;
        private const float PAUSED_ZOOM = 2.5f;
        private const float ZOOM_TRANSITION_SPEED = 3.0f;

        private Vector2 position;
        private readonly int worldWidth;
        private readonly int worldHeight;
        private float zoom = DEFAULT_ZOOM;
        private float targetZoom = DEFAULT_ZOOM;

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public Vector2 Center => Position + new Vector2(Main.Dimensions.X / (2 * Zoom), Main.Dimensions.Y / (2 * Zoom));

        public float Zoom
        {
            get => zoom;
            set => zoom = MathHelper.Clamp(value, MIN_ZOOM, MAX_ZOOM);
        }

        public Matrix TransformMatrix => Matrix.CreateTranslation(new Vector3(-Position, 0)) * Matrix.CreateScale(Zoom, Zoom, ZOOM_SCALE);

        public Camera(Vector2 initialPosition, int worldWidth, int worldHeight)
        {
            Position = initialPosition;
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;
        }

        public void SetInventoryMode(bool enabled, Vector2? focusPosition = null, float? zoom = null) { }

        public void Update(Player player, float deltaTime = 1 / 60f, bool isPaused = false)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            targetZoom = isPaused ? PAUSED_ZOOM : DEFAULT_ZOOM;
            if (Math.Abs(zoom - targetZoom) > 0.001f)
            {
                zoom = MathHelper.Lerp(zoom, targetZoom, ZOOM_TRANSITION_SPEED * deltaTime);
                if (Math.Abs(zoom - targetZoom) < 0.01f)
                {
                    zoom = targetZoom;
                }
            }

            Vector2 screenSize = Main.Dimensions;
            Vector2 targetPosition = player.Position - (screenSize / (2f * zoom));
            float maxX = worldWidth - (screenSize.X / zoom);
            float maxY = worldHeight - (screenSize.Y / zoom);

            Position = new Vector2(
                MathHelper.Clamp(targetPosition.X, 0, Math.Max(0, maxX)),
                MathHelper.Clamp(targetPosition.Y, 0, Math.Max(0, maxY))
            );
        }

        public Rectangle GetVisibleArea(Vector2 screenSize, Arena world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            float visibleWidth = screenSize.X / Zoom;
            float visibleHeight = screenSize.Y / Zoom;

            float visibleX = MathHelper.Clamp(Position.X, 0, world.Width - visibleWidth);
            float visibleY = MathHelper.Clamp(Position.Y, 0, world.Height - visibleHeight);

            return new Rectangle((int)visibleX, (int)visibleY, (int)visibleWidth, (int)visibleHeight);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(TransformMatrix));
        }
    }
}