using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content
{
    public enum DrawLayer
    {
        BelowPlayer = 0,
        AbovePlayer = 1
    }

    public struct Particle
    {
        public int ID;
        public int DrawLayer;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color StartColor;
        public Color EndColor;
        public float Scale;
        public float Rotation;
        public float ColorFade;
        public bool NoGravity;
        public bool IsActive;
        public float CurrentLifeTime;
        public float TotalLifeTime;
        public bool HasTrail;
        public int AI;
        public int Frame;

        public object Owner;

        private const int MaxTrailLength = 10;
        private const int TrailUpdateInterval = 4;
        private Vector2[] trailPositions;
        private int trailIndex;
        private int trailCount;
        private int frameCount;

        public Particle(int id, Vector2 position, Vector2 velocity, float colorFade, Color startColor, Color endColor, float scale, int type, int drawLayer, float lifeTime, bool hasTrail = false, int ai = 0, float rotation = 0f, int frame = 0)
        {
            ID = id;
            Position = position;
            Velocity = velocity;
            StartColor = startColor;
            EndColor = endColor;
            Scale = scale;
            Rotation = rotation;
            DrawLayer = drawLayer;
            NoGravity = true;
            IsActive = true;
            CurrentLifeTime = 0f;
            ColorFade = colorFade;
            TotalLifeTime = lifeTime;
            HasTrail = hasTrail;
            AI = ai;
            Owner = null;
            Frame = frame;
            trailPositions = new Vector2[MaxTrailLength];
            trailIndex = 0;
            trailCount = 0;
            frameCount = 0;
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            StartColor = Color.White;
            EndColor = Color.White;
            Scale = 1f;
            Rotation = 0f;
            DrawLayer = 0;
            NoGravity = true;
            IsActive = false;
            CurrentLifeTime = 0f;
            ColorFade = 0f;
            TotalLifeTime = 1f;
            HasTrail = false;
            AI = 0;
            Frame = 0;
            trailIndex = 0;
            trailCount = 0;
            frameCount = 0;
            if (trailPositions == null) trailPositions = new Vector2[MaxTrailLength];
            Array.Clear(trailPositions, 0, trailPositions.Length);
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            if (Owner != null)
            {
                if (Owner is Projectile proj && !proj.IsActive)
                {
                    IsActive = false;
                    return;
                }
                if (Owner is NPC npc && !npc.IsActive)
                {
                    IsActive = false;
                    return;
                }
                if (Owner is Player player && player.CurrentHealth <= 0)
                {
                    IsActive = false;
                    return;
                }
            }

            CurrentLifeTime += deltaTime;

            if (CurrentLifeTime >= TotalLifeTime)
            {
                IsActive = false;
                return;
            }

            if (!NoGravity)
                Velocity.Y += 0.5f;

            Position += Velocity * deltaTime;

            if (HasTrail && (++frameCount % TrailUpdateInterval == 0))
            {
                trailPositions[trailIndex] = Position;
                trailIndex = (trailIndex + 1) % MaxTrailLength;
                if (trailCount < MaxTrailLength) trailCount++;
            }

            if (AI == 1)
            {
                float lerpAmount = MathHelper.Clamp(CurrentLifeTime / (TotalLifeTime * 3f), 0f, 1f);
                Scale = MathHelper.Lerp(Scale, 0f, lerpAmount);
            }
            if (AI == 2)
            {
                float ejectPhase = TotalLifeTime * 0.015f;
                float fallPhase = TotalLifeTime * 0.075f;

                if (CurrentLifeTime <= 0.01f)
                {
                }

                if (CurrentLifeTime <= ejectPhase)
                {
                    Velocity.Y -= 3f;
                    if (Owner is Player player && player.CurrentHealth > 0)
                    {
                        Velocity.X -= 2f * (player.IsFacingLeft ? -1f : 1f);
                    }
                }
                else if (CurrentLifeTime <= ejectPhase + fallPhase)
                {
                    Velocity.Y += 1.5f;
                }
                else
                {
                    Velocity = Vector2.Zero;
                }

                if (CurrentLifeTime <= ejectPhase + fallPhase)
                {
                    Random random = new Random();
                    Rotation += (float)random.NextDouble() * 0.5f - 0.05f;
                }
            }
            if (AI == 3)
            {
                float fallPhase = TotalLifeTime * 0.2f;
                if (CurrentLifeTime <= 0.01f)
                {
                }

                if (CurrentLifeTime <= fallPhase)
                {
                    Velocity.Y += 1f;
                }
                else
                {
                    Velocity = Vector2.Zero;
                }
            }
            if (AI == 4)
            {
                float lerpAmount = MathHelper.Clamp(CurrentLifeTime / (TotalLifeTime * 2f), 0f, 1f);
                Velocity = Vector2.Lerp(Velocity, Vector2.Zero, lerpAmount);
            }
        }

        public Color GetCurrentColor()
        {
            if (StartColor == EndColor) return StartColor;
            float lerpAmount = MathHelper.Clamp(CurrentLifeTime / (TotalLifeTime * ColorFade), 0f, 1f);
            return Color.Lerp(StartColor, EndColor, lerpAmount);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (!IsActive || texture == null) return;

            Color drawColor = AI == 2 ? Color.White : GetCurrentColor();
            float invLifeTime = TotalLifeTime > 0f ? 1f / TotalLifeTime : 1f;
            float alpha = 1f - (CurrentLifeTime * invLifeTime);
            alpha = MathHelper.Clamp(alpha, 0f, 1f);
            float layerDepth = DrawLayer == 0 ? 0.6f : 0.4f;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            Rectangle? sourceRect = null;
            if (ID == 7)
            {
                int frameHeight = texture.Height / 4;
                sourceRect = new Rectangle(0, Frame * frameHeight, texture.Width, frameHeight);
                origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            }

            if (HasTrail && trailCount > 0)
            {
                int step = 3;
                for (int i = 0; i < trailCount; i += step)
                {
                    int index = (trailIndex - i - 1 + MaxTrailLength) % MaxTrailLength;
                    float trailAlpha = 1f - (i / (float)MaxTrailLength);
                    float trailScale = Scale * trailAlpha;

                    Color trailColor = new Color(drawColor.R, drawColor.G, drawColor.B, (byte)(drawColor.A * trailAlpha));
                    spriteBatch.Draw(texture, trailPositions[index], sourceRect, trailColor * alpha, Rotation, origin, trailScale, SpriteEffects.None, layerDepth);
                }
            }

            spriteBatch.Draw(texture, Position, sourceRect, drawColor * alpha, Rotation, origin, Scale, SpriteEffects.None, layerDepth);
        }

        public void PreDraw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (DrawLayer == 0) Draw(spriteBatch, texture);
        }

        public void PostDraw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (DrawLayer == 1) Draw(spriteBatch, texture);
        }
    }

    public class ParticleManager
    {
        private const int MaxParticles = 2000;
        private readonly Particle[] activeParticles = new Particle[MaxParticles];
        private int activeCount = 0;
        private readonly Particle[] particlePool = new Particle[MaxParticles];
        private int poolCount = 0;
        private readonly Dictionary<int, Texture2D> particleTextures;
        private readonly ContentManager contentManager;
        private readonly Random random;
        private readonly Dictionary<Texture2D, List<Particle>> preDrawTextureGroups = new();
        private readonly Dictionary<Texture2D, List<Particle>> postDrawTextureGroups = new();

        private readonly Queue<List<Particle>> listPool = new();
        private Rectangle lastVisibleArea;
        private const float ParticlePadding = 200f;

        private bool IsParticleVisible(Vector2 position, Rectangle visibleArea)
        {
            return position.X >= visibleArea.Left - ParticlePadding &&
                   position.X <= visibleArea.Right + ParticlePadding &&
                   position.Y >= visibleArea.Top - ParticlePadding &&
                   position.Y <= visibleArea.Bottom + ParticlePadding;
        }

        public ParticleManager(ContentManager contentManager)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            particleTextures = new Dictionary<int, Texture2D>();
            random = new Random(Environment.TickCount + 1);
            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < MaxParticles; i++)
            {
                particlePool[poolCount++] = new Particle(0, Vector2.Zero, Vector2.Zero, 0f, Color.White, Color.White, 1f, 0, 0, 1f, false);
            }
        }

        private Texture2D GetParticleTexture(int id)
        {
            if (!particleTextures.TryGetValue(id, out Texture2D texture))
            {
                try
                {
                    string texturePath = $"Textures/Particles/t_Particle_{id}";
                    texture = contentManager.Load<Texture2D>(texturePath);
                    particleTextures[id] = texture;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return texture;
        }

        private Particle GetParticleFromPool()
        {
            if (poolCount > 0)
            {
                var particle = particlePool[--poolCount];
                particle.Reset();
                return particle;
            }
            return new Particle(0, Vector2.Zero, Vector2.Zero, 0f, Color.White, Color.White, 1f, 0, 0, 1f);
        }

        private void ReturnParticleToPool(Particle particle)
        {
            particle.IsActive = false;
            if (poolCount < MaxParticles)
                particlePool[poolCount++] = particle;
        }

        private List<Particle> GetListFromPool()
        {
            if (listPool.Count > 0)
                return listPool.Dequeue();
            return new List<Particle>(32);
        }

        private void ReturnListToPool(List<Particle> list)
        {
            list.Clear();
            listPool.Enqueue(list);
        }

        private int newParticleFrameCounter = 0;

        public Particle NewParticle(int id, Rectangle area, Vector2 velocity, float colorFade, Color startColor, Color endColor, float scale, float lifeTime, int drawLayer, bool hasTrail = false, int ai = 0, object owner = null, bool isDirect = true, float rotation = 0f)
        {
            newParticleFrameCounter++;
            if (!isDirect && newParticleFrameCounter % 2 != 0)
                return default;

            if (GetParticleTexture(id) == null) return default;

            Vector2 center = new Vector2(area.Center.X, area.Center.Y);
            Vector2 lengthDir = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
            Vector2 widthDir = new Vector2(-lengthDir.Y, lengthDir.X);

            float randomLength = ((float)random.NextDouble() - 0.5f) * area.Height;
            float randomWidth = ((float)random.NextDouble() - 0.5f) * area.Width;

            Vector2 position = center + (lengthDir * randomLength) + (widthDir * randomWidth);

            if (activeCount >= MaxParticles) return default;
            Particle particle = GetParticleFromPool();
            particle.ID = id;
            particle.Position = position;
            particle.Velocity = velocity;
            particle.StartColor = startColor;
            particle.EndColor = endColor;
            particle.Scale = scale;
            particle.TotalLifeTime = lifeTime;
            particle.DrawLayer = drawLayer;
            particle.CurrentLifeTime = 0f;
            particle.ColorFade = colorFade;
            particle.IsActive = true;
            particle.HasTrail = hasTrail;
            particle.AI = ai;
            particle.Rotation = rotation;
            particle.Owner = owner;
            if (id == 7)
                particle.Frame = random.Next(0, 4);
            else
                particle.Frame = 0;
            activeParticles[activeCount++] = particle;
            return particle;
        }

        public void Update(float deltaTime)
        {
            int i = 0;
            while (i < activeCount)
            {
                ref var particle = ref activeParticles[i];

                bool isOffscreen = lastVisibleArea != Rectangle.Empty && !IsParticleVisible(particle.Position, lastVisibleArea);

                if (isOffscreen)
                {
                    if (!particle.IsActive)
                    {
                        ReturnParticleToPool(particle);
                        activeParticles[i] = activeParticles[--activeCount];
                        continue;
                    }

                    if (particle.Owner != null)
                    {
                        if (particle.Owner is Projectile proj && !proj.IsActive ||
                            particle.Owner is NPC npc && !npc.IsActive ||
                            particle.Owner is Player player && player.CurrentHealth <= 0)
                        {
                            particle.IsActive = false;
                            ReturnParticleToPool(particle);
                            activeParticles[i] = activeParticles[--activeCount];
                            continue;
                        }
                    }

                    particle.CurrentLifeTime += deltaTime;
                    if (particle.CurrentLifeTime >= particle.TotalLifeTime)
                    {
                        particle.IsActive = false;
                        ReturnParticleToPool(particle);
                        activeParticles[i] = activeParticles[--activeCount];
                        continue;
                    }
                    i++;
                }
                else
                {
                    particle.Update(deltaTime);
                    if (!particle.IsActive)
                    {
                        ReturnParticleToPool(particle);
                        activeParticles[i] = activeParticles[--activeCount];
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public void PreDrawParticles(SpriteBatch spriteBatch, Camera camera, Arena world)
        {
            Rectangle visibleArea = camera.GetVisibleArea(Main.Dimensions, world);
            lastVisibleArea = visibleArea;

            foreach (var list in preDrawTextureGroups.Values)
                ReturnListToPool(list);
            preDrawTextureGroups.Clear();

            for (int i = 0; i < activeCount; i++)
            {
                var particle = activeParticles[i];
                if (particle.DrawLayer != 0)
                    continue;

                if (!IsParticleVisible(particle.Position, visibleArea))
                    continue;

                Texture2D texture = GetParticleTexture(particle.ID);
                if (texture == null)
                    continue;

                if (!preDrawTextureGroups.TryGetValue(texture, out var list))
                {
                    list = GetListFromPool();
                    preDrawTextureGroups[texture] = list;
                }
                list.Add(particle);
            }

            foreach (var kvp in preDrawTextureGroups)
            {
                Texture2D texture = kvp.Key;
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].PreDraw(spriteBatch, texture);
                }
            }
        }

        public void PostDrawParticles(SpriteBatch spriteBatch, Camera camera, Arena world)
        {
            Rectangle visibleArea = camera.GetVisibleArea(Main.Dimensions, world);
            foreach (var list in postDrawTextureGroups.Values)
                ReturnListToPool(list);
            postDrawTextureGroups.Clear();
            for (int i = 0; i < activeCount; i++)
            {
                var particle = activeParticles[i];
                if (particle.DrawLayer != 1) continue;
                if (!visibleArea.Contains(particle.Position.ToPoint())) continue;
                Texture2D texture = GetParticleTexture(particle.ID);
                if (texture == null) continue;
                particle.PostDraw(spriteBatch, texture);
            }
        }

        public string ParticleCount()
        {
            return $"Active particles: {activeCount}, Pool size: {poolCount}";
        }
    }
}