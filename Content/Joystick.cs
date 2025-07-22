namespace Proximity
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input.Touch;
    using System;
    using System.Linq;

    public class Joystick
    {
        private const float DRAG_MULTIPLIER = 2f;
        private const float OPACITY = 0.6f;
        private const float SHADOW_OFFSET = 10f;

        private readonly Texture2D baseTexture;
        private readonly Texture2D knobTexture;
        private readonly Texture2D knobShadowTexture;
        private Vector2 basePosition;
        private Vector2 knobPosition;
        private readonly float radius;

        public Vector2 Direction { get; private set; }
        public Vector2 BasePosition => basePosition;
        public Vector2 KnobPosition => knobPosition;
        public float Radius => radius;

        public Joystick(Texture2D baseTexture, Texture2D knobTexture, Texture2D knobShadowTexture, Vector2 position, float size)
        {
            if (baseTexture == null) throw new ArgumentNullException(nameof(baseTexture));
            if (knobTexture == null) throw new ArgumentNullException(nameof(knobTexture));
            if (knobShadowTexture == null) throw new ArgumentNullException(nameof(knobShadowTexture));

            this.baseTexture = baseTexture;
            this.knobTexture = knobTexture;
            this.knobShadowTexture = knobShadowTexture;
            this.basePosition = position;
            this.knobPosition = position;
            this.radius = size / 2;
            this.Direction = Vector2.Zero;
        }

        public void Update(TouchCollection touches, Rectangle jumpButton, bool isAttackJoystick)
        {
            var activeTouches = touches.GetAllTouchPositions();
            if (activeTouches.Count == 0)
            {
                ResetJoystick();
                return;
            }

            var closestTouch = activeTouches
                .Where(t => !jumpButton.Contains(t))
                .OrderBy(t => Vector2.Distance(t, basePosition))
                .FirstOrDefault();

            if (closestTouch != Vector2.Zero && Vector2.Distance(closestTouch, basePosition) < radius * DRAG_MULTIPLIER)
            {
                UpdateKnobPosition(closestTouch);
            }
            else
            {
                ResetJoystick();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            /*if (spriteBatch == null) throw new ArgumentNullException(nameof(spriteBatch));

            Vector2 baseOrigin = new Vector2(baseTexture.Width / 2, baseTexture.Height / 2);
            Vector2 knobOrigin = new Vector2(knobTexture.Width / 2, knobTexture.Height / 2);
            Vector2 shadowOffset = new Vector2(0, SHADOW_OFFSET);

            Color baseColor = new Color(150, 150, 150, (int)(255 * OPACITY));
            Color knobColor = new Color(150, 150, 150, (int)(255 * OPACITY));

            spriteBatch.Draw(baseTexture, basePosition, null, baseColor, 0f, baseOrigin, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(knobShadowTexture, knobPosition + shadowOffset, null, baseColor, 0f, knobOrigin, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(knobTexture, knobPosition, null, knobColor, 0f, knobOrigin, 1f, SpriteEffects.None, 0f);*/
        }

        private void UpdateKnobPosition(Vector2 touchPosition)
        {
            Vector2 direction = touchPosition - basePosition;
            if (direction.Length() > radius)
            {
                direction = Vector2.Normalize(direction) * radius;
            }

            knobPosition = basePosition + direction;
            Direction = direction / radius;
        }

        private void ResetJoystick()
        {
            knobPosition = basePosition;
            Direction = Vector2.Zero;
        }
    }
}