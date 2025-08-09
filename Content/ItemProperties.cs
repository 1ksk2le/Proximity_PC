using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proximity.Content
{
    public class ItemProperties
    {
        private const string ITEMS_NAMESPACE = "Proximity.Content.Items";

        private readonly Dictionary<int, Item> itemDictionary;
        private readonly ContentManager contentManager;
        private readonly ParticleManager particleManager;
        private readonly ProjectileProperties projectileProperties;
        public Dictionary<int, Item> Items => itemDictionary;
        public IReadOnlyDictionary<int, Prefix> Prefixes => ItemModifier.Prefixes;
        public IReadOnlyDictionary<int, Suffix> Suffixes => ItemModifier.Suffixes;

        public ItemProperties(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties)
        {
            this.contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            this.particleManager = particleManager ?? throw new ArgumentNullException(nameof(particleManager));
            this.projectileProperties = projectileProperties ?? throw new ArgumentNullException(nameof(projectileProperties));
            itemDictionary = new Dictionary<int, Item>();
            InitializeItems();
        }

        public IReadOnlyDictionary<int, Item> GetItems()
        {
            return Items;
        }

        public Item CreateItem(int itemId, int prefixId = 0, int suffixId = 0)
        {
            if (!itemDictionary.TryGetValue(itemId, out var baseItem))
            {
                return null;
            }

            try
            {
                var itemType = baseItem.GetType();
                var newItem = (Item)Activator.CreateInstance(itemType, contentManager, particleManager, projectileProperties);

                ApplyModifiers(newItem, prefixId, suffixId);
                return newItem;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // List to track dropped items in the world
        public List<ItemDrop> DroppedItems { get; } = new List<ItemDrop>();

        // Drops an item at the given position in the world
        public ItemDrop? DropItem(int itemId, int prefixId, int suffixId, Vector2 position)
        {
            var item = CreateItem(itemId, prefixId, suffixId);
            if (item == null)
                return null;
            var drop = new ItemDrop(item, position);
            DroppedItems.Add(drop);
            return drop;
        }

        // Draw all dropped items in the world with levitation, shadow, and name display on pickup proximity
        public void DrawDroppedItems(SpriteBatch spriteBatch, float time, Rectangle playerHitbox, BitmapFont font, Inventory inventory)
        {
            if (Main.Paused)
            {
                time = 0f;
            }
            foreach (var drop in DroppedItems)
            {
                if (drop.Item?.Texture != null)
                {
                    var texture = drop.Item.Texture;
                    var pos = drop.Position;
                    var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

                    // Animation parameters
                    float period = 5f;
                    float t = (float)(time % period) / period;
                    float levitate = (float)Math.Abs(Math.Sin(t * Math.PI * 2));
                    float levitateHeight = 30f * levitate;
                    float shadowScale = 1f * (1.0f + 0.2f * (1 - levitate));
                    float shadowAlpha = 0.3f * (1.0f - 0.3f * levitate);
                    var shadowColor = new Color(0, 0, 0, shadowAlpha);
                    var shadowOffset = new Vector2(0, texture.Height / 2);

                    // Perspective scale
                    float perspectiveScale = 1.0f + 0.2f * levitate;

                    // Calculate dropped item hitbox (centered, scaled)
                    var itemRect = new Rectangle(
                        (int)(pos.X - origin.X * perspectiveScale),
                        (int)(pos.Y - origin.Y * perspectiveScale - levitateHeight),
                        (int)(texture.Width * perspectiveScale),
                        (int)(texture.Height * perspectiveScale)
                    );

                    // Draw shadow
                    spriteBatch.Draw(
                        texture,
                        pos + shadowOffset,
                        null,
                        shadowColor,
                        0f,
                        origin,
                        new Vector2(shadowScale, shadowScale * 0.45f),
                        SpriteEffects.None,
                        0.14f
                    );

                    // Draw the item
                    spriteBatch.Draw(
                        texture,
                        pos - new Vector2(0, levitateHeight),
                        null,
                        Color.White,
                        0f,
                        origin,
                        perspectiveScale,
                        SpriteEffects.None,
                        0.15f
                    );

                    var rarityInfo = drop.Item != null ? Item.GetRarityInfo(drop.Item.Rarity) : Item.GetRarityInfo(0);
                    // If player hitbox intersects, draw item name above
                    if (playerHitbox.Intersects(itemRect))
                    {
                        string itemName = drop.Item.GetName();
                        var nameSize = font.MeasureString(itemName) * perspectiveScale;
                        var namePos = new Vector2(pos.X, pos.Y - levitateHeight - origin.Y * perspectiveScale - nameSize.Y - 8f);
                        // Draw the name centered above the item, with scale
                        font.DrawString(
                            spriteBatch,
                            itemName,
                            namePos - nameSize / 2f,
                            rarityInfo.Color,
                            scale: perspectiveScale
                        );
                    }
                }
            }
        }

        public float GetSpeedModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item => item.SpeedBonus,
                prefix => prefix.SpeedBonus,
                suffix => suffix.SpeedBonus);

        public float GetHealthModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item => item.HealthBonus,
                prefix => prefix.HealthBonus,
                suffix => suffix.HealthBonus);

        public float GetDamageModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item =>
                {
                    float baseDamage = item.Damage;
                    float totalBonus = item.DamageBonus;
                    if (ItemModifier.Prefixes.TryGetValue(prefixId, out var prefix))
                        totalBonus += prefix.DamageBonus;
                    if (ItemModifier.Suffixes.TryGetValue(suffixId, out var suffix))
                        totalBonus += suffix.DamageBonus;
                    return (baseDamage * totalBonus) + baseDamage;
                },
                prefix => 0f,
                suffix => 0f
            );

        public float GetShotSpeedModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item =>
                {
                    float baseShootSpeed = item.ShootSpeed;
                    float totalBonus = item.ShootSpeedBonus;
                    if (ItemModifier.Prefixes.TryGetValue(prefixId, out var prefix))
                        totalBonus += prefix.ShootSpeedBonus;
                    if (ItemModifier.Suffixes.TryGetValue(suffixId, out var suffix))
                        totalBonus += suffix.ShootSpeedBonus;
                    return (baseShootSpeed * totalBonus) + baseShootSpeed;
                },
                prefix => 0f,
                suffix => 0f
            );

        public float GetDefenseModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item =>
                {
                    float baseDamage = item.Defense;
                    float totalBonus = item.DefenseBonus;
                    if (ItemModifier.Prefixes.TryGetValue(prefixId, out var prefix))
                        totalBonus += prefix.DefenseBonus;
                    if (ItemModifier.Suffixes.TryGetValue(suffixId, out var suffix))
                        totalBonus += suffix.DefenseBonus;
                    return (baseDamage * totalBonus) + baseDamage;
                },
                prefix => 0f,
                suffix => 0f
            );

        public float GetKnockbackResistanceModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item =>
                {
                    float baseDamage = item.KnockbackResistance;
                    float totalBonus = item.KnockbackResistanceBonus;
                    if (ItemModifier.Prefixes.TryGetValue(prefixId, out var prefix))
                        totalBonus += prefix.KnockbackResistanceBonus;
                    if (ItemModifier.Suffixes.TryGetValue(suffixId, out var suffix))
                        totalBonus += suffix.KnockbackResistanceBonus;
                    return (baseDamage * totalBonus) + baseDamage;
                },
                prefix => 0f,
                suffix => 0f
            );

        public float GetKnockbackModifier(int itemId, int prefixId, int suffixId) =>
            CalculateModifier(itemId, prefixId, suffixId,
                item =>
                {
                    float baseDamage = item.Knockback;
                    float totalBonus = item.KnockbackBonus;
                    if (ItemModifier.Prefixes.TryGetValue(prefixId, out var prefix))
                        totalBonus += prefix.KnockbackBonus;
                    if (ItemModifier.Suffixes.TryGetValue(suffixId, out var suffix))
                        totalBonus += suffix.KnockbackBonus;
                    return (baseDamage * totalBonus) + baseDamage;
                },
                prefix => 0f,
                suffix => 0f
            );

        private void InitializeItems()
        {
            try
            {
                var itemTypes = typeof(Item).Assembly.GetTypes()
                    .Where(t => t.Namespace == ITEMS_NAMESPACE && t.IsSubclassOf(typeof(Item)));

                foreach (var type in itemTypes)
                {
                    var item = (Item)Activator.CreateInstance(type, contentManager, particleManager, projectileProperties);
                    itemDictionary[item.ID] = item;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize items.", ex);
            }
        }

        private void ApplyModifiers(Item item, int prefixId, int suffixId)
        {
            if (ItemModifier.Prefixes.TryGetValue(prefixId, out var prefix))
                item.Prefix = prefix.Name;
            if (ItemModifier.Suffixes.TryGetValue(suffixId, out var suffix))
                item.Suffix = suffix.Name;
        }

        private float CalculateModifier(int itemId, int prefixId, int suffixId,
            Func<Item, float> itemSelector,
            Func<Prefix, float> prefixSelector,
            Func<Suffix, float> suffixSelector)
        {
            float modifier = 0f;

            if (Items.TryGetValue(itemId, out Item item))
                modifier += itemSelector(item);

            if (ItemModifier.Prefixes.TryGetValue(prefixId, out Prefix prefix))
                modifier += prefixSelector(prefix);

            if (ItemModifier.Suffixes.TryGetValue(suffixId, out Suffix suffix))
                modifier += suffixSelector(suffix);

            return modifier;
        }
    }

    public static class ItemModifier
    {
        public static readonly Dictionary<int, Prefix> Prefixes = new();
        public static readonly Dictionary<int, Suffix> Suffixes = new();

        static ItemModifier()
        {
            InitializePrefixes();
            InitializeSuffixes();
        }

        private static void InitializePrefixes()
        {
            Prefixes.Add(1, new Prefix(1, "Sharp", "Increases damage by 10%") { ShootSpeedBonus = 1f });
        }

        private static void InitializeSuffixes()
        {
            Suffixes.Add(1, new Suffix(1, "of Power", "Increases all stats by 5%") { ShootSpeedBonus = 1f });
        }
    }

    public class Prefix
    {
        public int ID { get; }
        public string Name { get; }
        public string Effect { get; }
        public float KnockbackBonus { get; set; }
        public float SpeedBonus { get; set; }
        public float HealthBonus { get; set; }
        public float DamageBonus { get; set; }
        public float DefenseBonus { get; set; }
        public float KnockbackResistanceBonus { get; set; }
        public float ShootSpeedBonus { get; set; }

        public Prefix(int id, string name, string effect)
        {
            ID = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }
    }

    public class Suffix
    {
        public int ID { get; }
        public string Name { get; }
        public string Effect { get; }
        public float KnockbackBonus { get; set; }
        public float SpeedBonus { get; set; }
        public float HealthBonus { get; set; }
        public float DamageBonus { get; set; }
        public float DefenseBonus { get; set; }
        public float KnockbackResistanceBonus { get; set; }
        public float ShootSpeedBonus { get; set; }

        public Suffix(int id, string name, string effect)
        {
            ID = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }
    }

    // Represents a dropped item and its position in the world
    public struct ItemDrop
    {
        public Item Item;
        public Microsoft.Xna.Framework.Vector2 Position;

        public ItemDrop(Item item, Microsoft.Xna.Framework.Vector2 position)
        {
            Item = item;
            Position = position;
        }
    }
}