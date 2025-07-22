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
            ShootSpeed = 140f;
            Knockback = 250f;
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
            DrawSwordAttack(spriteBatch, gameTime, player);
            DrawSwordIdle(spriteBatch, gameTime, player);
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PostDraw(spriteBatch, gameTime, player);
        }
    }
}