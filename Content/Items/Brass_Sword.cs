using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Brass_Sword : Item
    {
        public Brass_Sword(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 1;
            Rarity = 0;
            Name = "Brass Sword";
            Lore = "'A sword made of brass, slightly better than iron'";
            Type = "[Weapon - Sword]";
            Value = 200;
            Damage = 8;
            UseTime = 0.7f;
            SwingRange = 120f;
            Knockback = 200f;
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