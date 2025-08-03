using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proximity.Content
{
    public class ProjectileProperties
    {
        private const string PROJECTILES_NAMESPACE = "Proximity.Content.Projectiles";
        private readonly Dictionary<int, Projectile> projectileDictionary;
        private readonly List<Projectile> activeProjectiles;
        private readonly Queue<Projectile> projectilePool;
        private readonly ContentManager contentManager;
        private readonly ParticleManager particleManager;
        private const int InitialPoolSize = 200;
        private static int nextProjectileId = 1;
        public IReadOnlyDictionary<int, Projectile> Projectiles => projectileDictionary;
        public IReadOnlyList<Projectile> ActiveProjectiles => activeProjectiles;

        public ProjectileProperties(ContentManager contentManager, ParticleManager particleManager)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            this.particleManager = particleManager ?? throw new ArgumentNullException(nameof(particleManager));
            projectileDictionary = new Dictionary<int, Projectile>();
            activeProjectiles = new List<Projectile>();
            projectilePool = new Queue<Projectile>();
            InitializeProjectiles();
            InitializePool();
        }

        private void InitializePool()
        {
            if (projectileDictionary.Count > 0)
            {
                var defaultProjectile = projectileDictionary.First().Value;
                for (int i = 0; i < InitialPoolSize; i++)
                {
                    var projectile = (Projectile)Activator.CreateInstance(
                        defaultProjectile.GetType(),
                        contentManager,
                        particleManager
                    );
                    projectilePool.Enqueue(projectile);
                }
            }
        }

        public Projectile NewProjectile(int projectileId, int ai, int damage, float knockBack, float speed, float lifeTime, Vector2 position, Vector2 direction)
        {
            if (!projectileDictionary.TryGetValue(projectileId, out var baseProjectile))
            {
                return null;
            }
            Projectile projectile;
            if (projectilePool.Count > 0)
            {
                projectile = projectilePool.Dequeue();
                if (projectile.GetType() != baseProjectile.GetType())
                {
                    projectile = (Projectile)Activator.CreateInstance(
                        baseProjectile.GetType(),
                        contentManager,
                        particleManager
                    );
                }
            }
            else
            {
                projectile = (Projectile)Activator.CreateInstance(
                    baseProjectile.GetType(),
                    contentManager,
                    particleManager
                );
            }
            projectile.IsActive = true;
            projectile.AI = ai;
            projectile.Damage = damage;
            projectile.Knockback = knockBack;
            projectile.Speed = speed;
            projectile.TotalLifeTime = lifeTime;
            projectile.CurrentLifeTime = 0f;
            projectile.Position = position;
            projectile.Direction = direction;
            projectile.Velocity = baseProjectile.Velocity;
            projectile.Penetrate = baseProjectile.Penetrate;
            projectile.UniqueId = nextProjectileId++;
            activeProjectiles.Add(projectile);
            return projectile;
        }

        public void UpdateProjectiles(GameTime gameTime, Player player)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int j = 0; j < activeProjectiles.Count; j++)
            {
                var proj = activeProjectiles[j];
                if (proj != null && proj.IsActive)
                    proj.Update(deltaTime, player);
            }

            int i = 0;
            while (i < activeProjectiles.Count)
            {
                var projectile = activeProjectiles[i];
                if (!projectile.IsActive || projectile.CurrentLifeTime >= projectile.TotalLifeTime)
                {
                    projectile.Kill();
                    projectile.IsActive = false;
                    int lastIdx = activeProjectiles.Count - 1;
                    if (i != lastIdx)
                    {
                        activeProjectiles[i] = activeProjectiles[lastIdx];
                    }
                    activeProjectiles.RemoveAt(lastIdx);
                    projectilePool.Enqueue(projectile);
                }
                else
                {
                    i++;
                }
            }
        }

        public void DrawProjectiles(SpriteBatch spriteBatch, GameTime gameTime, Player player, Camera camera, Arena world)
        {
            Rectangle visibleArea = camera.GetVisibleArea(Main.Dimensions, world);
            var textureGroups = new Dictionary<Texture2D, List<Projectile>>();
            foreach (var projectile in activeProjectiles)
            {
                if (!projectile.IsActive || projectile.Texture == null) continue;
                if (!visibleArea.Contains(projectile.Position.ToPoint())) continue;
                if (!textureGroups.TryGetValue(projectile.Texture, out var list))
                {
                    list = new List<Projectile>();
                    textureGroups[projectile.Texture] = list;
                }
                list.Add(projectile);
            }
            foreach (var kvp in textureGroups)
            {
                foreach (var projectile in kvp.Value)
                {
                    projectile.DrawShadow(spriteBatch, gameTime, 0.40f);
                    projectile.PreDraw(spriteBatch, gameTime, player, 0.41f);
                    projectile.Draw(spriteBatch, gameTime, player, 0.42f);
                    projectile.PostDraw(spriteBatch, gameTime, player, 0.43f);
                }
            }
        }

        private void InitializeProjectiles()
        {
            try
            {
                var projectileTypes = typeof(Projectile).Assembly.GetTypes()
                    .Where(t => t.Namespace == PROJECTILES_NAMESPACE && t.IsSubclassOf(typeof(Projectile)))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {projectileTypes.Count} projectile types");
                foreach (var type in projectileTypes)
                {
                    System.Diagnostics.Debug.WriteLine($"Found projectile type: {type.FullName}");
                    var projectile = (Projectile)Activator.CreateInstance(type, contentManager, particleManager);
                    projectileDictionary[projectile.ID] = projectile;
                    System.Diagnostics.Debug.WriteLine($"Added projectile with ID {projectile.ID}");
                }

                System.Diagnostics.Debug.WriteLine($"Total projectiles in dictionary: {projectileDictionary.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize projectiles: {ex}");
                throw new Exception($"Failed to initialize projectiles: {ex.Message}", ex);
            }
        }
    }
}