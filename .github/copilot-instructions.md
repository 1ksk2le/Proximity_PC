# Proximity Game - AI Agent Instructions

## Project Overview
Proximity is a 2D arena combat game built with MonoGame/XNA. Players engage in dynamic combat using various weapons and items in a tile-based environment.

## File Structure
```
/
├── Core Systems
│   ├── Main.cs                 # Game initialization and loop
│   ├── Program.cs              # Entry point
│   └── Content/
│       ├── Arena.cs           # World/tile management
│       ├── Camera.cs          # View management
│       ├── FPSManager.cs      # Performance monitoring
│       └── Tile.cs            # Tile system
├── Extensions
│   ├── ColorExtensions.cs      # Color manipulation
│   ├── RandomExtensions.cs     # RNG utilities
│   ├── RectangleExtensions.cs  # Geometry helpers
│   ├── SpriteBatchExtensions.cs # Drawing utilities
│   ├── TouchInputExtensions.cs # Input helpers
│   └── Vector2Extensions.cs    # Vector math
├── Game Entities
│   ├── Content/
│   │   ├── Item.cs            # Base item class
│   │   ├── Player.cs          # Player entity
│   │   ├── Projectile.cs      # Projectile system
│   │   └── Items/             # Weapon implementations
│   └── Content/NPCs/          # AI entities
├── UI & Input
│   ├── Content/
│   │   ├── BitmapFont.cs      # Text rendering
│   │   ├── FloatingText.cs    # Damage numbers
│   │   ├── InputManager.cs    # Input handling
│   │   ├── Inventory.cs       # Item management
│   │   └── Joystick.cs        # Touch controls
└── Content Management
    └── Content/
        ├── ItemProperties.cs   # Item data
        ├── NPCProperties.cs    # NPC data
        └── ProjectileProperties.cs # Projectile data
```

### Core Systems Architecture

#### 1. Game Loop (`Main.cs`)
- Game States:
  ```csharp
  protected override void Update(GameTime gameTime)
  {
      float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
      UpdateCamera(deltaTime);
      UpdatePlayer(deltaTime);
      UpdateProjectiles(deltaTime);
      UpdateParticles(deltaTime);
  }
  ```
- Input Processing:
  ```csharp
  private void HandleInput()
  {
      KeyboardState keyboardState = Keyboard.GetState();
      MouseState mouseState = Mouse.GetState();
      UpdateJoysticks(keyboardState, mouseState);
      HandlePlayerInput(keyboardState, mouseState);
  }
  ```
- Performance Monitoring:
  ```csharp
  public class FPSManager
  {
      private const float UPDATE_INTERVAL = 1f;
      private float elapsedTime;
      private int frameCount;
      public float CurrentFPS { get; private set; }
  }
  ```

#### 2. Extensions Library
- Vector2Extensions:
  ```csharp
  public static class Vector2Extensions
  {
      public static float ToRotation(this Vector2 vector)
      {
          return (float)Math.Atan2(vector.Y, vector.X);
      }
      
      public static Vector2 RotatedBy(this Vector2 vector, float rotation)
      {
          return Vector2.Transform(vector, Matrix.CreateRotationZ(rotation));
      }
  }
  ```
- RectangleExtensions:
  ```csharp
  public static class RectangleExtensions
  {
      public static Vector2 GetCenter(this Rectangle rectangle)
      {
          return new Vector2(rectangle.X + rectangle.Width / 2f,
                           rectangle.Y + rectangle.Height / 2f);
      }
  }
  ```
- SpriteBatchExtensions:
  ```csharp
  public static class SpriteBatchExtensions
  {
      public static void DrawLine(this SpriteBatch spriteBatch,
                                Vector2 start, Vector2 end, Color color,
                                float thickness = 1f)
      {
          Vector2 delta = end - start;
          float angle = (float)Math.Atan2(delta.Y, delta.X);
          spriteBatch.Draw(pixel, start, null, color,
                          angle, Vector2.Zero, new Vector2(delta.Length(), thickness),
                          SpriteEffects.None, 0);
      }
  }
  ```

#### 2. World Systems

##### Arena System (`Arena.cs`)
```csharp
public class Arena
{
    private const int TILE_SIZE = 80;
    private const int ARENA_SIZE = 30;
    private readonly Tile[,] tiles;

    public bool IsColliding(Rectangle bounds)
    {
        // Tile-based collision detection
    }

    private void GenerateTerrain()
    {
        // Procedural terrain generation
    }
}
```

##### Camera System (`Camera.cs`)

#### Core Implementation
```csharp
public class Camera
{
    // Zoom Constants
    private const float MIN_ZOOM = 1f;
    private const float MAX_ZOOM = 3f;
    private const float DEFAULT_ZOOM = 0.75f;
    private const float PAUSED_ZOOM = 2.5f;
    private const float ZOOM_TRANSITION_SPEED = 3.0f;
    
    // State
    private Vector2 position;
    private float zoom = DEFAULT_ZOOM;
    private float targetZoom = DEFAULT_ZOOM;
    
    // Screen Shake
    private Vector2 shakeOffset = Vector2.Zero;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;
    
    // Properties
    public Vector2 Center => Position + 
        new Vector2(Main.Dimensions.X / (2 * Zoom), 
                   Main.Dimensions.Y / (2 * Zoom));
                   
    public Matrix TransformMatrix => 
        Matrix.CreateTranslation(new Vector3(-Position, 0)) * 
        Matrix.CreateScale(Zoom, Zoom, 1.0f);
}
```

#### Core Features

1. **Player Following**
```csharp
public void Update(Player player, float deltaTime, bool isPaused)
{
    // Update zoom based on game state
    targetZoom = isPaused ? PAUSED_ZOOM : DEFAULT_ZOOM;
    zoom = MathHelper.Lerp(zoom, targetZoom, 
           ZOOM_TRANSITION_SPEED * deltaTime);
    
    // Follow player with bounds checking
    Vector2 screenSize = Main.Dimensions;
    Vector2 targetPosition = player.Position - 
                           (screenSize / (2f * zoom));
    
    Position = new Vector2(
        MathHelper.Clamp(targetPosition.X, 0, maxX),
        MathHelper.Clamp(targetPosition.Y, 0, maxY)
    );
}
```

