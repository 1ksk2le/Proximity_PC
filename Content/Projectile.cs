using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proximity.Content
{
    public abstract class Projectile
    {
        private const string TEXTURE_PATH_FORMAT = "Textures/Projectiles/t_Projectile_{0}";

        private readonly ContentManager contentManager;
        public readonly ParticleManager particle;
        public readonly Random random = new Random(Environment.TickCount + 4);

        public int ID { get; protected set; }
        public int AI { get; set; }
        public int Damage { get; set; }
        public int Penetrate { get; set; }
        public string Name { get; protected set; }
        public float Rotation { get; protected set; }
        public float TotalLifeTime { get; set; }
        public float Speed { get; set; }
        public float Knockback { get; set; }
        public float CurrentLifeTime { get; set; }
        public float Scale { get; set; } = 1f;
        public Texture2D Texture { get; protected set; }
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 HitboxOffset { get; protected set; }
        public float HitboxRotation => Rotation;
        public bool IsActive { get; set; }

        protected Projectile(ContentManager contentManager, ParticleManager particleManager)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            this.particle = particleManager ?? throw new ArgumentNullException(nameof(particleManager));
            Initialize();
            LoadTexture();
        }

        protected virtual void Initialize()
        {
            IsActive = true;
            Penetrate = 1;
            OnSpawn();
        }

        private void LoadTexture()
        {
            try
            {
                string texturePath = string.Format(TEXTURE_PATH_FORMAT, ID);
                Texture = contentManager.Load<Texture2D>(texturePath);
            }
            catch (Exception)
            {
            }
        }

        public virtual void DrawShadow(SpriteBatch spriteBatch, GameTime gameTime)
        {
            float shadowWidth = Texture.Width * 2;
            float shadowHeight = Texture.Width;

            Rectangle shadowRect = new Rectangle(
                (int)(Position.X - shadowWidth / 2f),
                (int)(Position.Y + Texture.Height),
                (int)shadowWidth,
                (int)shadowHeight
            );

            spriteBatch.Draw(Main.Shadow, shadowRect, Color.White * Player.SHADOW_OPACITY);
        }

        public virtual void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            if (!IsActive || Texture == null) return;
        }

        public virtual void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            if (!IsActive || Texture == null) return;
            if (Main.DebugMode)
            {
                spriteBatch.DrawCircle(Hitbox().Center.ToVector2(), 4f, Color.Black);
                spriteBatch.DrawRectangleBorder(Hitbox(), Color.CadetBlue * 0.2f, Color.CadetBlue * 0.8f);
            }
        }

        public virtual void Update(float deltaTime, Player player)
        {
            if (!IsActive || Penetrate <= 0) return;

            CurrentLifeTime += deltaTime;

            if (CurrentLifeTime >= TotalLifeTime || Penetrate <= 0)
            {
                Kill();
                return;
            }

            if (AI == 0)
            {
                Position += Vector2.Normalize(Direction) * deltaTime * Speed;
                Rotation = (float)Math.Atan2(Direction.Y, Direction.X);
            }
        }

        protected virtual void OnSpawn()
        {
        }

        public virtual void Kill()
        {
            IsActive = false;
        }

        public Rectangle Hitbox()
        {
            Vector2 center = new Vector2(
                Position.X + HitboxOffset.X,
                Position.Y + HitboxOffset.Y
            );

            Vector2[] corners = GetRotatedRectangleCorners(
                center,
                Texture.Width * Scale,
                Texture.Height * Scale,
                HitboxRotation
            );

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (Vector2 corner in corners)
            {
                minX = Math.Min(minX, corner.X);
                minY = Math.Min(minY, corner.Y);
                maxX = Math.Max(maxX, corner.X);
                maxY = Math.Max(maxY, corner.Y);
            }

            return new Rectangle(
                (int)minX,
                (int)minY,
                (int)(maxX - minX),
                (int)(maxY - minY)
            );
        }

        private Vector2[] GetRotatedRectangleCorners(Vector2 center, float width, float height, float rotation)
        {
            Vector2[] corners = new Vector2[4];

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            corners[0] = new Vector2(-halfWidth, -halfHeight);
            corners[1] = new Vector2(halfWidth, -halfHeight);
            corners[2] = new Vector2(halfWidth, halfHeight);
            corners[3] = new Vector2(-halfWidth, halfHeight);

            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);

            for (int i = 0; i < 4; i++)
            {
                Vector2 rotated = new Vector2(
                    corners[i].X * cos - corners[i].Y * sin,
                    corners[i].X * sin + corners[i].Y * cos
                );
                corners[i] = rotated + center;
            }

            return corners;
        }

        public bool Collides(Rectangle otherHitbox)
        {
            Rectangle bounds = Hitbox();

            if (!bounds.Intersects(otherHitbox))
                return false;

            return true;
        }
    }
}