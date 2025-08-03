using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Iron_Axe : Item
    {
        public Iron_Axe(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 13;
            Rarity = 5;
            Name = "Iron Axe";
            Lore = "'A sword made of brass, slightly better than iron'";
            Type = "[Weapon - Sword]";
            Value = 200;
            Damage = 20;
            UseTime = 0.1f;
            ShootSpeed = 360f;
            Knockback = 500f;
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