2. **Screen Shake Effects**
```csharp
public void Shake(float intensity, float duration)
{
    shakeIntensity = intensity;
    shakeDuration = duration;
}

// In Update method:
if (shakeDuration > 0f)
{
    shakeOffset = new Vector2(
        random.NextFloat(-1, 1) * shakeIntensity,
        random.NextFloat(-1, 1) * shakeIntensity
    );
    shakeDuration -= deltaTime;
}
```

3. **Coordinate Transformations**
```csharp
public Rectangle GetVisibleArea(Vector2 screenSize, Arena world)
{
    float visibleWidth = screenSize.X / Zoom;
    float visibleHeight = screenSize.Y / Zoom;
    
    return new Rectangle(
        (int)Position.X,
        (int)Position.Y,
        (int)visibleWidth,
        (int)visibleHeight
    );
}

public Vector2 ScreenToWorld(Vector2 screenPosition)
{
    return Vector2.Transform(screenPosition, 
           Matrix.Invert(TransformMatrix));
}
```

#### Key Features
- Smooth player following with bounds checking
- Dynamic zoom levels for different game states
- Screen shake system for impact feedback
- World/screen coordinate conversion
- Visible area calculations for culling
- Interpolated camera movement
        // Smooth camera following
    }

    public Rectangle GetVisibleArea()
    {
        // Viewport culling calculation
    }
}
```

#### 3. Entity Framework

##### Player System (`Player.cs`)
```csharp
public class Player
{
    // Base Stats
    public const float BASE_SCALE = 1.0f;
    public const float BASE_SPEED = 100f;
    public const float BASE_KNOCKBACK = 0f;
    public const int BASE_HEALTH = 100;
    public const int BASE_DAMAGE = 0;
    public const int BASE_DEFENSE = 0;
    public const float BASE_KNOCKBACK_RESISTANCE = 0f;
    public const float BASE_IMMUNITY_TIME = 3f;

    // Movement Constants
    public const float JUMP_TIME_VALUE = 0.75f;
    public const float JUMP_BOUNCE_HEIGHT = 100f;
    public const float JUMP_SCALE = 1.3f;
    public const float WALK_BOUNCE_HEIGHT = 15f;
    public const float WALK_HEAD_BOUNCE_HEIGHT = 7f;
    public const float SPEED_MULTIPLIER = 0.06f;

    // Visual Effects
    public const float SHADOW_OPACITY = 0.6f;
    public const float SHADOW_SCALE = 0.75f;
    public const float EYE_BLINK_INTERVAL_MIN = 0.2f;
    public const float EYE_BLINK_INTERVAL_MAX = 3.0f;
    public const float HEAD_ROTATION_LIMIT = 60f;
    public const float HEAD_VERTICAL_OFFSET = 5f;

    // Equipment System
    private readonly Dictionary<EquipmentSlot, Item> Equipment;
    public enum EquipmentSlot
    {
        Weapon,
        Chestplate,
        Offhand,
        Helmet
    }

    // State Management
    public bool IsAttacking;
    public bool IsMoving;
    public bool IsFacingLeft;
    public bool IsKnocked;
    public bool IsImmune;
    public bool IsJumping { get; private set; }

    // Core Update Loop
    public void Update(GameTime gameTime, Arena arena, 
                      KeyboardState keyboardState, MouseState mouseState, 
                      Camera camera = null)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update Systems
        particle.Update(deltaTime);
        UpdateJump(deltaTime);
        UpdateKnockback(deltaTime);
        UpdateMovement(deltaTime, arena, keyboardState, mouseState);
        UpdateEyeBlink(deltaTime);
        UpdateAttack(deltaTime, camera);
        UpdateHurtEffect(deltaTime);
        UpdateHitboxes(gameTime);
        UpdateEquippedItems(deltaTime, gameTime, this);
        
