using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Proximity.Content
{
    public class BitmapFont
    {
        private readonly Texture2D fontTexture;
        private readonly Dictionary<char, Rectangle> characterMap;
        private readonly Dictionary<char, int> characterAdvanceMap;
        private readonly int characterWidth;
        private readonly int characterHeight;

        public BitmapFont(Texture2D fontTexture)
        {
            this.fontTexture = fontTexture;
            this.characterWidth = 21;
            this.characterHeight = 30;
            characterMap = new Dictionary<char, Rectangle>();
            characterAdvanceMap = new Dictionary<char, int>();

            InitializeCharacterMap();
        }

        private void InitializeCharacterMap()
        {
            string characters = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890:/-.!'?[]% ";
            for (int i = 0; i < characters.Length; i++)
            {
                int x = (i % 10) * characterWidth;
                int y = (i / 10) * characterHeight;
                char character = characters[i];
                characterMap[character] = new Rectangle(x, y, characterWidth, characterHeight);

                // Assign specific widths for characters
                int advance = character switch
                {
                    'f' => 18,
                    'I' => 15,
                    'i' => 12,
                    'l' => 12,
                    't' => 18,
                    '1' => 18,
                    ':' => 15,
                    '.' => 12,
                    '!' => 12,
                    '\'' => 9,
                    '[' => 15,
                    ']' => 15,
                    _ => 21 // Default width for other characters
                };
                characterAdvanceMap[character] = advance;
            }
        }

        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f, Vector2? origin = null)
        {
            Vector2 currentPosition = position;
            Vector2 effectiveOrigin = origin ?? Vector2.Zero;

            foreach (char c in text)
            {
                if (characterMap.TryGetValue(c, out Rectangle sourceRect))
                {
                    spriteBatch.Draw(fontTexture, currentPosition, sourceRect, color, 0f, effectiveOrigin, scale, SpriteEffects.None, 1f);
                    int advance = characterAdvanceMap.TryGetValue(c, out int adv) ? adv : characterWidth;
                    currentPosition.X += advance * scale;
                }
            }
        }

        public Vector2 MeasureString(string text)
        {
            float width = 0f;
            foreach (char c in text)
            {
                width += characterAdvanceMap.TryGetValue(c, out int adv) ? adv : characterWidth;
            }
            float height = characterHeight;
            return new Vector2(width, height);
        }
    }
}