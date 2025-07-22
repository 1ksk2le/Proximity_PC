using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Proximity.Content.NPCs
{
    public class Target_Dummy : NPC
    {
        public Target_Dummy(ContentManager contentManager, ParticleManager particleManager, FloatingTextManager floatingTextManager)
            : base(contentManager, particleManager, floatingTextManager)
        {
        }

        protected override void Initialize()
        {
            ID = 1;
            Name = "Target Dummy";
            MaxHealth = 100;
            Damage = 0;
            Defense = 0;
            Knockback = 0f;
            KnockbackResistance = 1f;
            DetectRange = 0f;
            IsImmune = false;

            Color = Color.White;
            TotalFrames = 1;
            Scale = 1f;
        }

        public override void Update(float deltaTime, Player player, IReadOnlyList<Projectile> projectiles)
        {
            base.Update(deltaTime, player, projectiles);
            TexturePosition = Position;
        }

        public override void DrawShadows(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.DrawShadows(spriteBatch, gameTime);
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            base.PreDraw(spriteBatch, gameTime);
            spriteBatch.Draw(Texture, Position - new Vector2(Texture.Width / 2, Texture.Height / 2), null, GetColor());
        }

        public override void Kill()
        {
        }
    }
}