using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Iron_Sword : Item
    {
        public Iron_Sword(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 0;
            Rarity = 1;
            Name = "Iron Sword";
            Lore = "'A trustworthy piece of iron'";
            Type = "[Weapon - Sword]";
            Value = 300;
            Damage = 10;
            UseTime = 0.85f;
            SwingRange = 140f;
            Knockback = 250f;
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            base.PreDraw(spriteBatch, gameTime, player, drawLayer);
            DrawSwordAttack(spriteBatch, gameTime, player, drawLayer);
            DrawSwordIdle(spriteBatch, gameTime, player, drawLayer);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player, float drawLayer)
        {
            base.PostDraw(spriteBatch, gameTime, player, drawLayer);
        }
    }
}