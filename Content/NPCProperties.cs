using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proximity.Content
{
    public class NPCProperties
    {
        private readonly Dictionary<int, NPC> npcDictionary;
        private readonly List<NPC> activeNPCs;
        private readonly Queue<NPC> npcPool;
        private readonly ContentManager contentManager;
        private readonly ParticleManager particleManager;
        private readonly FloatingTextManager floatingTextManager;
        private const int InitialPoolSize = 500;
        private static readonly string[] PrefixNames = { "", "Fiery" };

        public IReadOnlyList<NPC> ActiveNPCs => activeNPCs;

        public NPCProperties(ContentManager contentManager, ParticleManager particleManager, FloatingTextManager floatingTextManager)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            this.particleManager = particleManager ?? throw new ArgumentNullException(nameof(particleManager));
            this.floatingTextManager = floatingTextManager ?? throw new ArgumentNullException(nameof(floatingTextManager));
            npcDictionary = new Dictionary<int, NPC>();
            activeNPCs = new List<NPC>();
            npcPool = new Queue<NPC>();
            InitializeNPCs();
            InitializePool();
        }

        private void InitializePool()
        {
            if (npcDictionary.Count > 0)
            {
                var defaultNPC = npcDictionary.First().Value;
                for (int i = 0; i < InitialPoolSize; i++)
                {
                    var npc = (NPC)Activator.CreateInstance(defaultNPC.GetType(), contentManager, particleManager, floatingTextManager);
                    npcPool.Enqueue(npc);
                }
            }
        }

        public string GetPrefixName(int prefixId)
        {
            if (prefixId >= 0 && prefixId < PrefixNames.Length)
                return PrefixNames[prefixId];
            return string.Empty;
        }

        public NPC NewNPC(int npcId, Vector2 position, int prefixId = 0)
        {
            if (!npcDictionary.TryGetValue(npcId, out var baseNPC))
            {
                return null;
            }
            NPC npc;
            if (npcPool.Count > 0)
            {
                npc = npcPool.Dequeue();
                if (npc.GetType() != baseNPC.GetType())
                {
                    npc = (NPC)Activator.CreateInstance(baseNPC.GetType(), contentManager, particleManager, floatingTextManager);
                }
            }
            else
            {
                npc = (NPC)Activator.CreateInstance(baseNPC.GetType(), contentManager, particleManager, floatingTextManager);
            }
            npc.IsActive = true;
            npc.Position = position;
            npc.Health = npc.MaxHealth;
            npc.Prefix = GetPrefixName(prefixId);
            activeNPCs.Add(npc);
            return npc;
        }

        public void UpdateNPCs(GameTime gameTime, Player player, IReadOnlyList<Projectile> projectiles)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int j = 0; j < activeNPCs.Count; j++)
            {
                var npc = activeNPCs[j];
                if (npc != null && npc.IsActive)
                    npc.Update(deltaTime, player, projectiles);
            }

            for (int aIdx = 0; aIdx < activeNPCs.Count; aIdx++)
            {
                var npcA = activeNPCs[aIdx];
                if (!npcA.IsActive) continue;
                Rectangle a = npcA.Hitbox();
                for (int bIdx = 0; bIdx < activeNPCs.Count; bIdx++)
                {
                    if (aIdx == bIdx) continue;
                    var npcB = activeNPCs[bIdx];
                    if (!npcB.IsActive) continue;
                    Rectangle b = npcB.Hitbox();
                    if (a.Intersects(b))
                    {
                        Vector2 diff = npcA.Position - npcB.Position;
                        if (diff.LengthSquared() < 1e-4f) diff = new Vector2(1, 0);
                        diff.Normalize();
                        float pushAmount = 2f;
                        npcA.Position += diff * pushAmount;
                        npcB.Position -= diff * pushAmount;
                    }
                }
            }

            int i = 0;
            while (i < activeNPCs.Count)
            {
                var npc = activeNPCs[i];
                if (!npc.IsActive || npc.Health <= 0)
                {
                    npc.Kill();
                    int lastIdx = activeNPCs.Count - 1;
                    if (i != lastIdx)
                    {
                        activeNPCs[i] = activeNPCs[lastIdx];
                    }
                    activeNPCs.RemoveAt(lastIdx);
                    npcPool.Enqueue(npc);
                }
                else
                {
                    i++;
                }
            }
        }

        public void DrawNPCs(SpriteBatch spriteBatch, GameTime gameTime, Player player, Camera camera, Arena world)
        {
            Rectangle visibleArea = camera.GetVisibleArea(Main.Dimensions, world);
            var textureGroups = new Dictionary<Texture2D, List<NPC>>();
            foreach (var npc in activeNPCs)
            {
                if (!npc.IsActive || npc.Texture == null) continue;
                if (!visibleArea.Contains(npc.Position.ToPoint())) continue;
                if (!textureGroups.TryGetValue(npc.Texture, out var list))
                {
                    list = new List<NPC>();
                    textureGroups[npc.Texture] = list;
                }
                list.Add(npc);
            }
            foreach (var kvp in textureGroups)
            {
                foreach (var npc in kvp.Value)
                {
                    if (npc.TexturePosition.Y <= player.PlayerSpriteHitbox.Center.Y)
                    {
                        npc.DrawShadow(spriteBatch, gameTime, 0.20f);
                        npc.PreDraw(spriteBatch, gameTime, 0.21f);
                        npc.Draw(spriteBatch, gameTime, 0.22f);
                        npc.PostDraw(spriteBatch, gameTime, 0.23f);
                    }
                    else
                    {
                        npc.DrawShadow(spriteBatch, gameTime, 0.40f);
                        npc.PreDraw(spriteBatch, gameTime, 0.41f);
                        npc.Draw(spriteBatch, gameTime, 0.42f);
                        npc.PostDraw(spriteBatch, gameTime, 0.43f);
                    }
                }
            }
        }

        private void InitializeNPCs()
        {
            try
            {
                var npcTypes = typeof(NPC).Assembly.GetTypes()
                    .Where(t => t.Namespace == "Proximity.Content.NPCs" && t.IsSubclassOf(typeof(NPC)))
                    .ToList();

                foreach (var type in npcTypes)
                {
                    var npc = (NPC)Activator.CreateInstance(type, contentManager, particleManager, floatingTextManager);
                    npcDictionary[npc.ID] = npc;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize NPCs: {ex}");
                throw new Exception($"Failed to initialize NPCs: {ex.Message}", ex);
            }
        }
    }
}