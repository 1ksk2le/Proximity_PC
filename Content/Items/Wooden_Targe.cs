using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Wooden_Targe : Item
    {
        public Wooden_Targe(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 4;
            Rarity = 0;
            Name = "Wooden Targe";
            Lore = "'It used to be a grown tree'";
            Type = "[Offhand]";
            Value = 65;
            Defense = 2;
            KnockbackResistance = 0.1f;
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            base.PreDraw(spriteBatch, gameTime, player, drawLayer);
            DrawOffhandIdle(spriteBatch, gameTime, player, drawLayer);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            base.PostDraw(spriteBatch, gameTime, player, drawLayer);
        }
    }
}