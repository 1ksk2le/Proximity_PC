using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Proximity.Content;

namespace Proximity
{
    public class FPSManager
    {
        private int fps;
        private double accumulatedTime;
        private int frameCount;
        private readonly BitmapFont font;

        public FPSManager(BitmapFont font)
        {
            this.font = font;
        }

        public void Update(GameTime gameTime)
        {
            accumulatedTime += gameTime.ElapsedGameTime.TotalSeconds;
            frameCount++;

            if (accumulatedTime >= 0.3)
            {
                fps = (int)(frameCount / accumulatedTime);
                accumulatedTime = 0;
                frameCount = 0;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            font.DrawString(
                spriteBatch,
                $"FPS: {fps}",
                new Vector2(10, 10),
                Color.White
            );
        }
    }
}