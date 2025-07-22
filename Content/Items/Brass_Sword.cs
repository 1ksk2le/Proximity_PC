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
            ShootSpeed = 120f;
            Knockback = 200f;
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