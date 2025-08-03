using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Proximity.Content.Items
{
    public class Wooden_Sword : Item
    {
        public Wooden_Sword(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        protected override void Initialize()
        {
            ID = 2;
            Rarity = 0;
            Name = "Wooden Sword";
            Lore = "'A basic wooden training sword'";
            Type = "[Weapon - Sword]";
            Value = 50;
            Damage = 5;
            UseTime = 0.8f;
            ShootSpeed = 100f;
            Knockback = 120f;
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