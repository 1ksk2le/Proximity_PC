using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Proximity.Content.Items
{
    public class Seers_Spire : Item
    {
        public Seers_Spire(ContentManager contentManager, ParticleManager particleManager, ProjectileProperties projectileProperties) : base(contentManager, particleManager, projectileProperties)
        {
        }

        private class OrbitingSoul
        {
            public float Angle;
            public float OrbitRadius;
            public float OrbitSpeed;
            public Vector2 Offset;
            public int XArm;
            public int PhaseSign;
            public float SpawnTimer = 0f;
            public Vector2 StartPos;
            public bool IsOrbiting = false;
        }

        private List<OrbitingSoul> orbitingSouls = new();
        private float soulSpawnTimer = 0f;
        private const int MaxSouls = 6;
        private const int SoulsPerSpawn = 2;
        private bool wasAttackingLastFrame = false;
        private Vector2 lastAttackDirection = Vector2.UnitX;

        protected override void Initialize()
        {
            ID = 7;
            Rarity = 3;
            Name = "Seer's Spire";
            Lore = "'Never bat an eye...'";
            Type = "[Weapon - Staff]";
            Info = "Holding the attack button causes the staff to emit a wave of souls that orbit the player and releasing the attack button releases the souls";
            Value = 1200;
            Damage = 14;
            UseTime = 0.4f;
            Knockback = 170f;
            ShootSpeed = 400f;
        }

        public override void Update(float deltaTime, GameTime gameTime, Player player)
        {
            base.Update(deltaTime, gameTime, player);
            if (player.IsAttacking)
            {
                soulSpawnTimer += deltaTime;
                lastAttackDirection = player.AttackDirection != Vector2.Zero ? player.AttackDirection : lastAttackDirection;
                if (orbitingSouls.Count < MaxSouls && soulSpawnTimer >= UseTime)
                {
                    int soulsToSpawn = Math.Min(SoulsPerSpawn, MaxSouls - orbitingSouls.Count);
                    for (int i = 0; i < soulsToSpawn; i++)
                    {
                        float angle = (float)random.NextDouble() * MathHelper.TwoPi;
                        float radius = random.Next(40, 65);
                        float speed = random.NextFloat(1.2f, 2.2f);
                        int xArm = random.Next(0, 4);
                        int phaseSign = i % 2 == 0 ? 1 : -1;
                        float staffAngle = player.WeaponHitboxRotation - MathHelper.PiOver2;
                        Vector2 staffTip = player.WeaponHitbox.Center.ToVector2() + new Vector2((float)Math.Cos(staffAngle), (float)Math.Sin(staffAngle)) * (player.WeaponHitbox.Height * 0.5f);
                        orbitingSouls.Add(new OrbitingSoul
                        {
                            Angle = angle,
                            OrbitRadius = radius,
                            OrbitSpeed = speed,
                            XArm = xArm,
                            PhaseSign = phaseSign,
                            StartPos = staffTip,
                            SpawnTimer = 0f,
                            IsOrbiting = false
                        });
                    }
                    soulSpawnTimer = 0f;
                }
            }
            else
            {
                soulSpawnTimer = 0f;
            }

            if (wasAttackingLastFrame && !player.IsAttacking && orbitingSouls.Count > 0)
            {
                float weaponRotation = player.WeaponHitboxRotation - MathHelper.PiOver2;
                Vector2 velocityDir = lastAttackDirection != Vector2.Zero ? Vector2.Normalize(lastAttackDirection) : new Vector2((float)Math.Cos(weaponRotation), (float)Math.Sin(weaponRotation));
                Vector2 spawnPos = player.Position;
                foreach (var soul in orbitingSouls)
                {
                    float baseAngle = (float)Math.Atan2(velocityDir.Y, velocityDir.X);
                    float randomOffset = MathHelper.ToRadians((float)(random.NextDouble() * 20 - 10));
                    float finalAngle = baseAngle + randomOffset;
                    Vector2 randomizedDir = new Vector2((float)Math.Cos(finalAngle), (float)Math.Sin(finalAngle));
                    projectile.NewProjectile(
                        2,
                        0,
                        Damage,
                        Knockback,
                        ShootSpeed + random.Next(-300, 300),
                        0.8f + (float)random.NextFloat(0.2f, 1f),
                        spawnPos + soul.Offset,
                        randomizedDir
                    );
                }
                orbitingSouls.Clear();
            }
            wasAttackingLastFrame = player.IsAttacking;

            float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            for (int i = 0; i < orbitingSouls.Count; i++)
            {
                var soul = orbitingSouls[i];
                float phase = MathHelper.PiOver4 * soul.PhaseSign;
                float armAngle = soul.Angle;
                switch (soul.XArm)
                {
                    case 0: armAngle = soul.Angle; break;
                    case 1: armAngle = MathHelper.Pi - soul.Angle; break;
                    case 2: armAngle = MathHelper.Pi + soul.Angle; break;
                    case 3: armAngle = -soul.Angle; break;
                }
                float rotationSpeed = 2.6f;
                soul.Angle += soul.OrbitSpeed * deltaTime * (rotationSpeed + (float)(random.NextDouble() - 0.5) * 0.2f);
                float x = (float)Math.Cos(armAngle) * (soul.OrbitRadius + 30f);
                float y = (float)Math.Sin(armAngle + phase) * (soul.OrbitRadius + 30f) * 0.8f;
                Vector2 targetOffset = new Vector2(x, y);

                var SpawnDuration = (UseTime / (MaxSouls / SoulsPerSpawn));
                if (!soul.IsOrbiting)
                {
                    soul.SpawnTimer += deltaTime;
                    float t = MathHelper.Clamp(soul.SpawnTimer / SpawnDuration, 0f, 1f);
                    soul.Offset = Vector2.Lerp(soul.StartPos - player.Position, targetOffset, t);
                    if (t >= 1f) soul.IsOrbiting = true;
                }
                else
                {
                    soul.Offset = targetOffset;
                }

                float depth = (soul.Offset.Y + (soul.OrbitRadius + 30f) * 0.8f) / ((soul.OrbitRadius + 30f) * 1.6f);
                float scale = MathHelper.Lerp(0.35f, 0.9f, depth);
                int drawLayer = depth > 0.5f ? (int)DrawLayer.AbovePlayer : (int)DrawLayer.BelowPlayer;

                Vector2 soulPos = player.Position + soul.Offset;
                var pCore = particle.NewParticle(
                    1,
                    new Rectangle((int)(soulPos.X), (int)(soulPos.Y), 1, 1),
                    Vector2.Zero,
                    0.2f,
                    new Color(0, 226, 189, 220),
                    new Color(149, 33, 77, 220),
                    scale * .74f * player.CurrentScale,
                    1f,
                    drawLayer,
                    1,
                    player,
                    true,
                    0f
                );
            }

            var (hitbox, rotation) = CalculateWeaponHitbox(player, gameTime);
            if (hitbox == Rectangle.Empty) return;

            Vector2 weaponCenter = hitbox.Center.ToVector2();
            int particleCount = 8;
            float ovalWidth = 30f;
            float ovalHeight = 10f;
            for (int i = 0; i < particleCount; i++)
            {
                float angle = MathHelper.TwoPi * (i / (float)particleCount) + time * 2f;
                Vector2 localOffset = new Vector2((float)Math.Cos(angle) * ovalWidth, (float)Math.Sin(angle) * ovalHeight - 30f);
                Vector2 rotatedOffset = Vector2.Transform(localOffset, Matrix.CreateRotationZ(0f));
                Vector2 particlePos = weaponCenter + rotatedOffset;

                int drawLayer = (Math.Sin(angle) > 0) ? 1 : 0;

                Rectangle spawnRect = new Rectangle((int)particlePos.X, (int)particlePos.Y, 1, 1);
                var p = particle.NewParticle(
                    4,
                    spawnRect,
                    Vector2.Zero,
                    0.1f,
                    new Color(0, 226, 189, 110),
                    new Color(149, 33, 77, 110),
                    0.8f * player.CurrentScale,
                    0.01f,
                    drawLayer,
                    0,
                    player,
                    true,
                    0f
                );
            }
            for (int i = 0; i < particleCount; i++)
            {
                float angle = MathHelper.TwoPi * (i / (float)particleCount) + time * 2f;
                Vector2 localOffset = new Vector2((float)Math.Cos(angle) * ovalWidth * 2, (float)Math.Sin(angle) * ovalHeight - 30f);
                Vector2 rotatedOffset = Vector2.Transform(localOffset, Matrix.CreateRotationZ(0f));
                Vector2 particlePos = weaponCenter + rotatedOffset;

                int drawLayer = (Math.Sin(angle) > 0) ? 1 : 0;

                Rectangle spawnRect = new Rectangle((int)particlePos.X, (int)particlePos.Y, 1, 1);
                var p = particle.NewParticle(
                    4,
                    spawnRect,
                    Vector2.Zero,
                    0.1f,
                    new Color(0, 226, 189, 110),
                    new Color(149, 33, 77, 110),
                    0.4f * player.CurrentScale,
                    0.1f,
                    drawLayer,
                    0,
                    player,
                    true,
                    0f
                );
            }
        }

        public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PreDraw(spriteBatch, gameTime, player);
            DrawStaffAttack(spriteBatch, gameTime, player);
            DrawStaffIdle(spriteBatch, gameTime, player);

            float pulseTime = (float)gameTime.TotalGameTime.TotalSeconds * 2f;
            float pulse = (float)(0.5 + 0.5 * Math.Sin(pulseTime));
            Color colorA = new Color(0, 226, 189, 110);
            Color colorB = Color.DarkTurquoise;
            Color bloomColor = Color.Lerp(colorA, colorB, pulse);
            foreach (var soul in orbitingSouls)
            {
                float phase = MathHelper.PiOver4 * soul.PhaseSign;
                float armAngle = soul.Angle;
                switch (soul.XArm)
                {
                    case 0: armAngle = soul.Angle; break;
                    case 1: armAngle = MathHelper.Pi - soul.Angle; break;
                    case 2: armAngle = MathHelper.Pi + soul.Angle; break;
                    case 3: armAngle = -soul.Angle; break;
                }
                float x = (float)Math.Cos(armAngle) * (soul.OrbitRadius + 30f);
                float y = (float)Math.Sin(armAngle + phase) * (soul.OrbitRadius + 30f) * 0.8f;
                Vector2 soulPos = player.Position + new Vector2(x, y);
                float depth = (y + (soul.OrbitRadius + 30f) * 0.8f) / ((soul.OrbitRadius + 30f) * 1.6f);
                float scale = MathHelper.Lerp(0.35f, 0.9f, depth);
                int drawLayer = depth > 0.5f ? (int)DrawLayer.AbovePlayer : (int)DrawLayer.BelowPlayer;
                if (drawLayer == (int)DrawLayer.BelowPlayer)
                {
                    spriteBatch.Draw(Main.Bloom, new Rectangle((int)(soulPos.X - 24 * scale), (int)(soulPos.Y - 24 * scale), (int)(48 * scale), (int)(48 * scale)), bloomColor);
                }
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            base.PostDraw(spriteBatch, gameTime, player);
            float pulseTime = (float)gameTime.TotalGameTime.TotalSeconds * 2f;
            float pulse = (float)(0.5 + 0.5 * Math.Sin(pulseTime));
            Color colorA = new Color(0, 226, 189, 110);
            Color colorB = Color.DarkTurquoise;
            Color bloomColor = Color.Lerp(colorA, colorB, pulse);
            foreach (var soul in orbitingSouls)
            {
                float phase = MathHelper.PiOver4 * soul.PhaseSign;
                float armAngle = soul.Angle;
                switch (soul.XArm)
                {
                    case 0: armAngle = soul.Angle; break;
                    case 1: armAngle = MathHelper.Pi - soul.Angle; break;
                    case 2: armAngle = MathHelper.Pi + soul.Angle; break;
                    case 3: armAngle = -soul.Angle; break;
                }
                float x = (float)Math.Cos(armAngle) * (soul.OrbitRadius + 30f);
                float y = (float)Math.Sin(armAngle + phase) * (soul.OrbitRadius + 30f) * 0.8f;
                Vector2 soulPos = player.Position + new Vector2(x, y);
                float depth = (y + (soul.OrbitRadius + 30f) * 0.8f) / ((soul.OrbitRadius + 30f) * 1.6f);
                float scale = MathHelper.Lerp(0.35f, 0.9f, depth);
                int drawLayer = depth > 0.5f ? (int)DrawLayer.AbovePlayer : (int)DrawLayer.BelowPlayer;
                if (drawLayer == (int)DrawLayer.AbovePlayer)
                {
                    spriteBatch.Draw(Main.Bloom, new Rectangle((int)(soulPos.X - 24 * scale), (int)(soulPos.Y - 24 * scale), (int)(48 * scale), (int)(48 * scale)), bloomColor);
                }
            }

            float colorPulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.1f + 0.4f;
            float scalePulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2) * 0.2f + 1.2f;
            Rectangle bloomRect = new Rectangle(
                (int)((player.WeaponHitbox.X + (player.WeaponHitbox.Width / 2) - (int)(50 * scalePulse))),
                (int)((player.WeaponHitbox.Y - (int)(30 * scalePulse))),
                (int)(100 * scalePulse * player.CurrentScale),
                (int)(100 * scalePulse * player.CurrentScale)
            );
            spriteBatch.Draw(Main.Bloom, bloomRect, new Color(0, 226, 189) * colorPulse);
        }
    }
}