        // Update Center Position
        Center = Position + new Vector2(T_Body.Width / 2, T_Body.Height / 2);
    }

    // Layered Drawing System
    public void DrawBelowBody(SpriteBatch spriteBatch, GameTime gameTime)
    {
        // Draw items below body
        foreach (var item in Equipment.Values)
        {
            if (item?.DrawSlot == DrawSlot.BelowBody)
            {
                item.PreDraw(spriteBatch, gameTime, this);
                item.PostDraw(spriteBatch, gameTime, this);
            }
        }
    }
}
```

Features:
- Equipment system with multiple slots
- Detailed state management for animations
- Advanced hitbox system
- Particle effect integration
- Layered drawing system
- Animation effects (blinking, head rotation)
- Stat calculation system
- Combat and movement systems

##### NPC System (`NPCs/*.cs`)

#### Base Properties (`NPC.cs`)
```csharp
public abstract class NPC
{
    // Core Stats
    public int ID { get; protected set; }
    public string Name { get; protected set; }
    public int MaxHealth { get; protected set; }
    public int Damage { get; protected set; }
    public int Defense { get; protected set; }
    public float Knockback { get; protected set; }
    public float KnockbackResistance { get; protected set; }
    public float DetectRange { get; protected set; }
    
    // State
    public bool IsImmune { get; protected set; }
    public bool IsActive { get; protected set; }
    public Color Color { get; protected set; }
    public int TotalFrames { get; protected set; }
}
```

#### Example Implementation: Slime NPC
```csharp
public class Slime : NPC
{
    // Animation Constants
    private const float FrameDuration = 0.12f;
    private int animationFrame;
    private float animationTimer;
    
    // Movement Constants
    private const float DashCooldown = 3f;
    private const float DashTime = 1f;
    private const float DashSpeed = 300f;
    private const float DashBounceHeight = 160f;
    private const float WalkSpeed = 50f;
    private const float WalkBounceHeight = 20f;
    
    // AI States
    private float aiTimer;
    private float dashDuration;
    private bool isDashing;
    private Vector2 dashDirection;

    protected override void Initialize()
    {
        ID = 0;
        Name = "Slime";
        MaxHealth = 20;
        Damage = 10;
        Defense = 2;
        Knockback = 500f;
        DetectRange = 600f;
        // Random color for variety
        Color = new Color(random.Next(255), random.Next(255), 
                         random.Next(255), 200) * 0.5f;
    }

    public override void Update(float deltaTime, Player player, 
                              IReadOnlyList<Projectile> projectiles)
    {
        // AI State Machine
        if (isDashing)
        {
            // Execute dash attack
            Position += dashDirection * DashSpeed * deltaTime;
            if (dashDuration >= DashTime)
            {
                isDashing = false;
                SpawnLandingEffect();
            }
        }
        else if (aiTimer >= DashCooldown && IsReadyToDash())
        {
            // Initiate dash attack
            dashDirection = Vector2.Normalize(player.Position - Position);
            isDashing = true;
            dashDuration = 0f;
        }
        else if (InDetectionRange(player))
        {
            // Normal following behavior
            Vector2 direction = Vector2.Normalize(player.Position - Position);
            Position += direction * WalkSpeed * deltaTime;
            UpdateTrailEffects();
        }

        // Visual Updates
        UpdateBobAnimation();
        UpdateSpriteAnimation();
    }

    // Visual Effects
    public override void DrawShadows(SpriteBatch spriteBatch, GameTime gameTime)
    {
        float jumpProgress = CalculateJumpProgress();
        float shadowScale = 0.9f - 0.3f * jumpProgress;
        float shadowAlpha = 0.5f - (jumpProgress * 0.2f);
        
        DrawShadowSprite(spriteBatch, shadowScale, shadowAlpha);
    }
}
```

#### Key Features
1. **AI Behaviors**
   - State-based decision making
   - Player detection and following
   - Special attack patterns (dash, projectiles)
   - Customizable parameters per NPC type

2. **Animation System**
   - Frame-based sprite animation
   - Movement-based bob animations
   - Attack-specific animations
   - Dynamic shadow effects

3. **Visual Effects**
   - Particle trails during movement
   - Impact effects on attacks
   - Dynamic shadows with scaling/alpha
   - Color variations for variety

4. **Combat Integration**
   - Hitbox-based collision detection
   - Health and damage system
   - Knockback mechanics
   - Particle effects for feedback
        // Movement and pathfinding
    }

    protected virtual void UpdateCombat(float deltaTime)
    {
        // Combat decisions and actions
    }
}
```

##### Projectile System (`Projectile.cs`)
```csharp
public abstract class Projectile
{
    // Core Properties
    public int ID { get; protected set; }
    public int AI { get; set; }
    public int Damage { get; set; }
    public int Penetrate { get; set; }
    public float Speed { get; set; }
    public float Knockback { get; set; }
    public float Scale { get; set; } = 1f;
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    public Vector2 HitboxOffset { get; protected set; }

    // State Management
    public bool IsActive { get; set; }
    public float CurrentLifeTime { get; set; }
    public float TotalLifeTime { get; set; }

    // AI Types
    // 0: Linear movement with constant direction
    // 1: Ballistic movement with gravity
    public virtual void Update(float deltaTime, Player player)
    {
        if (AI == 0)
        {
            // Linear movement
            Position += Vector2.Normalize(Direction) * deltaTime * Speed;
            Rotation = (float)Math.Atan2(Direction.Y, Direction.X);
        }
        else if (AI == 1)
        {
            // Ballistic movement
            Position += Vector2.Normalize(Direction) * deltaTime * Speed;
            Velocity += new Vector2(0, 500f * deltaTime); // Gravity
            Position += Velocity * deltaTime;
            Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);
        }
    }

    // Collision System
    public Rectangle Hitbox()
    {
        // Rotated hitbox calculation
        Vector2 center = Position + HitboxOffset;
        Vector2[] corners = GetRotatedRectangleCorners(
            center,
            Texture.Width * Scale,
            Texture.Height * Scale,
            HitboxRotation
        );
        // Calculate AABB from rotated corners
        // ...
    }
}
```

Features:
- Abstract base class for all projectiles
- Flexible AI system for different movement patterns
- Accurate rotated hitbox collision detection
- Particle system integration for effects
- Penetration system for piercing multiple targets
- Debug visualization support
- Shadow rendering for visual depth

#### 4. UI & Input Systems

##### Input Management (`InputManager.cs`)
```csharp
public class InputManager
{
    // State Management
    public KeyboardState currentKeyboardState;
    public KeyboardState previousKeyboardState;
    public MouseState currentMouseState;
    public MouseState previousMouseState;
    public Vector2 mousePosition;

    // Singleton Pattern
    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new InputManager();
            }
            return instance;
        }
    }

    // Update Cycle
    public void PreUpdate()
    {
        previousKeyboardState = currentKeyboardState;
        previousMouseState = currentMouseState;
    }

    public void PostUpdate(GameTime gameTime)
    {
        currentKeyboardState = Keyboard.GetState();
        currentMouseState = Mouse.GetState();
        mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y) * 
            (float)gameTime.ElapsedGameTime.TotalSeconds * 60f;
    }

    // Input Detection Methods
    public bool IsKeyDown(Keys key)
    {
        return currentKeyboardState.IsKeyDown(key);
    }

    public bool IsKeySinglePress(Keys key)
    {
        return currentKeyboardState.IsKeyDown(key) && 
               !previousKeyboardState.IsKeyDown(key);
    }

    public bool IsButtonSingleClick(bool leftClick)
    {
        return leftClick
            ? currentMouseState.LeftButton == ButtonState.Pressed && 
              previousMouseState.LeftButton == ButtonState.Released
            : currentMouseState.RightButton == ButtonState.Pressed && 
              previousMouseState.RightButton == ButtonState.Released;
    }

    // Text Input Support
    public string GetPressedKeys()
    {
        Keys[] pressedKeys = currentKeyboardState.GetPressedKeys();
        StringBuilder keysStringBuilder = new StringBuilder();

        foreach (Keys key in pressedKeys)
        {
            if (!IsModifierKey(key) && !IsSpecialKey(key) && 
                IsKeySinglePress(key))
            {
                string keyString = GetKeyString(key);
                if (!string.IsNullOrEmpty(keyString))
                {
                    keysStringBuilder.Append(keyString);
                }
            }
        }

        return keysStringBuilder.ToString();
    }
}
```

Features:
- Singleton pattern for global access
- Keyboard and mouse state tracking
- Single press/click detection
- Text input support
- Special key handling
- Frame-based input updates
- Modifier key detection

##### Text Rendering (`BitmapFont.cs`)
```csharp
public class BitmapFont
{
    private readonly Texture2D fontTexture;
    private readonly Rectangle[] charRects;

    public void DrawText(SpriteBatch spriteBatch, string text, Vector2 position,
                        Color color, float scale = 1f)
    {
        // Character-by-character rendering
        Vector2 currentPos = position;
        foreach (char c in text)
        {
            DrawChar(spriteBatch, c, currentPos, color, scale);
            currentPos.X += GetCharWidth(c) * scale;
        }
    }
}
```

##### Inventory System (`Inventory.cs`)

#### Core Structure
```csharp
public class Inventory
{
    // Layout Constants
    public const int Columns = 5;
    public const int Rows = 20;
    public const int TotalSlots = Columns * Rows;
    public const int VisibleRows = 5;
    public const int VisibleSlots = Columns * VisibleRows;
    private const int SlotPixelSize = 120;

    // Storage and State
    private readonly List<Item> items;
    private float scrollOffset;
    private int? selectedItemSlot = null;
    
    // Rendering Resources
    private readonly Texture2D slotTexture;
    private readonly Texture2D thumbTexture;
    private RenderTarget2D inventoryRenderTarget;
    private RenderTarget2D infoBoxRenderTarget;
}
```

#### Key Features

1. **Grid Layout System**
- 5 columns × 20 rows total grid
- 5 visible rows at a time
- Smooth scrolling with momentum
- Custom slot sizing and spacing
- Scrollbar with dynamic thumb size

2. **Item Display**
```csharp
private void DrawItemInSlot(SpriteBatch batch, Item item, Rectangle slotRect)
{
    // Draw slot background with rarity color
    Color slotColor = item != null ? Item.GetRarityInfo(item.Rarity).Color : Color.White;
    batch.Draw(slotTexture, slotRect, srcRect, slotColor);
    
    if (item?.Texture != null)
    {
        // Calculate item scale to fit slot
        float scale = Math.Min(itemAreaSize / width, itemAreaSize / height);
        if (item.Type.Contains("[Armor]")) scale *= 0.8f; // Smaller for armor
        
        // Draw item shadow and item
        batch.Draw(item.Texture, shadowRect, null, Color.Black * 0.3f);
        batch.Draw(item.Texture, itemRect, null, Color.White);
    }
}
```

3. **Item Info Panel**
- Dynamic layout based on content
- Item icon with pulsing animation
- Name with rarity color
- Type and category information
- Stats with icons:
  - Damage/Defense
  - Speed/Knockback
  - Special effects
- Word-wrapped lore text
- Scrollable for long descriptions

4. **Visual Effects**
- Rarity-colored slot backgrounds
- Item shadows for depth
- Scale adjustments by item type
- Smooth scrolling animations
- Dynamic scrollbar sizing
- Item hover highlights

5. **Touch Controls**
```csharp
public void HandleTouch(TouchCollection touches)
{
    // Track touch for scrolling
    if (touches.Count > 0)
    {
        float touchDelta = currentTouch.Position.Y - lastTouchY;
        scrollOffset -= touchDelta / SlotPixelSize;
        scrollOffset = MathHelper.Clamp(scrollOffset, 0, maxScroll);
    }
    
    // Detect slot selection
    if (touches.Count > 0 && touches[0].State == TouchLocationState.Released)
    {
        int slotIndex = GetSlotAtPosition(touches[0].Position);
        if (slotIndex >= 0) selectedItemSlot = slotIndex;
    }
}
```

#### Implementation Notes

1. **Performance Optimizations**
- Uses RenderTarget2D for smooth scrolling
- Batches item drawings by texture
- Only renders visible slots plus padding
- Caches text measurements
- Reuses memory buffers

2. **Memory Management**
```csharp
public void Dispose()
{
    inventoryRenderTarget?.Dispose();
    infoBoxRenderTarget?.Dispose();
    // Clean up other resources...
}
```

3. **Layout Calculations**
- Centered on screen
- Dynamic slot sizing
- Automatic text wrapping
- Padding and spacing constants
- Scrollbar proportions

4. **Rendering Pipeline**
```csharp
public void Draw(SpriteBatch spriteBatch)
{
    if (!IsOpen) return;
    
    // 1. Render inventory grid to texture
    graphicsDevice.SetRenderTarget(inventoryRenderTarget);
    graphicsDevice.Clear(Color.Transparent);
    using (SpriteBatch rtBatch = new SpriteBatch(graphicsDevice))
    {
        rtBatch.Begin();
        
        // Draw visible slots plus padding for smooth scrolling
        for (int row = 0; row <= VisibleRows + 1; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                DrawSlot(rtBatch, row, col);
                if (items[slotIndex] != null)
                    DrawItem(rtBatch, items[slotIndex], slotRect);
            }
        }
        rtBatch.End();
    }
    
    // 2. Draw scrollable inventory to screen
    graphicsDevice.SetRenderTarget(null);
    Rectangle destRect = GetInventoryScreenRect();
    Rectangle sourceRect = GetScrolledSourceRect();
    spriteBatch.Draw(inventoryRenderTarget, destRect, sourceRect, Color.White);
    
    // 3. Draw scrollbar
    DrawScrollbar(spriteBatch, destRect);
    
    // 4. Draw item info panel if item selected
    if (selectedItemSlot.HasValue)
        DrawItemInfo(spriteBatch, items[selectedItemSlot.Value]);
}
```

Key Rendering Features:
- Uses render targets for smooth scrolling
- Double-buffered drawing for clean transitions
- Calculates visible region based on scroll offset
- Dynamic scrollbar thumb sizing and positioning
- Separate info panel with own render target
- Efficient texture batching and state management
        {
            // Find existing stack
        }
        
        // Find empty slot
        var emptySlot = slots.FirstOrDefault(s => s.IsEmpty);
        if (emptySlot != null)
        {
            emptySlot.SetItem(item);
        }
    }
}
```

#### 5. Content Management

##### Item Properties (`ItemProperties.cs`)
```csharp
public class ItemProperties
{
    private readonly Dictionary<int, Item> items;

    public void LoadItems()
    {
        // Register item types
        RegisterItem<Wooden_Sword>();
        RegisterItem<Iron_Sword>();
        RegisterItem<Emberwood_Staff>();
        // ...
    }

    public Item CreateItem(int id)
    {
        // Create new instance of item
        if (items.TryGetValue(id, out Item template))
        {
            return template.Clone();
        }
        return null;
    }
}
```

##### Particle System (`ParticleManager.cs`)
```csharp
public class ParticleManager
{
    private readonly List<Particle> activeParticles;
    private readonly Queue<Particle> particlePool;

    public Particle NewParticle(int type, Rectangle bounds, Vector2 velocity,
                              float opacity, Color startColor, Color endColor,
                              float scale, float lifetime, int layer)
    {
        // Get/create particle
        Particle particle = GetParticle();
        
        // Initialize properties
        particle.Initialize(type, bounds, velocity, opacity,
                          startColor, endColor, scale, lifetime, layer);
        
        return particle;
    }

    private void UpdateParticles(float deltaTime)
    {
        // Update active particles
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = activeParticles[i];
            
            // Update lifetime
            particle.Update(deltaTime);
            
            // Return to pool if dead
            if (!particle.IsAlive)
            {
                ReturnToPool(particle);
                activeParticles.RemoveAt(i);
            }
        }
    }
}
```

##### Projectile Properties (`ProjectileProperties.cs`)
```csharp
public class ProjectileProperties
{
    private readonly Dictionary<int, ProjectileType> projectileTypes;

    public void RegisterProjectileType(int id, string name,
                                     float speed, float scale,
                                     bool destroyOnHit)
    {
        projectileTypes[id] = new ProjectileType
        {
            Name = name,
            Speed = speed,
            Scale = scale,
            DestroyOnHit = destroyOnHit
        };
    }
}
```

### Technical Implementation

#### 1. Rendering Pipeline
```csharp
protected override void Draw(GameTime gameTime)
{
    // Clear screen
    GraphicsDevice.Clear(Color.Black);

    // World rendering
    spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp,
                     null, null, null, camera.TransformMatrix);
    
    // Draw order:
    // 1. Background tiles
    arena.Draw(spriteBatch);
    
    // 2. Below-player effects
    particleManager.DrawLayer(DrawLayer.BelowPlayer);
    
    // 3. Player and entities
    player.Draw(spriteBatch);
    foreach (var npc in npcs) npc.Draw(spriteBatch);
    
    // 4. Above-player effects
    particleManager.DrawLayer(DrawLayer.AbovePlayer);
    
    spriteBatch.End();

    // UI rendering (separate pass)
    spriteBatch.Begin();
    DrawUI();
    spriteBatch.End();
}
```

#### 2. Physics System
```csharp
public class PhysicsSystem
{
    public void ResolveCollision(Entity entity)
    {
        // Get surrounding tiles
        var bounds = entity.Bounds;
        var tiles = arena.GetSurroundingTiles(bounds);

        foreach (var tile in tiles)
        {
            if (!tile.Walkable && bounds.Intersects(tile.Bounds))
            {
                // Push entity out of collision
                Vector2 depth = GetCollisionDepth(bounds, tile.Bounds);
                entity.Position += depth;
                
                // Apply any collision effects
                OnCollision(entity, tile);
            }
        }
    }
}
```

#### 3. Combat Mathematics
```csharp
public class CombatSystem
{
    public float CalculateDamage(float baseDamage, float critChance,
                               float critMultiplier, float defense)
    {
        float damage = baseDamage;
        
        // Apply critical hit
        if (random.NextDouble() < critChance)
        {
            damage *= critMultiplier;
        }
        
        // Apply defense reduction
        damage *= (1f - defense);
        
        return damage;
    }

    public Vector2 CalculateKnockback(Vector2 sourcePos, Vector2 targetPos,
                                    float knockbackForce)
    {
        Vector2 direction = Vector2.Normalize(targetPos - sourcePos);
        return direction * knockbackForce;
    }
}
```

#### 4. Asset Management
```csharp
public class AssetManager
{
    private readonly ContentManager content;
    private readonly Dictionary<string, Texture2D> textures;
    
    public void LoadContent()
    {
        // Load textures with error handling
        foreach (var assetPath in assetPaths)
        {
            try
            {
                textures[assetPath] = content.Load<Texture2D>(assetPath);
            }
            catch (ContentLoadException e)
            {
                // Log error and load fallback texture
                LoadFallbackTexture(assetPath);
            }
        }
    }

    public void UnloadContent()
    {
        // Proper cleanup
        foreach (var texture in textures.Values)
        {
            texture.Dispose();
        }
        textures.Clear();
    }
}
```

### Build & Debug
- Use `dotnet build` and `dotnet run` for development
- Debug configuration provides additional testing features
- MonoGame content pipeline handles asset compilation

## Key Components

### Item System
- Base `Item` class in `Content/Item.cs` defines core item behavior
- Core Properties:
  ```csharp
  public class Item
  {
      // Core Stats
      public int ID { get; protected set; }
      public int Rarity { get; protected set; }
      public int Damage { get; protected set; }
      public int Defense { get; protected set; }
      
      // Equipment Properties
      public DrawSlot DrawSlot { get; protected set; }
      public float UseTime { get; protected set; }
      public float ShootSpeed { get; set; }
      public float Knockback { get; set; }
      
      // Item Info
      public string Name { get; protected set; }
      public string Type { get; protected set; }
      public string Lore { get; protected set; }
      
      // Stats Bonuses
      public float SpeedBonus { get; protected set; }
      public float HealthBonus { get; protected set; }
      public float DamageBonus { get; protected set; }
  }
  ```

- Item Categories:
  - `[Weapon - Sword]`: Melee weapons with swing animations (e.g., `Iron_Sword.cs`)
  - `[Weapon - Staff]`: Magical weapons with projectiles and effects (e.g., `Emberwood_Staff.cs`)
  - `[Weapon - Pistol]`/`[Weapon - Rifle]`: Ranged weapons with recoil and spread (e.g., `Makeshift_Shotgun.cs`)
  - `[Offhand]`: Secondary items like shields (e.g., `Wooden_Targe.cs`)
  - `[Chestplate]`: Body armor with defense bonuses
  - `[Helmet]`: Head armor with various effects

- Core Methods:
  - `Initialize()`: Set base stats and properties
  - `Use(deltaTime, player, direction)`: Handle attack/use logic
  - `Update(deltaTime, gameTime, player)`: State updates
  - `PreDraw/PostDraw`: Layered rendering
  - `UpdateHitboxes`: Combat collision system

- Rarity System:
  ```csharp
  public static readonly (string Name, Color Color)[] Rarities = new[]
  {
      ("Rubbish", Color.Silver),
      ("Common", Color.White),
      ("Uncommon", Color.PaleGreen),
      ("Rare", Color.CornflowerBlue),
      ("Epic", Color.MediumOrchid),
      ("Legendary", Color.Coral),
      ("Mythical", Color.Turquoise)
  };
  ```

### Draw System
- Items use different draw methods based on type:
  - `DrawSwordAttack/DrawSwordIdle`: Melee weapon animations
  - `DrawStaffAttack/DrawStaffIdle`: Staff animations with effects
  - `DrawGunAttack/DrawGunIdle`: Gun animations with recoil
- Rendering considers:
  - Player facing direction (`IsFacingLeft`)
  - Animation state (attack progress, idle movement)
  - Visual effects (particles, trails)

### Player System (`Content/Player.cs`)
- Core player functionality:
  - Movement and physics (walking, jumping)
  - Combat state management
  - Equipment handling
  - Animation coordination
- Key properties:
  - `IsAttacking`: Combat state
  - `IsMoving`, `IsJumping`: Movement states
  - `IsFacingLeft`: Direction control
  - `WeaponHitbox`, `WeaponHitboxRotation`: Weapon positioning
  - `AttackTimer`, `AttackDirection`: Combat timing/aiming
- Equipment slots:
  - Weapon (primary combat item)
  - Offhand (shields, secondary items)
  - Armor (helmet, chestplate)

### Combat System

#### Weapon Types and Implementations

1. Melee Weapons (Swords)
```csharp
public class Wooden_Sword : Item
{
    protected override void Initialize()
    {
        Type = "[Weapon - Sword]";
        Damage = 5;
        UseTime = 0.8f;
        ShootSpeed = 100f;
        Knockback = 120f;
    }
    
    public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
    {
        DrawSwordAttack(spriteBatch, gameTime, player);
        DrawSwordIdle(spriteBatch, gameTime, player);
    }
}
```

2. Magical Weapons (Staves)
```csharp
public class Emberwood_Staff : Item
{
    protected override void Initialize()
    {
        Type = "[Weapon - Staff]";
        Damage = 10;
        UseTime = 0.8f;
        Knockback = 300f;
        ShootSpeed = 900f;
    }
    
    public override void Update(float deltaTime, GameTime gameTime, Player player)
    {
        // Generate fire particles at staff tip
        if (player.IsAttacking)
        {
            Vector2 weaponTip = CalculateWeaponTip(player);
            particle.NewParticle(
                id: 4,
                position: weaponTip,
                velocity: new Vector2(-5, -70),
                colorFade: 0.1f,
                startColor: new Color(255, 70, 0, 0),
                endColor: new Color(255, 100, 20, 0),
                scale: 0.8f,
                lifeTime: 2f,
                drawLayer: DrawLayer.AbovePlayer
            );
        }
    }
}
```

3. Ranged Weapons (Guns)
```csharp
public class Makeshift_Shotgun : Item
{
    private float smokeTimer = 0f;
    private Vector2 lastMuzzlePosition;

    protected override void Initialize()
    {
        Type = "[Weapon - Rifle]";
        Damage = 8;
        UseTime = 1f;
        ShootSpeed = 1500f;
        Recoil = 0.6f;
    }
    
    public override void Use(float deltaTime, Player player, Vector2 direction)
    {
        // Spawn multiple projectiles with spread
        const int projectileCount = 4;
        float spreadAngle = MathHelper.ToRadians(15f);
        Vector2 spawnPosition = CalculateMuzzlePosition(player);
        
        for (int i = 0; i < projectileCount; i++)
        {
            // Generate spread pattern
            // Create projectiles
            // Apply recoil effects
        }
    }
    
    public override void Update(float deltaTime, GameTime gameTime, Player player)
    {
        // Generate smoke particles
        if (smokeTimer > 0f)
        {
            particle.NewParticle(
                id: 4,
                position: lastMuzzlePosition,
                velocity: new Vector2(0, -30),
                colorFade: 0.5f,
                startColor: Color.WhiteSmoke * 0.5f,
                endColor: Color.DimGray,
                scale: random.NextFloat(0.2f, 0.8f),
                lifeTime: random.NextFloat(2f, 4f),
                drawLayer: DrawLayer.AbovePlayer
            );
        }
    }
}
```

#### Combat Flow
1. Player Input → Attack Initiation
   - Mouse/keyboard input processed
   - Weapon type determines attack pattern

2. Weapon Logic
   - Calculate hitboxes and rotations
   - Apply weapon-specific effects
   - Generate projectiles/particles
   - Update visual state

3. Collision Detection
   - Weapon hitboxes checked against entities
   - Projectile paths calculated
   - Hit detection and damage application

4. Visual Feedback
   - Weapon swing/fire animations
   - Particle effects (fire, smoke, impacts)
   - Screen shake and recoil effects
   - Damage numbers and hit indicators

### Floating Text System (`FloatingText.cs`)

#### Core Classes
```csharp
public class FloatingText
{
    // Text Properties
    public string Text;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Scale;
    
    // Visual Properties
    public Color StartColor;
    public Color EndColor;
    public float Alpha;
    public float Lifetime;
    
    // State
    private float totalLifetime;
    private Vector2 initialPosition;
    public object Owner { get; set; }
}

public class FloatingTextManager
{
    private readonly List<FloatingText> texts;
    private readonly BitmapFont font;
    
    public void Add(string text, Vector2 position, 
                   Color startColor, Color endColor,
                   float lifetime, object owner, bool direct)
    {
        // Smart positioning to avoid overlap
        Vector2 offset = CalculateNonOverlappingOffset();
        texts.Add(new FloatingText(text, position, 
                 startColor, endColor, lifetime, 
                 owner, offset, direct));
    }
}
```

#### Features
1. **Text Animation**
   - Smooth fade in/out (alpha interpolation)
   - Color transition (start to end color)
   - Scale animation for pop effect
   - Velocity-based movement

2. **Smart Positioning**
   - Collision detection between text elements
   - Random offset within max radius
   - Multiple placement attempts
   - Owner-based positioning

3. **Visual Effects**
   - Custom bitmap font rendering
   - Alpha blending
   - Scale animation
   - Color interpolation

4. **Use Cases**
   - Damage numbers
   - Heal amounts
   - Status effects
   - Item pickups

### Weapon Mechanics
- Hitbox System:
  - Weapons use dynamic hitboxes for collision detection
  - Hitbox rotation follows weapon animation
  - Position updates based on player movement/state
- Weapon Types:
  - Melee (Swords):
    - Swing animations with wind-up/follow-through
    - Hitbox follows swing arc
    - Damage on collision
  - Ranged (Guns):
    - Projectile spawning with spread
    - Recoil animation
    - Muzzle effects
  - Magical (Staffs):
    - Projectile effects
    - Continuous particle generation
    - Glow/trail effects

### Particle System
#### Core Components

1. Draw Layers
```csharp
public enum DrawLayer
{
    BelowPlayer = 0,  // Rendered behind the player
    AbovePlayer = 1,  // Rendered in front of the player
    OnArena = 2       // Rendered on the arena surface
}
```

2. Particle Properties
```csharp
public struct Particle
{
    // Core Properties
    public Vector2 Position;
    public Vector2 Velocity;
    public float Scale;
    public float Rotation;
    public float CurrentLifeTime;
    public float TotalLifeTime;
    
    // Visual Properties
    public Color StartColor;
    public Color EndColor;
    public float ColorFade;
    
    // Behavior Flags
    public bool NoGravity;
    public bool HasTrail;
    public int AI;
    
    // Trail System
    private const int MaxTrailLength = 10;
    private Vector2[] trailPositions;
}
```

3. Manager Class
```csharp
public class ParticleManager
{
    private const int MaxParticles = 2000;
    private readonly Particle[] activeParticles;
    private readonly Dictionary<int, Texture2D> particleTextures;
    
    // Performance Optimizations
    private readonly Dictionary<Texture2D, List<Particle>> onArenaDrawTextureGroups;
    private readonly Dictionary<Texture2D, List<Particle>> preDrawTextureGroups;
    private readonly Dictionary<Texture2D, List<Particle>> postDrawTextureGroups;
}
  
- Particle Properties:
  - Basic Attributes: ID, position, velocity, scale, rotation
  - Visual: StartColor, EndColor, ColorFade
  - Behavior: NoGravity, HasTrail, AI type
  - Lifecycle: CurrentLifeTime, TotalLifeTime, IsActive
  - Owner reference for lifetime management

- Advanced Features:
  1. Trail System:
     - Configurable trail length (MaxTrailLength = 10)
     - Position history with circular buffer
     - Alpha fade along trail length
     - Performance-optimized update interval

  2. AI Behaviors:
     - Type 1: Scale fade-out effect
     - Type 2: Shell casing physics (eject + fall)
     - Type 3: Gravity-based falling
     - Type 4: Velocity smoothing to zero

  3. Performance Optimizations:
     - Object pooling (MaxParticles = 2000)
     - Viewport culling with padding
     - Texture-based batching
     - Layer-based sorting

- Draw Layer Management:
  ```csharp
  public void PreDraw()  // BelowPlayer (0.6f depth)
  public void PostDraw() // AbovePlayer (0.4f depth)
  public void OnArenaDraw() // Arena layer
  ```

- Usage Examples:
  1. Weapon Effects:
     ```csharp
     // Muzzle flash
     particle.NewParticle(
         id: 1,
         position: weaponTip,
         velocity: direction * speed,
         colorFade: 0.5f,
         startColor: Color.Yellow,
         endColor: Color.Red,
         scale: 1.0f,
         drawLayer: DrawLayer.AbovePlayer
     );
     ```
  
  2. Trail Effects:
     ```csharp
     // Sword trail
     particle.NewParticle(
         hasTrail: true,
         position: swordTip,
         velocity: swingDirection,
         ai: 4  // Smooth velocity fade
     );
     ```

## Common Patterns

### Projectile Spawn Logic
```csharp
// Calculate spawn position relative to weapon hitbox
Vector2 spawnPosition = player.WeaponHitbox.Center.ToVector2() + new Vector2(
    (float)Math.Cos(player.WeaponHitboxRotation),
    (float)Math.Sin(player.WeaponHitboxRotation)
) * (player.WeaponHitbox.Width * 0.5f * (player.IsFacingLeft ? -1f : 1f));

// Calculate target direction with spread
Vector2 intendedTarget = playerCenter + direction * 1000f;
Vector2 correctedDirection = Vector2.Normalize(intendedTarget - spawnPosition);
float randomAngle = random.NextFloat(-spreadAngle, spreadAngle);
Vector2 spreadDirection = Vector2.Transform(correctedDirection, 
    Matrix.CreateRotationZ(randomAngle));

// Create projectile
projectile.NewProjectile(type, variant, Damage, Knockback, ShootSpeed, lifetime, 
    spawnPosition, spreadDirection);
```

### Visual Effects
```csharp
// Muzzle flash particles
const int muzzleParticles = 5;
float muzzleConeAngle = MathHelper.ToRadians(20f);
float baseAngle = (float)Math.Atan2(direction.Y, direction.X);

for (int i = 0; i < muzzleParticles; i++)
{
    float angle = baseAngle + random.NextFloat(-muzzleConeAngle/2f, muzzleConeAngle/2f);
    Vector2 velocity = new Vector2(
        (float)Math.Cos(angle), 
        (float)Math.Sin(angle)
    ) * random.NextFloat(30f, 70f);

    particle.NewParticle(
        type,
        new Rectangle((int)position.X, (int)position.Y, width, height),
        velocity,
        opacity,
        startColor,
        endColor,
        random.NextFloat(minScale, maxScale),
        random.NextFloat(minLife, maxLife),
        drawLayer
    );
}
```

### Weapon Animation
```csharp
// Calculate attack progress and effects
float attackProgress = 1f - (player.AttackTimer / UseTime);
float scaleEffect = 1f;

if (attackProgress < windUpRatio)
{
    // Wind-up phase
    float windUpProgress = attackProgress / windUpRatio;
    scaleEffect = MathHelper.Lerp(0.85f, 1.2f, windUpProgress);
    swingOffset = -MaxSwingAngle * (1f - (float)Math.Cos(windUpProgress * MathHelper.PiOver2));
}
else
{
    // Follow-through phase
    float swingProgress = (attackProgress - windUpRatio) / (1f - windUpRatio);
    scaleEffect = MathHelper.Lerp(1.2f, 0.85f, swingProgress);
    swingOffset = MaxSwingAngle * (float)Math.Sin(swingProgress * MathHelper.PiOver2);
}

// Apply rotation and scale
float finalAngle = baseAngle + (player.IsFacingLeft ? -swingOffset : swingOffset);
Vector2 itemPosition = basePosition + Vector2.Transform(offset, Matrix.CreateRotationZ(finalAngle));
```

## Development Workflow

### Adding New Items
1. Create item class:
   ```csharp
   public class New_Weapon : Item
   {
       protected override void Initialize()
       {
           ID = <unique_id>;
           Type = "[Weapon - <type>]";
           Name = "New Weapon";
           Damage = 10;
           UseTime = 0.8f;
           ShootSpeed = 500f;
           Knockback = 200f;
       }
   }
   ```

2. Implement core methods:
   ```csharp
   public override void Use(float deltaTime, Player player, Vector2 direction)
   {
       base.Use(deltaTime, player, direction);
       // Weapon-specific attack logic
   }

   public override void Update(float deltaTime, GameTime gameTime, Player player)
   {
       base.Update(deltaTime, gameTime, player);
       // Continuous effects, particles, etc
   }
   ```

3. Add drawing logic:
   ```csharp
   public override void PreDraw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
   {
       base.PreDraw(spriteBatch, gameTime, player);
       DrawWeaponAttack(spriteBatch, gameTime, player);
       DrawWeaponIdle(spriteBatch, gameTime, player);
   }
   ```

4. Register in `ItemProperties.cs`

### Testing New Weapons
1. Check basic mechanics:
   - Attack timing and animations
   - Projectile/hitbox positioning
   - Particle effects
2. Test edge cases:
   - Different player states (moving, jumping)
   - Rapid attacks
   - Extreme angles
3. Validate visual effects:
   - Animation smoothness
   - Particle generation
   - Screen positioning

## Game Systems

### Movement System
```csharp
// Player movement with physics
private void UpdateMovement(float deltaTime)
{
    // Apply movement input
    Vector2 input = Joystick_Movement.Direction;
    Position += input * Speed * deltaTime;
    
    // Handle jumping
    if (IsJumping)
    {
        float jumpProgress = 1f - (jumpTime / JUMP_TIME_VALUE);
        Position.Y -= JUMP_BOUNCE_HEIGHT * (float)Math.Sin(Math.PI * jumpProgress);
    }
    
    // Apply collision resolution
    Rectangle bounds = GetBounds();
    if (arena.IsColliding(bounds))
    {
        // Resolve collision and adjust position
    }
}
```

### Arena System
```csharp
public class Arena
{
    private const int TILE_SIZE = 80;
    private const int ARENA_SIZE = 30;
    private readonly Tile[,] tiles;

    // Collision checking
    public bool IsColliding(Rectangle bounds)
    {
        int leftTile = bounds.Left / TileSize;
        int rightTile = bounds.Right / TileSize;
        int topTile = bounds.Top / TileSize;
        int bottomTile = bounds.Bottom / TileSize;

        for (int x = leftTile; x <= rightTile; x++)
        {
            for (int y = topTile; y <= bottomTile; y++)
            {
                if (!GetTile(x, y).Walkable) return true;
            }
        }
        return false;
    }
}
```

### Camera System
```csharp
public class Camera
{
    private const float MIN_ZOOM = 1f;
    private const float MAX_ZOOM = 3f;
    private Vector2 position;
    private float zoom = 1f;

    // Screen shake effect
    public void ApplyShake(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
    }

    // Transform calculation for rendering
    public Matrix GetTransform()
    {
        return Matrix.CreateTranslation(new Vector3(-Position + shakeOffset, 0)) 
             * Matrix.CreateScale(zoom);
    }
}
```

## Content Pipeline

### Asset Management
1. Textures:
   - Items: `Content/Textures/Items/t_Item_{ID}.png`
   - NPCs: `Content/Textures/NPCs/t_NPC_{ID}.png`
   - Effects: `Content/Textures/Particles/t_Particle_{ID}.png`

2. Content Building:
   ```xml
   <!-- Content.mgcb -->
   #begin Textures/Items/t_Item_0.png
   /importer:TextureImporter
   /processor:TextureProcessor
   /processorParam:ColorKeyColor=255,0,255,255
   /build:Textures/Items/t_Item_0.png
   ```

3. Asset Loading:
   ```csharp
   protected override void LoadContent()
   {
       // Load textures
       itemTextures = new Dictionary<int, Texture2D>();
       foreach (var item in ItemProperties.Items)
       {
           itemTextures[item.ID] = Content.Load<Texture2D>($"Textures/Items/t_Item_{item.ID}");
       }
   }
   ```

## Performance Considerations

### Optimization Techniques
1. Draw Call Batching:
   ```csharp
   protected override void Draw(GameTime gameTime)
   {
       spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, camera.TransformMatrix);
       // Draw world
       arena.Draw(spriteBatch);
       // Draw entities
       entities.ForEach(e => e.Draw(spriteBatch));
       spriteBatch.End();
   }
   ```

2. Particle System Pooling:
   ```csharp
   public class ParticleManager
   {
       private readonly List<Particle> activeParticles;
       private readonly Queue<Particle> particlePool;

       public Particle NewParticle()
       {
           if (particlePool.Count > 0)
               return particlePool.Dequeue();
           return new Particle();
       }
   }
   ```

3. Viewport Culling:
   ```csharp
   public void Draw(SpriteBatch spriteBatch, Camera camera)
   {
       Rectangle visibleArea = camera.GetVisibleArea();
       // Only draw visible tiles
       for (int x = visibleArea.Left; x < visibleArea.Right; x++)
       {
           for (int y = visibleArea.Top; y < visibleArea.Bottom; y++)
           {
               // Draw tile
           }
       }
   }
   ```

## Tips & Best Practices

### Game Development
1. Core Systems:
   - Follow existing patterns for consistency
   - Use proper layer management for rendering
   - Implement pooling for frequently created objects
   - Handle edge cases in collision detection

2. Combat Design:
   - Balance weapon properties carefully
   - Test different combat scenarios
   - Consider player feedback and game feel
   - Optimize particle effects for performance

3. Visual Effects:
   - Use screen shake judiciously
   - Implement proper particle lifecycle
   - Consider frame timing in animations
   - Layer effects for visual impact

### Common Issues
1. Hitbox Problems:
   - Verify hitbox calculations
   - Test edge cases in movement
   - Check collision resolution
   - Validate weapon ranges

2. Visual Bugs:
   - Check draw order
   - Verify texture coordinates
   - Test animation timing
   - Monitor particle systems

3. Performance:
   - Profile draw calls
   - Optimize particle count
   - Implement culling
   - Pool frequently used objects
