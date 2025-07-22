using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Proximity.Content;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proximity
{
    public class Main : Game
    {
        private const string CONTENT_ROOT = "Content";
        private const float JOYSTICK_SIZE = 250f;
        private const float JOYSTICK_OFFSET = 1.75f;
        private const float JUMP_BUTTON_OFFSET = 2.5f;
        private const float DEBUG_BUTTON_OPACITY = 0.8f;
        private const int DEBUG_BUTTON_BORDER = 2;
        private const int DEBUG_ITEM_BUTTON_WIDTH = 250;
        private const int DEBUG_ITEM_BUTTON_HEIGHT = 80;
        private const int DEBUG_ITEM_BUTTON_SPACING = 20;

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private bool isDisposed;
        private static bool isPaused;

        private Joystick joystickMovement;
        private Joystick joystickAttack;
        private Player player;
        private Camera camera;
        private Arena arena;
        private ItemProperties itemProperties;
        private FPSManager fpsManager;
        private ParticleManager particleManager;
        private ProjectileProperties projectileProperties;
        private NPCProperties npcProperties;
        private FloatingTextManager floatingTextManager;
        private Inventory inventory;
        private Rectangle inventoryButtonRectangle;
        private bool inventoryButtonTouchState;

        private Dictionary<int, Texture2D> tileTextures;
        private Texture2D pixel;
        private Texture2D playerHead;
        private Texture2D playerBody;
        private Texture2D playerEye;
        private Texture2D joystickBase;
        private Texture2D joystickKnob;
        private Texture2D joystickKnobShadow;
        private Texture2D joystickJump;
        private Texture2D bloom;

        private RenderTarget2D renderTarget;
        private Effect grayscaleEffect;

        public static BitmapFont Font { get; private set; }
        public static Texture2D Pixel { get; private set; }
        public static Texture2D Bloom { get; private set; }
        public static Texture2D Shadow { get; private set; }
        public static Vector2 Dimensions { get; private set; }
        public static bool DebugMode { get; private set; }
        public static bool Paused { get; private set; }

        private bool pauseTouchState = false;
        private bool debugTouchState = false;
        private bool pickupButtonTouchState = false;
        private bool spawnItemTouchState = false;
        private bool spawnNPCTouchState = false;

        private readonly Rectangle spawnItemButtonRectangle = new Rectangle(800, 900, 100, 100);
        private readonly Rectangle spawnNPCButtonRectangle = new Rectangle(1000, 900, 100, 100);

        private Rectangle PickupButtonRectangle => new Rectangle(GraphicsDevice.Viewport.Width - 140, GraphicsDevice.Viewport.Height - 140, 120, 120);
        private Rectangle PauseButtonRectangle => new Rectangle(GraphicsDevice.Viewport.Width - 120, 20, 100, 100);

        private readonly Rectangle debugButtonRectangle = new Rectangle(20, 100, 100, 100);
        private Dictionary<string, Rectangle> debugItemButtons = new Dictionary<string, Rectangle>();

        public Main()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
                PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
                PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height,
                SynchronizeWithVerticalRetrace = true
            };
            Content.RootDirectory = CONTENT_ROOT;
            IsMouseVisible = true;
            IsFixedTimeStep = true;
        }

        protected override void Initialize()
        {
            graphics.ApplyChanges();
            Dimensions = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

            InitializeGameObjects();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            LoadGraphics();
            LoadJoysticks();
            LoadPlayer();
        }

        protected override void UnloadContent()
        {
            DisposeResources();
            base.UnloadContent();
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    UnloadContent();
                }
                isDisposed = true;
            }
            base.Dispose(disposing);
        }

        protected override void Update(GameTime gameTime)
        {
            Paused = (inventory.IsOpen || isPaused) ? true : false;

            var touches = TouchPanel.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update InputManager states
            InputManager.Instance.PreUpdate();
            InputManager.Instance.PostUpdate(gameTime);

            bool currentPauseTouch = touches.IsTouching(PauseButtonRectangle);
            if (currentPauseTouch && !pauseTouchState)
            {
                isPaused = !isPaused;
            }

            // PC: Use InputManager for single key press
            if (InputManager.Instance.IsKeySinglePress(Keys.Escape))
            {
                isPaused = !isPaused;
            }

            pauseTouchState = currentPauseTouch;
            if (player != null)
            {
                camera.Update(player, deltaTime, isPaused);
            }

            if (isPaused)
            {
                inventory.IsOpen = false;
                base.Update(gameTime);
                return;
            }

            // PC: Use InputManager for single key press
            if (InputManager.Instance.IsKeySinglePress(Keys.I))
            {
                inventory.IsOpen = !inventory.IsOpen;
            }

            bool currentInventoryTouch = touches.IsTouching(inventoryButtonRectangle);
            if (currentInventoryTouch && !inventoryButtonTouchState)
            {
                inventory.IsOpen = !inventory.IsOpen;
            }
            inventoryButtonTouchState = currentInventoryTouch;

            bool currentNPCTouch = touches.IsTouching(spawnNPCButtonRectangle);
            if (currentNPCTouch && !spawnNPCTouchState)
            {
                Random random = new Random();
                npcProperties.NewNPC(0, new Vector2(random.NextFloat(player.Position.X - 200, player.Position.X + 200), random.NextFloat(player.Position.Y - 200, player.Position.Y + 200)));
            }
            spawnNPCTouchState = currentNPCTouch;

            bool currentItemTouch = touches.IsTouching(spawnItemButtonRectangle);
            if (currentItemTouch && !spawnItemTouchState)
            {
                Random random = new Random();
                itemProperties.DropItem(random.Next(0, itemProperties.Items.Count + 1), random.Next(0, 5), random.Next(0, 5), new Vector2(random.NextFloat(player.Position.X - 200, player.Position.X + 200), random.NextFloat(player.Position.Y - 200, player.Position.Y + 200)));
            }
            spawnItemTouchState = currentItemTouch;

            if (inventory.IsOpen)
            {
                inventory.Update(touches);
                base.Update(gameTime);
                return;
            }
            UpdateDebugInput(touches);
            UpdateGameObjects(gameTime, InputManager.Instance.currentKeyboardState, InputManager.Instance.currentMouseState);

            bool currentPickupTouch = touches.IsTouching(PickupButtonRectangle);
            if (currentPickupTouch && !pickupButtonTouchState)
            {
                inventory.TryPickingItem(player, itemProperties.DroppedItems, floatingTextManager);
            }
            pickupButtonTouchState = currentPickupTouch;

            // PC: Pick up item with F key
            if (InputManager.Instance.IsKeySinglePress(Keys.F))
            {
                inventory.TryPickingItem(player, itemProperties.DroppedItems, floatingTextManager);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Black);
            DrawGameWorld(gameTime);
            DrawUI();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.TransformMatrix);
            floatingTextManager.Draw(spriteBatch, Font);
            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                isPaused ? grayscaleEffect : null
            );
            spriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);

            if (inventory != null && inventory.IsOpen)
            {
                inventory.Draw(spriteBatch);
            }
            spriteBatch.End();

            if (isPaused)
            {
                var pausedText = "PAUSED";
                var pausedSize = Font.MeasureString(pausedText);
                var pausedCenter = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp
                );
                Font.DrawString(
                    spriteBatch,
                    "PAUSED",
                    pausedCenter - pausedSize / 2,
                    Color.Yellow
                );
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        private void InitializeGameObjects()
        {
            var fontTexture = Content.Load<Texture2D>("Textures/UI/t_Font");
            Font = new BitmapFont(fontTexture);

            Tile.InitializeTypes(Content);
            arena = new Arena();
            camera = new Camera(Vector2.Zero, arena.TileSize * arena.SizeX, arena.TileSize * arena.SizeY);
            particleManager = new ParticleManager(Content);
            floatingTextManager = new FloatingTextManager(Font);
            projectileProperties = new ProjectileProperties(Content, particleManager);
            npcProperties = new NPCProperties(Content, particleManager, floatingTextManager);
            itemProperties = new ItemProperties(Content, particleManager, projectileProperties);
            inventory = new Inventory(Content, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            FillInventoryWithAllItems();
        }

        private void FillInventoryWithAllItems()
        {
            if (itemProperties?.Items == null || inventory == null) return;
            int slot = 0;
            foreach (var kvp in itemProperties.Items)
            {
                Random random = new Random();
                var item = kvp.Value;
                if (item == null) continue;
                if (slot >= Inventory.TotalSlots) break;
                var itemCopy = itemProperties.CreateItem(kvp.Key, random.Next(1, 5), random.Next(1, 5)) ?? item;
                inventory.SetItem(slot, itemCopy);
                slot++;
            }
        }

        private void LoadGraphics()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            var fontTexture = Content.Load<Texture2D>("Textures/UI/t_Font");
            Font = new BitmapFont(fontTexture);

            LoadTextures();
            CreatePixelTexture();
            fpsManager = new FPSManager(Font);
            renderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            grayscaleEffect = Content.Load<Effect>("Shaders/Grayscale");
        }

        private void LoadJoysticks()
        {
            var movementPosition = new Vector2(
                joystickBase.Width * JOYSTICK_OFFSET,
                GraphicsDevice.Viewport.Height - joystickBase.Height * JOYSTICK_OFFSET
            );

            var attackPosition = new Vector2(
                GraphicsDevice.Viewport.Width - joystickBase.Width * JOYSTICK_OFFSET,
                GraphicsDevice.Viewport.Height - joystickBase.Height * JOYSTICK_OFFSET
            );

            joystickMovement = new Joystick(joystickBase, joystickKnob, joystickKnobShadow, movementPosition, JOYSTICK_SIZE);
            joystickAttack = new Joystick(joystickBase, joystickKnob, joystickKnobShadow, attackPosition, JOYSTICK_SIZE);
        }

        private void LoadPlayer()
        {
            player = new Player(
                joystickMovement,
                joystickAttack,
                playerHead,
                playerBody,
                playerEye,
                joystickKnobShadow,
                new Vector2(arena.CenterX, arena.CenterY),
                itemProperties,
                particleManager,
                floatingTextManager,
                1f
            );
        }

        private void CreatePixelTexture()
        {
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            Pixel = pixel;
        }

        private void LoadTextures()
        {
            joystickBase = Content.Load<Texture2D>("Textures/UI/t_Joystick_Base");
            joystickKnob = Content.Load<Texture2D>("Textures/UI/t_Joystick_Knob");
            joystickKnobShadow = Content.Load<Texture2D>("Textures/UI/t_Joystick_KnobShadow");
            joystickJump = Content.Load<Texture2D>("Textures/UI/t_Joystick_Jump");
            playerHead = Content.Load<Texture2D>("Textures/Player/t_Player_Head");
            playerBody = Content.Load<Texture2D>("Textures/Player/t_Player_Body");
            playerEye = Content.Load<Texture2D>("Textures/Player/t_Player_Eye");
            bloom = Content.Load<Texture2D>("Textures/UI/t_Bloom");
            Bloom = bloom;
            Shadow = joystickKnobShadow;

            int buttonWidth = 120;
            int buttonHeight = 60;
            int buttonX = (GraphicsDevice.Viewport.Width - buttonWidth) / 2;
            int buttonY = 10;
            inventoryButtonRectangle = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

            LoadTileTextures();
        }

        private void LoadTileTextures()
        {
            tileTextures = new Dictionary<int, Texture2D>();
            for (int i = 0; ; i++)
            {
                string texturePath = $"Textures/Tiles/t_Tile_{i}";
                try
                {
                    var texture = Content.Load<Texture2D>(texturePath);
                    tileTextures[i] = texture;
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private void UpdateDebugInput(TouchCollection touches)
        {
            bool currentTouchState = touches.IsTouching(debugButtonRectangle);

            // Touch input for debug menu
            if (currentTouchState && !debugTouchState)
            {
                DebugMode = !DebugMode;
            }
            debugTouchState = currentTouchState;

            // Mouse input for debug menu
            var mouseState = InputManager.Instance.currentMouseState;
            var prevMouseState = InputManager.Instance.previousMouseState;
            Point mousePoint = new Point(mouseState.X, mouseState.Y);
            bool mouseClicked = mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released;

            // Toggle debug info by clicking debug button
            if (mouseClicked && debugButtonRectangle.Contains(mousePoint))
            {
                DebugMode = !DebugMode;
            }
            // Spawn item by clicking spawn item button
            if (mouseClicked && spawnItemButtonRectangle.Contains(mousePoint))
            {
                Random random = new Random();
                itemProperties.DropItem(random.Next(0, itemProperties.Items.Count + 1), random.Next(0, 5), random.Next(0, 5), new Vector2(random.NextFloat(player.Position.X - 200, player.Position.X + 200), random.NextFloat(player.Position.Y - 200, player.Position.Y + 200)));
            }
            // Spawn NPC by clicking spawn NPC button
            if (mouseClicked && spawnNPCButtonRectangle.Contains(mousePoint))
            {
                Random random = new Random();
                npcProperties.NewNPC(0, new Vector2(random.NextFloat(player.Position.X - 200, player.Position.X + 200), random.NextFloat(player.Position.Y - 200, player.Position.Y + 200)));
            }
            // Cycle item in debug item menu by clicking item buttons
            foreach (var button in debugItemButtons)
            {
                if (mouseClicked && button.Value.Contains(mousePoint))
                {
                    if (Enum.TryParse<EquipmentSlot>(button.Key, out var slot))
                    {
                        CycleItemInSlot(slot);
                    }
                    break;
                }
            }

            // Keyboard input for debug menu (optional, keep for power users)
            if (InputManager.Instance.IsKeySinglePress(Keys.F3))
            {
                DebugMode = !DebugMode;
            }
            if (InputManager.Instance.IsKeySinglePress(Keys.F1))
            {
                Random random = new Random();
                itemProperties.DropItem(random.Next(0, itemProperties.Items.Count + 1), random.Next(0, 5), random.Next(0, 5), new Vector2(random.NextFloat(player.Position.X - 200, player.Position.X + 200), random.NextFloat(player.Position.Y - 200, player.Position.Y + 200)));
            }
            if (InputManager.Instance.IsKeySinglePress(Keys.F2))
            {
                Random random = new Random();
                npcProperties.NewNPC(0, new Vector2(random.NextFloat(player.Position.X - 200, player.Position.X + 200), random.NextFloat(player.Position.Y - 200, player.Position.Y + 200)));
                npcProperties.NewNPC(1, new Vector2(1000, 1000));
            }
            if (InputManager.Instance.IsKeySinglePress(Keys.F4))
            {
                // F4 cycles next (legacy), E for next, Q for previous
                CycleItemInSlot(EquipmentSlot.Weapon);
            }
            if (InputManager.Instance.IsKeySinglePress(Keys.E))
            {
                CycleItemInSlot(EquipmentSlot.Weapon, true); // true = next
            }
            if (InputManager.Instance.IsKeySinglePress(Keys.Q))
            {
                CycleItemInSlot(EquipmentSlot.Weapon, false); // false = previous
            }
            if (InputManager.Instance.IsKeySinglePress(Keys.F5))
            {
                CycleItemInSlot(EquipmentSlot.Offhand);
            }

            UpdateDebugItemMenu(touches);
        }

        private void UpdateDebugItemMenu(TouchCollection touches)
        {
            foreach (var touch in touches)
            {
                if (touch.State == TouchLocationState.Pressed)
                {
                    foreach (var button in debugItemButtons)
                    {
                        if (button.Value.Contains(touch.Position))
                        {
                            if (Enum.TryParse<EquipmentSlot>(button.Key, out var slot))
                            {
                                CycleItemInSlot(slot);
                            }
                            break;
                        }
                    }
                }
            }
        }

        protected void UpdateGameObjects(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            fpsManager.Update(gameTime);
            floatingTextManager.Update(deltaTime);

            //joystickMovement.Update(touches, GetJumpButtonRectangle(), false);
            //joystickAttack.Update(touches, GetJumpButtonRectangle(), true);

            particleManager.Update(deltaTime);
            player.Update(gameTime, arena, keyboardState, mouseState, camera);
            projectileProperties.UpdateProjectiles(gameTime, player);
            npcProperties.UpdateNPCs(gameTime, player, projectileProperties.ActiveProjectiles);

            /*if (touches.IsTouching(GetJumpButtonRectangle()))
            {
                player.Jump();
            }*/
            if (keyboardState.IsKeyDown(Keys.Space))
            {
                player.Jump();
            }
        }

        private void DrawGameWorld(GameTime gameTime)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                camera.TransformMatrix
            );

            arena.Draw(spriteBatch, camera, tileTextures);
            projectileProperties.DrawShadows(spriteBatch, gameTime, player, camera, arena);
            npcProperties.DrawNPCShadows(spriteBatch, gameTime);
            player.DrawShadow(spriteBatch);
            itemProperties.DrawDroppedItems(spriteBatch, (float)gameTime.TotalGameTime.TotalSeconds, player.Hitbox, Font, inventory);
            projectileProperties.PreDrawProjectiles(spriteBatch, gameTime, player, camera, arena);
            npcProperties.PreDrawNPCs(spriteBatch, gameTime, player, camera, arena);
            particleManager.PreDrawParticles(spriteBatch, camera, arena);
            player.DrawBody(spriteBatch, gameTime);
            player.DrawHead(spriteBatch, gameTime);
            projectileProperties.PostDrawProjectiles(spriteBatch, gameTime, player, camera, arena);
            npcProperties.PostDrawNPCs(spriteBatch, gameTime, player, camera, arena);
            particleManager.PostDrawParticles(spriteBatch, camera, arena);
            spriteBatch.End();
        }

        private void DrawUI()
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            if (!isPaused)
            {
                //joystickMovement.Draw(spriteBatch);
                //joystickAttack.Draw(spriteBatch);
                DrawJumpButton();
                fpsManager.Draw(spriteBatch);
                DrawDebugButton();
                DrawDebugInfo();
                DrawInventoryButton();
                DrawSpawnButtons();

                /*spriteBatch.Draw(Main.Pixel, PickupButtonRectangle, Color.LightGreen * 0.7f);
                var text = "PICK UP";
                var textSize = Font.MeasureString(text);
                var textPos = new Vector2(PickupButtonRectangle.Center.X - textSize.X / 2, PickupButtonRectangle.Center.Y - textSize.Y / 2);
                Font.DrawString(spriteBatch, text, textPos, Color.White);*/
            }
            spriteBatch.Draw(pixel, PauseButtonRectangle, Color.Gray * 0.7f);
            var pauseText = "II";
            var textSize2 = Font.MeasureString(pauseText);
            var center = new Vector2(PauseButtonRectangle.X + PauseButtonRectangle.Width / 2, PauseButtonRectangle.Y + PauseButtonRectangle.Height / 2);
            Font.DrawString(
                spriteBatch,
                pauseText,
                center - textSize2 / 2,
                Color.White
            );
            spriteBatch.End();
        }

        private void DrawJumpButton()
        {
            /*var originalPosition = new Vector2(
                GraphicsDevice.Viewport.Width - (joystickBase.Width * JOYSTICK_OFFSET) - joystickJump.Width / 2,
                GraphicsDevice.Viewport.Height - (joystickBase.Height * JOYSTICK_OFFSET) - joystickJump.Height * JUMP_BUTTON_OFFSET
            );

            float scale = player.IsJumping ? 0.8f : 1f;
            var color = new Color(150, 150, 150, 150);

            spriteBatch.Draw(
                joystickJump,
                originalPosition,
                null,
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );*/
        }

        private void DrawDebugInfo()
        {
            if (!DebugMode) return;

            Vector2 position = new Vector2(GraphicsDevice.Viewport.Width - 700, 10);
            float lineHeight = 30f;
            Color textColor = Color.White;

            Font.DrawString(
                spriteBatch,
                "Active NPCs: " + npcProperties.ActiveNPCs.Count.ToString(),
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                "Active projectiles: " + projectileProperties.ActiveProjectiles.Count.ToString(),
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                particleManager.ParticleCount(),
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Player State:",
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Moving: {player.IsMoving}",
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Attacking: {player.IsAttacking}",
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Jumping: {player.IsJumping}",
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Facing Left: {player.IsFacingLeft}",
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Knocked: {player.IsKnocked}",
                position,
                textColor
            );

            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Immune: {player.IsKnocked}",
                position,
                textColor
            );

            position.Y += lineHeight;
            position.Y += lineHeight;

            Font.DrawString(
                spriteBatch,
                $"Player Stats:",
                position,
                textColor
            );

            position.Y += lineHeight;
            if (player.Speed != 100f)
            {
                Font.DrawString(
                    spriteBatch,
                    $"Speed: {player.Speed}",
                    position,
                    textColor
                );

                position.Y += lineHeight;
            }
            if (player.CurrentHealth <= player.MaxHealth)
            {
                Font.DrawString(
                    spriteBatch,
                    $"Health: {player.CurrentHealth}/{player.MaxHealth}",
                    position,
                    textColor
                );

                position.Y += lineHeight;
            }
            if (player.Damage != 0)
            {
                Font.DrawString(
                    spriteBatch,
                    $"Damage: {player.Damage}",
                    position,
                    textColor
                );

                position.Y += lineHeight;
            }
            if (player.Defense != 0)
            {
                Font.DrawString(
                    spriteBatch,
                    $"Defense: {player.Defense}",
                    position,
                    textColor
                );

                position.Y += lineHeight;
            }
            if (player.KnockbackResistance != 0f)
            {
                Font.DrawString(
                    spriteBatch,
                    $"KB Resistance: {player.KnockbackResistance:P0}",
                    position,
                    textColor
                );

                position.Y += lineHeight;
            }
            if (player.Knockback != 0f)
            {
                Font.DrawString(
                    spriteBatch,
                    $"Knockback: {player.Knockback}",
                    position,
                    textColor
                );

                position.Y += lineHeight;
            }
            position.Y += lineHeight * 2;

            foreach (var equipment in player.EquipmentItems)
            {
                if (equipment.Value != null)
                {
                    float iconSize = lineHeight * 0.8f;
                    Vector2 iconPosition = new Vector2(position.X - iconSize - 10, position.Y);
                    spriteBatch.Draw(equipment.Value.Texture,
                        new Rectangle((int)iconPosition.X, (int)iconPosition.Y, (int)iconSize, (int)iconSize),
                        null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);

                    Font.DrawString(
                        spriteBatch,
                        $"{equipment.Key}: {equipment.Value.Name}",
                        position,
                        textColor
                    );

                    position.Y += lineHeight;
                    if (equipment.Value.ID != 0)
                    {
                        Font.DrawString(
                            spriteBatch,
                            $"  ID: {equipment.Value.ID}",
                            position,
                            textColor
                        );

                        position.Y += lineHeight;
                    }
                    if (!string.IsNullOrEmpty(equipment.Value.Prefix))
                    {
                        Font.DrawString(
                            spriteBatch,
                            $"  Prefix: {equipment.Value.Prefix}",
                            position,
                            textColor
                        );

                        position.Y += lineHeight;
                    }
                    if (!string.IsNullOrEmpty(equipment.Value.Suffix))
                    {
                        Font.DrawString(
                            spriteBatch,
                            $"  Suffix: {equipment.Value.Suffix}",
                            position,
                            textColor
                        );

                        position.Y += lineHeight;
                    }

                    position.Y += lineHeight;
                }
            }
        }

        private void DrawDebugButton()
        {
            /* var buttonColor = DebugMode ? Color.White : Color.Gray;
             spriteBatch.Draw(pixel, debugButtonRectangle, buttonColor * DEBUG_BUTTON_OPACITY);
             spriteBatch.DrawRectangleBorder(debugButtonRectangle, Color.White, Color.Black, 0f, DEBUG_BUTTON_BORDER);

             var textPosition = new Vector2(debugButtonRectangle.X + 5, debugButtonRectangle.Y + 10);
             Font.DrawString(
                 spriteBatch,
                 "DBG",
                 textPosition,
                 buttonColor
             );

             DrawDebugItemMenu();*/
        }

        private void DrawSpawnButtons()
        {
            /*spriteBatch.Draw(pixel, spawnItemButtonRectangle, Color.Red);
            spriteBatch.Draw(pixel, spawnNPCButtonRectangle, Color.Blue);

            var textPosition = new Vector2(spawnItemButtonRectangle.X + 5, spawnItemButtonRectangle.Y + 10);
            Font.DrawString(
                spriteBatch,
                "Item",
                textPosition,
                Color.White
            );
            var textPosition2 = new Vector2(spawnNPCButtonRectangle.X + 5, spawnNPCButtonRectangle.Y + 10);
            Font.DrawString(
                spriteBatch,
                "NPC",
                textPosition2,
                Color.White
            );

            DrawDebugItemMenu();*/
        }

        private void DrawDebugItemMenu()
        {
            int startY = debugButtonRectangle.Bottom + 10;
            debugItemButtons.Clear();

            foreach (var slot in player.EquipmentItems.Keys)
            {
                var buttonRect = new Rectangle(
                    debugButtonRectangle.X,
                    startY,
                    DEBUG_ITEM_BUTTON_WIDTH,
                    DEBUG_ITEM_BUTTON_HEIGHT
                );
                debugItemButtons[slot.ToString()] = buttonRect;

                var buttonColor = Color.Gray * 0.7f;
                spriteBatch.Draw(pixel, buttonRect, buttonColor);
                spriteBatch.DrawRectangleBorder(buttonRect, Color.White, Color.Black, 0f, 2);

                var item = player.EquipmentItems[slot];
                string itemName = item != null ? item.GetName() : "Empty";
                var textPosition = new Vector2(buttonRect.X + 5, buttonRect.Y + 5);
                Font.DrawString(
                    spriteBatch,
                    $"{slot}: {itemName}",
                    textPosition,
                    Color.White
                );

                startY += DEBUG_ITEM_BUTTON_HEIGHT + DEBUG_ITEM_BUTTON_SPACING;
            }
        }

        private void DrawInventoryButton()
        {
            /* Color buttonColor = inventory.IsOpen ? Color.White : Color.Gray;
             spriteBatch.Draw(pixel, inventoryButtonRectangle, buttonColor * 0.85f);
             spriteBatch.DrawRectangleBorder(inventoryButtonRectangle, Color.White, Color.Black, 0f, 2);
             var text = inventory.IsOpen ? "CLOSE" : "INV";
             var textSize = Font.MeasureString(text);
             var textPos = new Vector2(
                 inventoryButtonRectangle.X + (inventoryButtonRectangle.Width - textSize.X) / 2,
                 inventoryButtonRectangle.Y + (inventoryButtonRectangle.Height - textSize.Y) / 2
             );
             Font.DrawString(spriteBatch, text, textPos, Color.White);*/
        }

        private Rectangle GetJumpButtonRectangle()
        {
            return new Rectangle(
                (int)(GraphicsDevice.Viewport.Width - (joystickBase.Width * JOYSTICK_OFFSET) - joystickJump.Width / 2),
                (int)(GraphicsDevice.Viewport.Height - (joystickBase.Height * JOYSTICK_OFFSET) - joystickJump.Height * JUMP_BUTTON_OFFSET),
                joystickJump.Width,
                joystickJump.Height
            );
        }

        private void CycleItemInSlot(EquipmentSlot slot)
        {
            CycleItemInSlot(slot, true); // default to next
        }

        // Overload for direction: true = next, false = previous
        private void CycleItemInSlot(EquipmentSlot slot, bool next)
        {
            var currentItem = player.EquipmentItems[slot];
            int currentId = currentItem?.ID ?? 0;
            int newId = next ? FindNextItemId(currentId, slot) : FindPreviousItemId(currentId, slot);
            if (newId != currentId)
            {
                player.EquipItem(newId);
            }
        }

        private int FindPreviousItemId(int currentId, EquipmentSlot slot)
        {
            var availableItems = itemProperties.Items.Values;
            var slotItems = availableItems.Where(item =>
                (item.Type.StartsWith("[Weapon") && slot == EquipmentSlot.Weapon) ||
                (item.Type.StartsWith("[Offhand") && slot == EquipmentSlot.Offhand)
            ).ToList();

            if (slotItems.Count == 0) return currentId;

            int currentIndex = slotItems.FindIndex(item => item.ID == currentId);
            int prevIndex = (currentIndex - 1 + slotItems.Count) % slotItems.Count;
            return slotItems[prevIndex].ID;
        }

        private int FindNextItemId(int currentId, EquipmentSlot slot)
        {
            var availableItems = itemProperties.Items.Values;

            var slotItems = availableItems.Where(item =>
                (item.Type.StartsWith("[Weapon") && slot == EquipmentSlot.Weapon) ||
                (item.Type.StartsWith("[Offhand") && slot == EquipmentSlot.Offhand)
            ).ToList();

            if (slotItems.Count == 0) return currentId;

            int currentIndex = slotItems.FindIndex(item => item.ID == currentId);

            int nextIndex = (currentIndex + 1) % slotItems.Count;
            return slotItems[nextIndex].ID;
        }

        private void DisposeResources()
        {
            if (pixel != null)
            {
                pixel.Dispose();
                pixel = null;
            }

            if (spriteBatch != null)
            {
                spriteBatch.Dispose();
                spriteBatch = null;
            }

            if (renderTarget != null)
            {
                renderTarget.Dispose();
                renderTarget = null;
            }
            if (grayscaleEffect != null)
            {
                grayscaleEffect.Dispose();
                grayscaleEffect = null;
            }
        }
    }
}