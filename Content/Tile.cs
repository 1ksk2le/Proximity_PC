using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity
{
    public class Tile
    {
        public int ID { get; set; }
        public Rectangle Rectangle { get; set; }
        public bool Walkable { get; set; }
        public bool Destroyable { get; set; }
        public string Name { get; set; }

        private static List<Tile> types;

        public static List<Tile> Types
        {
            get
            {
                if (types == null)
                {
                    throw new InvalidOperationException("Tile Types not initialized. Call InitializeTypes first.");
                }
                return types;
            }
            private set => types = value;
        }

        public Tile(int id, Rectangle rectangle, string name, bool walkable, bool destroyable)
        {
            ID = id;
            Rectangle = rectangle;
            Name = name;
            Walkable = walkable;
            Destroyable = destroyable;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (spriteBatch == null || texture == null || ID == -1)
                return;

            //spriteBatch.Draw(texture, Rectangle, Color.White);
            spriteBatch.Draw(texture, new Vector2(Rectangle.X, Rectangle.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        public Tile Clone(Rectangle rectangle)
        {
            return new Tile(ID, rectangle, Name, Walkable, Destroyable);
        }

        public static void InitializeTypes(ContentManager content)
        {
            Types = new List<Tile>();
            try
            {
                for (int i = 0; ; i++)
                {
                    string texturePath = $"Textures/Tiles/t_Tile_{i}";
                    try
                    {
                        content.Load<Texture2D>(texturePath);

                        bool isWalkable = i != 3;
                        string name = GetDefaultTileName(i);
                        Types.Add(new Tile(i, Rectangle.Empty, name, isWalkable, false));
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }

                if (Types.Count == 0)
                {
                    Types.Add(new Tile(0, Rectangle.Empty, "Unknown", false, false));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string GetDefaultTileName(int id)
        {
            return id switch
            {
                0 => "Void",
                1 => "Grass",
                2 => "Stone",
                3 => "Water",
                _ => $"Tile {id}"
            };
        }

        public static string GetTileName(int id)
        {
            var tile = Types.Find(t => t.ID == id);
            return tile?.Name ?? "Unknown";
        }

        public static bool CanWalk(int id)
        {
            var tile = Types.Find(t => t.ID == id);
            return tile?.Walkable ?? false;
        }
    }
}