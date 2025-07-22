using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Leather_Chestplate : Item
    {
        public Leather_Chestplate(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 11;
            Rarity = 1;
            Name = "Leather Chestplate";
            Lore = "'Provides basic protection for your chest'";
            Type = "[Chestplate]";
            Value = 200;
            Defense = 3;
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
            DrawChestplate(spriteBatch, gameTime, player);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PostDraw(spriteBatch, gameTime, player);
        }
    }
}