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
        AbovePlayer = 1,
        OnArena = 2
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
        public int AI;
        public int Frame;

        public object Owner;

        public Particle(int id, Vector2 position, Vector2 velocity, float colorFade, Color startColor, Color endColor, float scale, int type, int drawLayer, float lifeTime, int ai = 0, float rotation = 0f, int frame = 0)
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
            AI = ai;
            Owner = null;
            Frame = frame;
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
            AI = 0;
            Frame = 0;
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

            if (AI == 1)
            {
                float lerpAmount = MathHelper.Clamp(CurrentLifeTime / (TotalLifeTime * 3f), 0f, 1f);
                Scale = MathHelper.Lerp(Scale, 0f, lerpAmount);
            }
            if (AI == 2)
            {
                float ejectPhase = TotalLifeTime * 0.015f;
                float fallPhase = TotalLifeTime * 0.1f;

                if (CurrentLifeTime <= 0.01f)
                {
                }

                if (CurrentLifeTime <= ejectPhase)
                {
                    Velocity.Y -= 5f;
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
    }

    public class ParticleManager
    {
        private List<Texture2D> _particleTextures;
        private Dictionary<int, Texture2D> _idToTexture;
        private Effect _particleEffect;
        private DynamicVertexBuffer _vertexBuffer;

        private struct ParticleVertex : IVertexType
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public Color Color;
            public float Scale;
            public float Rotation;
            public Vector2 Corner;

            public static readonly VertexDeclaration VertexDecl = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(28, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2), // Rotation
                new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3) // Corner offset
            );

            VertexDeclaration IVertexType.VertexDeclaration => VertexDecl;
        }

        private ParticleVertex[] particles;
        private short[] quadIndices;
        private int particleCount;
        private const int MaxParticles = 2000;
        private readonly Particle[] activeParticles = new Particle[MaxParticles];
        private int activeCount = 0;
        private readonly Particle[] particlePool = new Particle[MaxParticles];
        private int poolCount = 0;
        private readonly Dictionary<int, Texture2D> particleTextures;
        private readonly ContentManager contentManager;
        private readonly Random random;
        private readonly Dictionary<Texture2D, List<Particle>> onArenaDrawTextureGroups = new();
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
                particlePool[poolCount++] = new Particle(0, Vector2.Zero, Vector2.Zero, 0f, Color.White, Color.White, 1f, 0, 0, 1f);
            }
        }

        public void LoadContent(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _particleTextures = new List<Texture2D>();
            _idToTexture = new Dictionary<int, Texture2D>();
            int textureIndex = 0;
            while (true)
            {
                try
                {
                    var tex = content.Load<Texture2D>("Textures/Particles/t_Particle_" + textureIndex);
                    _particleTextures.Add(tex);
                    _idToTexture[textureIndex] = tex;
                    textureIndex++;
                }
                catch
                {
                    break;
                }
            }

            _particleEffect = content.Load<Effect>("Shaders/ParticleInstance");
            particles = new ParticleVertex[particleCount * 4];
            quadIndices = new short[particleCount * 6];
            for (int i = 0; i < particleCount; i++)
            {
                int vi = i * 4;
                int ii = i * 6;
                quadIndices[ii + 0] = (short)(vi + 0);
                quadIndices[ii + 1] = (short)(vi + 1);
                quadIndices[ii + 2] = (short)(vi + 2);
                quadIndices[ii + 3] = (short)(vi + 2);
                quadIndices[ii + 4] = (short)(vi + 1);
                quadIndices[ii + 5] = (short)(vi + 3);
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

        private int newParticleFrameCounter = 0;

        public Particle NewParticle(int id, Rectangle area, Vector2 velocity, float colorFade, Color startColor, Color endColor, float scale, float lifeTime, int drawLayer, int ai = 0, object owner = null, bool isDirect = true, float rotation = 0f)
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

        public void Draw(GraphicsDevice graphicsDevice, Camera camera, float drawLayer)
        {
            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = new DepthStencilState
            {
                DepthBufferEnable = true,
                DepthBufferWriteEnable = true
            };

            var textureBatches = new Dictionary<int, List<int>>();
            for (int i = 0; i < activeCount; i++)
            {
                if (Math.Abs(activeParticles[i].DrawLayer - drawLayer) > 0.0001f) continue;

                int texId = activeParticles[i].ID;
                if (!textureBatches.TryGetValue(texId, out var list))
                {
                    list = new List<int>(32);
                    textureBatches[texId] = list;
                }
                list.Add(i);
            }

            foreach (var kvp in textureBatches)
            {
                int texId = kvp.Key;
                var indices = kvp.Value;
                int quadCount = indices.Count;
                if (quadCount == 0) continue;

                particles = new ParticleVertex[quadCount * 4];
                quadIndices = new short[quadCount * 6];

                for (int j = 0; j < quadCount; j++)
                {
                    int i = indices[j];
                    var p = activeParticles[i];
                    float invLifeTime = p.TotalLifeTime > 0f ? 1f / p.TotalLifeTime : 1f;
                    float alpha = 1f - (p.CurrentLifeTime * invLifeTime);
                    alpha = MathHelper.Clamp(alpha, 0f, 1f);
                    Color particleColor = p.GetCurrentColor() * alpha;

                    float scaleModifier = texId == 6 ? 40f : 15f;

                    int vbase = j * 4;
                    float zLayer;
                    switch (p.DrawLayer)
                    {
                        case (int)DrawLayer.BelowPlayer:
                            zLayer = 0.29f;
                            break;

                        case (int)DrawLayer.AbovePlayer:
                            zLayer = 0.49f;
                            break;

                        case (int)DrawLayer.OnArena:
                            zLayer = 0.13f;
                            break;

                        default:
                            zLayer = 0.50f;
                            break;
                    }
                    particles[vbase + 0].Position = new Vector3(p.Position, zLayer);
                    particles[vbase + 0].TexCoord = new Vector2(0, 0);
                    particles[vbase + 0].Color = particleColor;
                    particles[vbase + 0].Scale = p.Scale * scaleModifier;
                    particles[vbase + 0].Rotation = p.Rotation;
                    particles[vbase + 0].Corner = new Vector2(-0.5f, -0.5f);

                    particles[vbase + 1].Position = new Vector3(p.Position, zLayer);
                    particles[vbase + 1].TexCoord = new Vector2(1, 0);
                    particles[vbase + 1].Color = particleColor;
                    particles[vbase + 1].Scale = p.Scale * scaleModifier;
                    particles[vbase + 1].Rotation = p.Rotation;
                    particles[vbase + 1].Corner = new Vector2(0.5f, -0.5f);

                    particles[vbase + 2].Position = new Vector3(p.Position, zLayer);
                    particles[vbase + 2].TexCoord = new Vector2(0, 1);
                    particles[vbase + 2].Color = particleColor;
                    particles[vbase + 2].Scale = p.Scale * scaleModifier;
                    particles[vbase + 2].Rotation = p.Rotation;
                    particles[vbase + 2].Corner = new Vector2(-0.5f, 0.5f);

                    particles[vbase + 3].Position = new Vector3(p.Position, zLayer);
                    particles[vbase + 3].TexCoord = new Vector2(1, 1);
                    particles[vbase + 3].Color = particleColor;
                    particles[vbase + 3].Scale = p.Scale * scaleModifier;
                    particles[vbase + 3].Rotation = p.Rotation;
                    particles[vbase + 3].Corner = new Vector2(0.5f, 0.5f);

                    int ii = j * 6;
                    quadIndices[ii + 0] = (short)(vbase + 0);
                    quadIndices[ii + 1] = (short)(vbase + 1);
                    quadIndices[ii + 2] = (short)(vbase + 2);
                    quadIndices[ii + 3] = (short)(vbase + 2);
                    quadIndices[ii + 4] = (short)(vbase + 1);
                    quadIndices[ii + 5] = (short)(vbase + 3);
                }

                int vertexCount = quadCount * 4;
                int indexCount = quadCount * 6;

                _vertexBuffer = new DynamicVertexBuffer(graphicsDevice, ParticleVertex.VertexDecl, vertexCount, BufferUsage.WriteOnly);
                _vertexBuffer.SetData(particles, 0, vertexCount, SetDataOptions.Discard);
                graphicsDevice.SetVertexBuffer(_vertexBuffer);
                Matrix cameraMatrix = camera.TransformMatrix;
                Matrix projectionMatrix = Matrix.CreateOrthographicOffCenter(0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 0, -1, 1);
                _particleEffect.Parameters["WorldViewProjection"].SetValue(cameraMatrix * projectionMatrix);
                _particleEffect.Parameters["ParticleColor"].SetValue(Color.White.ToVector4());
                if (_idToTexture.TryGetValue(texId, out var tex))
                {
                    _particleEffect.Parameters["ParticleTexture"].SetValue(tex);
                }

                foreach (var pass in _particleEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserIndexedPrimitives<ParticleVertex>(
                        PrimitiveType.TriangleList,
                        particles, 0, vertexCount,
                        quadIndices, 0, indexCount / 3
                    );
                }
            }
        }

        public string ParticleCount()
        {
            return $"Active particles: {activeCount}, Pool size: {poolCount}";
        }
    }
}