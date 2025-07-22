using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content
{
    public class Arena
    {
        private const int DEFAULT_TILE_SIZE = 80;
        private const int ARENA_SIZE_X = 30;
        private const int ARENA_SIZE_Y = 30;

        private readonly Tile[,] tiles;

        public int TileSize { get; } = DEFAULT_TILE_SIZE;
        public int SizeX { get; } = ARENA_SIZE_X;
        public int SizeY { get; } = ARENA_SIZE_Y;
        public int CenterX { get; private set; }
        public int CenterY { get; private set; }
        public int Width => SizeX * TileSize;
        public int Height => SizeY * TileSize;

        public Arena()
        {
            tiles = new Tile[SizeX, SizeY];
            CalculateCenter();
            GenerateTerrain();
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, Dictionary<int, Texture2D> tileTextures)
        {
            if (spriteBatch == null || camera == null || tileTextures == null)
                throw new ArgumentNullException("SpriteBatch, Camera, and tileTextures cannot be null");

            Rectangle visibleArea = camera.GetVisibleArea(Main.Dimensions, this);
            int startX = Math.Max(0, visibleArea.X / TileSize);
            int startY = Math.Max(0, visibleArea.Y / TileSize);
            int endX = Math.Min(SizeX, (visibleArea.X + visibleArea.Width) / TileSize + 1);
            int endY = Math.Min(SizeY, (visibleArea.Y + visibleArea.Height) / TileSize + 1);

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    Tile tile = GetTile(x, y);
                    if (tile != null && tileTextures.TryGetValue(tile.ID, out Texture2D texture))
                    {
                        tile.Draw(spriteBatch, texture);
                    }
                }
            }
        }

        public int GetTileID(int x, int y)
        {
            return IsValidTile(x, y) ? tiles[x, y].ID : 0;
        }

        public void SetTileID(int x, int y, int tileID)
        {
            if (IsValidTile(x, y))
            {
                if (tiles[x, y] == null)
                {
                    tiles[x, y] = Tile.Types[0].Clone(new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
                tiles[x, y].ID = tileID;
            }
        }

        public Tile GetTile(int x, int y)
        {
            return IsValidTile(x, y) ? tiles[x, y] : null;
        }

        public void SetTile(int x, int y, Tile tile)
        {
            if (IsValidTile(x, y))
            {
                tiles[x, y] = tile.Clone(new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
            }
        }

        public bool IsColliding(Rectangle bounds)
        {
            int leftTile = bounds.Left / TileSize;
            int rightTile = bounds.Right / TileSize;
            int topTile = bounds.Top / TileSize;
            int bottomTile = bounds.Bottom / TileSize;

            for (int x = leftTile; x <= rightTile; x++)
            {
                for (int y = topTile; y <= bottomTile; y++)
                {
                    Tile tile = GetTile(x, y);
                    if (tile != null && !tile.Walkable)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CalculateCenter()
        {
            CenterX = Width / 2;
            CenterY = Height / 2;
        }

        private void GenerateTerrain()
        {
            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < SizeY; y++)
                {
                    bool isWater = x == 0 || x == SizeX - 1 || y == 0 || y == SizeY - 1;
                    int tileID = isWater ? 3 : 1;
                    tiles[x, y] = Tile.Types[tileID].Clone(new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
            }
        }

        private bool IsValidTile(int x, int y)
        {
            return x >= 0 && x < SizeX && y >= 0 && y < SizeY;
        }
    }
}