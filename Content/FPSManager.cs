using System.Collections.Generic;
using System.Linq;
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

        // Moving average buffer for FPS smoothing
        private readonly Queue<int> fpsBuffer = new Queue<int>();
        private const int BufferSize = 10;

        public FPSManager(BitmapFont font)
        {
            this.font = font;
        }

        public void Update(GameTime gameTime)
        {
            accumulatedTime += gameTime.ElapsedGameTime.TotalSeconds;
            frameCount++;

            if (accumulatedTime >= 0.1) // Update FPS more frequently
            {
                int currentFPS = (int)(frameCount / accumulatedTime);
                fpsBuffer.Enqueue(currentFPS);

                if (fpsBuffer.Count > BufferSize)
                {
                    fpsBuffer.Dequeue();
                }

                fps = (int)(fpsBuffer.Average()); // Calculate moving average

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