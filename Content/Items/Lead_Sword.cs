using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Lead_Sword : Item
    {
        public Lead_Sword(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 3;
            Rarity = 0;
            Name = "Lead Sword";
            Lore = "'A heavy lead sword, slow but powerful'";
            Type = "[Weapon - Sword]";
            Value = 125;
            Damage = 7;
            UseTime = 0.75f;
            ShootSpeed = 110f;
            Knockback = 150f;
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