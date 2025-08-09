using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proximity.Content
{
    public class Inventory
    {
        #region Constants

        public const int Columns = 5;
        public const int Rows = 20;
        public const int TotalSlots = Columns * Rows;
        public const int VisibleRows = 5;
        public const int VisibleSlots = Columns * VisibleRows;
        private const int SlotSize = 100;
        private const int StatBoxSpriteSize = 45;
        private const int StatIconSize = 30;
        private const int StarSmallIconSize = 33;
        private const int StarSmallGap = 4;
        private const int IconBoxPadding = 55;
        private const int bottomPadding = 40;
        private const int itemIconPadding = 20;
        private const int PortraitSize = 300;
        private const float STATS_ANIMATION_SPEED = 5f;

        #endregion Constants

        #region Core Fields

        private readonly List<Item> items;
        private readonly int screenWidth, screenHeight;
        private readonly int slotWidth, slotHeight;
        private readonly int renderTargetWidth, renderTargetHeight;
        private bool isOpen;

        #endregion Core Fields

        #region Textures

        private readonly Texture2D slotTexture;
        private readonly Texture2D thumbTexture;
        private readonly Texture2D statBoxTexture;
        private readonly Texture2D statBoxOutlineTexture;
        private readonly Texture2D statBoxOutlineTextureReversed;
        private readonly Texture2D statIconsTexture;
        private readonly Texture2D starTexture;

        #endregion Textures

        #region Render Targets

        private RenderTarget2D inventoryRenderTarget;
        private RenderTarget2D infoBoxRenderTarget;
        private RenderTarget2D playerPortraitRenderTarget;

        #endregion Render Targets

        #region Scrolling State

        private float scrollOffset;
        private float lastTouchY;
        private bool isTouching;
        private float infoBoxScrollOffset = 0f;
        private float infoBoxLastTouchY = 0f;
        private bool infoBoxIsTouching = false;

        #endregion Scrolling State

        #region Selection State

        private int? selectedItemSlot = null;
        private int lastSelectedItemSlot = -1;

        #endregion Selection State

        #region Stats Animation

        private bool isStatsVisible = false;
        private float statsBoxOffset = 0f;
        private bool isAnimatingStats = false;

        #endregion Stats Animation

        #region Sorting Animation

        private float[] sortBoxOffsets = new float[5];
        private const float TARGET_OFFSET = 50f;
        private int chosenSortingMethod = 3;
        private int previousSortingMethod = -1;

        #endregion Sorting Animation

        #region Portrait System

        private ParticleManager portraitParticleManager;
        private ContentManager contentManager;
        private bool portraitParticleManagerInitialized = false;
        private static DateTime portraitStartTime = DateTime.Now;

        #endregion Portrait System

        #region Mouse Input

        private int previousWheelValue = 0;

        #endregion Mouse Input

        public bool IsOpen
        {
            get => isOpen;
            set
            {
                isOpen = value;
                if (!isOpen)
                {
                    Item.FreezeGameWorldAnimations = false;
                }
            }
        }

        public Inventory(ContentManager content, int screenWidth, int screenHeight)
        {
            // Initialize core properties
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.contentManager = content;
            slotWidth = SlotSize;
            slotHeight = SlotSize;
            renderTargetWidth = slotWidth * Columns;
            renderTargetHeight = slotHeight * (VisibleRows + 2);

            // Initialize items array
            items = new List<Item>(TotalSlots);
            for (int i = 0; i < TotalSlots; i++)
                items.Add(null);

            // Load required texture
            slotTexture = content.Load<Texture2D>("Textures/UI/t_Inventory");

            // Load optional textures with fallbacks
            thumbTexture = LoadTextureWithFallback(content, "Textures/UI/t_Inventory_Thumb", slotTexture);
            statBoxTexture = LoadTextureWithFallback(content, "Textures/UI/t_Inventory_Box", null);
            statBoxOutlineTexture = LoadTextureWithFallback(content, "Textures/UI/t_Inventory_Box_Outline", null);
            statBoxOutlineTextureReversed = LoadTextureWithFallback(content, "Textures/UI/t_Inventory_Box_Outline_Reversed", null);
            statIconsTexture = LoadTextureWithFallback(content, "Textures/UI/t_Inventory_Icons", null);
            starTexture = LoadTextureWithFallback(content, "Textures/UI/t_Inventory_StarSmall", null);

            // Initialize portrait system
            portraitParticleManager = new ParticleManager(content);
        }

        private Texture2D LoadTextureWithFallback(ContentManager content, string path, Texture2D fallback)
        {
            try
            {
                return content.Load<Texture2D>(path);
            }
            catch
            {
                return fallback;
            }
        }

        #region Render Target Management

        private void EnsureRenderTarget(GraphicsDevice graphicsDevice)
        {
            if (IsRenderTargetInvalid(inventoryRenderTarget, renderTargetWidth, renderTargetHeight))
            {
                inventoryRenderTarget?.Dispose();
                inventoryRenderTarget = CreateRenderTarget(graphicsDevice, renderTargetWidth, renderTargetHeight);
            }
        }

        private void EnsurePlayerPortraitRenderTarget(GraphicsDevice graphicsDevice)
        {
            int portraitHeight = PortraitSize * 2;
            if (IsRenderTargetInvalid(playerPortraitRenderTarget, PortraitSize, portraitHeight))
            {
                playerPortraitRenderTarget?.Dispose();
                playerPortraitRenderTarget = CreateRenderTarget(graphicsDevice, PortraitSize, portraitHeight);
            }

            InitializePortraitParticleManager(graphicsDevice);
        }

        private void EnsureInfoBoxRenderTarget(GraphicsDevice graphicsDevice, int width, int height)
        {
            if (IsRenderTargetInvalid(infoBoxRenderTarget, width, height))
            {
                infoBoxRenderTarget?.Dispose();
                infoBoxRenderTarget = CreateRenderTarget(graphicsDevice, width, height);
            }
        }

        private bool IsRenderTargetInvalid(RenderTarget2D target, int width, int height)
        {
            return target == null || target.Width != width || target.Height != height;
        }

        private RenderTarget2D CreateRenderTarget(GraphicsDevice graphicsDevice, int width, int height)
        {
            return new RenderTarget2D(
                graphicsDevice,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None
            );
        }

        private void InitializePortraitParticleManager(GraphicsDevice graphicsDevice)
        {
            if (portraitParticleManager != null && !portraitParticleManagerInitialized)
            {
                try
                {
                    portraitParticleManager.LoadContent(graphicsDevice, contentManager);
                    portraitParticleManagerInitialized = true;
                }
                catch
                {
                    // Content loading failed - continue without particles
                }
            }
        }

        #endregion Render Target Management

        #region Scrolling

        public void Scroll(float deltaRows)
        {
            float maxOffsetRows = Math.Max(0, (TotalSlots - 1) / Columns - VisibleRows + 1);
            scrollOffset = MathHelper.Clamp(scrollOffset + deltaRows, 0, maxOffsetRows);
        }

        #endregion Scrolling

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            Item.FreezeGameWorldAnimations = IsOpen;

            if (!IsOpen) return;
            EnsureRenderTarget(spriteBatch.GraphicsDevice);

            var font = Main.Font;

            int baseRow = (int)scrollOffset;
            float fraction = scrollOffset - baseRow;

            int lastValidRow = (TotalSlots - 1) / Columns;
            int lastValidCol = (TotalSlots - 1) % Columns;

            var graphicsDevice = spriteBatch.GraphicsDevice;
            var prevRenderTargets = graphicsDevice.GetRenderTargets();
            graphicsDevice.SetRenderTarget(inventoryRenderTarget);
            graphicsDevice.Clear(Color.Transparent);
            using (SpriteBatch rtBatch = new SpriteBatch(graphicsDevice))
            {
                rtBatch.Begin();
                for (int row = 0; row <= VisibleRows + 1; row++)
                {
                    int currentRow = baseRow + row;
                    if (currentRow > lastValidRow) continue;
                    int y = row * slotHeight; // No gap
                    int maxCol = (currentRow == lastValidRow) ? (lastValidCol + 1) : Columns;
                    for (int col = 0; col < maxCol; col++)
                    {
                        int slotIndex = currentRow * Columns + col;
                        if (slotIndex >= TotalSlots) break;
                        int x = col * slotWidth; // No gap
                        Rectangle destRect = new Rectangle(x, y, slotWidth, slotHeight);
                        Item item = items[slotIndex];
                        Color slotColor = Color.White;
                        int rarity = item != null ? item.Rarity : 0;
                        if (rarity < 0 || rarity > 6) rarity = 0;
                        Rectangle srcRect = new Rectangle(0, 0, SlotSize, SlotSize);
                        rtBatch.Draw(slotTexture, destRect, srcRect, slotColor);
                        if (item != null && item.Texture != null)
                        {
                            int itemAreaSize = 100;
                            int itemAreaOffset = (slotWidth - itemAreaSize) / 2;
                            var tex = item.Texture;
                            int srcW = tex.Width;
                            int srcH = tex.Height;
                            string type = item.Type ?? "";
                            bool isReducedScale = type.Contains("[Chestplate]") || type.Contains("[Helmet]") || type.Contains("[Offhand]") || type.Contains("Gun");
                            Rectangle srcRectSwordStaff = new Rectangle(0, 0, srcW, srcH);
                            float baseScale = Math.Min((float)itemAreaSize / srcW, (float)itemAreaSize / srcH) * 0.8f;
                            float scale = isReducedScale ? baseScale * 0.8f : baseScale;
                            int drawWidth = (int)(srcW * scale);
                            int drawHeight = (int)(srcH * scale);
                            int drawX = x + itemAreaOffset + (itemAreaSize - drawWidth) / 2;
                            int drawXShadow = x + itemAreaOffset + (int)(itemAreaSize - drawWidth * 1.1f) / 2;
                            int drawY = y + itemAreaOffset + (itemAreaSize - drawHeight) / 2;
                            Rectangle drawRect = new Rectangle(drawX, drawY, drawWidth, drawHeight);
                            Rectangle drawRectShadow = new Rectangle(drawXShadow, drawY + 10, (int)(drawWidth * 1.1f), (int)(drawHeight * 1.1f));
                            rtBatch.Draw(tex, drawRectShadow, srcRectSwordStaff, Color.Black * 0.5f);
                            rtBatch.Draw(tex, drawRect, srcRectSwordStaff, Color.White);
                        }
                    }
                }
                rtBatch.End();
            }
            if (prevRenderTargets != null && prevRenderTargets.Length > 0 && prevRenderTargets[0].RenderTarget != null)
                graphicsDevice.SetRenderTarget(prevRenderTargets[0].RenderTarget as RenderTarget2D);
            else
                graphicsDevice.SetRenderTarget(null);

            //DRAW SORT ICONS

            int startY = (screenHeight - (slotHeight * VisibleRows)) / 2;
            int startX = (screenWidth - (slotWidth * Columns)) / 2;
            Rectangle destRectFull = new Rectangle(startX, startY, renderTargetWidth, slotHeight * VisibleRows);
            Rectangle srcRectFull = new Rectangle(0, (int)(fraction * slotHeight), renderTargetWidth, slotHeight * VisibleRows);
            spriteBatch.Draw(inventoryRenderTarget, destRectFull, srcRectFull, Color.White);

            int scrollbarX = destRectFull.Right;
            int scrollbarY = destRectFull.Y;
            int scrollbarHeight = destRectFull.Height;
            int totalRows = (TotalSlots + Columns - 1) / Columns;
            float visibleRatio = (float)VisibleRows / totalRows;
            float thumbHeight = scrollbarHeight * visibleRatio;
            float maxOffsetRows = totalRows - VisibleRows;
            if (maxOffsetRows < 1) maxOffsetRows = 1;
            float thumbY = scrollbarY + (scrollOffset / maxOffsetRows) * (scrollbarHeight - thumbHeight);
            int thumbTexWidth = 15;
            int thumbOffset = 0;
            int topHeight = 20;
            int midHeight = 5;
            int botHeight = 20;
            Rectangle thumbRect = new Rectangle(scrollbarX + thumbOffset, (int)thumbY, thumbTexWidth, (int)thumbHeight);
            Rectangle srcTop = new Rectangle(0, 0, thumbTexWidth, topHeight);
            Rectangle srcMid = new Rectangle(0, topHeight, thumbTexWidth, midHeight);
            Rectangle srcBot = new Rectangle(0, topHeight + midHeight, thumbTexWidth, botHeight);
            spriteBatch.Draw(Main.Pixel, new Rectangle(scrollbarX + thumbOffset, scrollbarY, thumbTexWidth, scrollbarHeight), Color.Black * 0.45f);
            if (thumbRect.Height >= topHeight + botHeight)
            {
                Rectangle destTop = new Rectangle(thumbRect.X, thumbRect.Y, thumbTexWidth, topHeight);
                Rectangle destMid = new Rectangle(thumbRect.X, thumbRect.Y + topHeight, thumbTexWidth, thumbRect.Height - topHeight - botHeight);
                Rectangle destBot = new Rectangle(thumbRect.X, thumbRect.Y + thumbRect.Height - botHeight, thumbTexWidth, botHeight);
                spriteBatch.Draw(thumbTexture, destTop, srcTop, Color.White);
                spriteBatch.Draw(thumbTexture, destMid, srcMid, Color.White);
                spriteBatch.Draw(thumbTexture, destBot, srcBot, Color.White);
            }
            else if (thumbRect.Height > topHeight)
            {
                Rectangle destTop = new Rectangle(thumbRect.X, thumbRect.Y, thumbTexWidth, topHeight);
                int remaining = thumbRect.Height - topHeight;
                Rectangle destBotSmall = new Rectangle(thumbRect.X, thumbRect.Y + topHeight, thumbTexWidth, remaining);
                spriteBatch.Draw(thumbTexture, destTop, srcTop, Color.White);
                spriteBatch.Draw(thumbTexture, destBotSmall, srcBot, Color.White);
            }
            else
            {
                Rectangle destTopSmall = new Rectangle(thumbRect.X, thumbRect.Y, thumbTexWidth, thumbRect.Height);
                spriteBatch.Draw(thumbTexture, destTopSmall, srcTop, Color.White);
            }

            // Draw item stats if selected
            if (selectedItemSlot.HasValue)
            {
                int slotIdx = selectedItemSlot.Value;
                Item selected = null;
                if (slotIdx >= 0 && slotIdx < items.Count)
                {
                    selected = items[slotIdx];
                }
                else if (slotIdx == -100)
                {
                    player.EquippedItems.TryGetValue(EquipmentSlot.Weapon, out selected);
                }
                else if (slotIdx == -101)
                {
                    player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out selected);
                }
                else if (slotIdx == -102)
                {
                    player.EquippedItems.TryGetValue(EquipmentSlot.Offhand, out selected);
                }
                else if (slotIdx == -103)
                {
                    player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out selected);
                }
                if (selected != null)
                {
                    int statBoxStartX = (screenWidth - (slotWidth * Columns)) / 2;
                    int statBoxScrollbarWidth = 15;
                    int statBoxScrollbarX = statBoxStartX + slotWidth * Columns;
                    int infoRegionX = statBoxScrollbarX + statBoxScrollbarWidth;
                    int infoRegionY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                    int infoRegionHeight = slotHeight * VisibleRows;
                    int iconBoxPadding = IconBoxPadding;
                    int iconBoxWidth = 0, iconBoxHeight = 0;
                    int iconDrawWidth = 0, iconDrawHeight = 0;
                    float iconScale = 0.8f;
                    if (selected.Texture != null)
                    {
                        int texW = selected.Texture.Width;
                        int texH = selected.Texture.Height;
                        float baseScale = iconScale;
                        double time = DateTime.Now.TimeOfDay.TotalSeconds;
                        float pulse = (float)Math.Sin(time * 2.2f) * 0.07f + 1.0f;
                        float scale = baseScale * pulse;
                        iconDrawWidth = (int)(texW * scale);
                        iconDrawHeight = (int)(texH);
                        iconBoxWidth = iconDrawWidth + iconBoxPadding * 2;
                        iconBoxHeight = iconDrawHeight + iconBoxPadding * 2;
                    }
                    int infoBoxPadding = 20;
                    int marginX = 40;
                    int maxInfoBoxWidth = screenWidth - marginX * 2;
                    // --- Improved word-wrapping and alignment logic ---
                    // Helper: Wrap text to fit max width, splitting long words if needed
                    List<string> WrapText(string text, int maxWidth)
                    {
                        var lines = new List<string>();
                        while (!string.IsNullOrEmpty(text))
                        {
                            int cut = text.Length;
                            string testLine = text.Substring(0, cut);
                            Vector2 testSize = font.MeasureString(testLine);
                            while (testSize.X > maxWidth && cut > 0)
                            {
                                int lastSpace = text.LastIndexOf(' ', cut - 1);
                                if (lastSpace > 0)
                                    cut = lastSpace;
                                else
                                {
                                    // No space: forcibly break long word
                                    int wordCut = cut;
                                    while (font.MeasureString(text.Substring(0, wordCut)).X > maxWidth && wordCut > 1)
                                        wordCut--;
                                    cut = wordCut;
                                    break;
                                }
                                testLine = text.Substring(0, cut);
                                testSize = font.MeasureString(testLine);
                            }
                            if (cut == 0) cut = 1;
                            lines.Add(text.Substring(0, cut));
                            text = text.Substring(cut).TrimStart();
                        }
                        return lines;
                    }

                    // Gather all content and wrap
                    var rarityInfo = Item.GetRarityInfo(selected.Rarity);
                    string rarityText = rarityInfo.Name;
                    string typeText = selected.Type ?? "";
                    List<string> statDisplay = new List<string>();
                    if (selected.Damage != 0) statDisplay.Add($"{selected.Damage} damage");
                    if (selected.Knockback != 0) statDisplay.Add($"{(selected.Knockback / 100f):.00} knockback");
                    if ((selected.Type.Contains("Sword") ? selected.SwingRange != 0 : selected.ShootSpeed != 0)) statDisplay.Add($"{(selected.Type.Contains("Sword") ? selected.SwingRange : selected.ShootSpeed)} " + (selected.Type.Contains("Sword") ? "degrees" : "meters"));
                    if (selected.UseTime != 0) statDisplay.Add($"{(1f / selected.UseTime).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} uses per second");
                    if (selected.Defense != 0) statDisplay.Add($"{selected.Defense} defense");
                    if (selected.KnockbackResistance != 0) statDisplay.Add($"{selected.KnockbackResistance * 100f}% knockback resistance");
                    if (selected.Value != 0) statDisplay.Add($"{selected.Value} gold");
                    List<(string name, string effect, bool isPrefix)> affixes = new();
                    if (!string.IsNullOrEmpty(selected.Prefix) && ItemModifier.Prefixes.Values.FirstOrDefault(p => p.Name == selected.Prefix) is Prefix prefixObj)
                        affixes.Add((prefixObj.Name, prefixObj.Effect, true));
                    if (!string.IsNullOrEmpty(selected.Suffix) && ItemModifier.Suffixes.Values.FirstOrDefault(s => s.Name == selected.Suffix) is Suffix suffixObj)
                        affixes.Add((suffixObj.Name, suffixObj.Effect, false));
                    string debugLine = null;
                    if (Main.DebugMode)
                        debugLine = $"#DEBUG-ID: {selected.ID}, #DEBUG-PREFIX: {selected.Prefix}, #DEBUG-SUFFIX: {selected.Suffix}";

                    // Find longest line for provisional width
                    int minInfoBoxWidth = 570; // Increased minimum width for better readability
                    int provisionalWidth = Math.Max(minInfoBoxWidth, Math.Min(maxInfoBoxWidth, Math.Max(iconBoxWidth, minInfoBoxWidth)));
                    int maxTextWidth = 0;
                    List<string> allWrappedLines = new List<string>();
                    void AddWrapped(string text)
                    {
                        foreach (var line in WrapText(text, provisionalWidth - infoBoxPadding * 2))
                        {
                            allWrappedLines.Add(line);
                            int w = (int)font.MeasureString(line).X;
                            if (w > maxTextWidth) maxTextWidth = w;
                        }
                    }
                    if (!string.IsNullOrEmpty(selected.GetName())) AddWrapped(selected.GetName());
                    if (!string.IsNullOrEmpty(rarityText)) AddWrapped(rarityText);
                    if (!string.IsNullOrEmpty(typeText)) AddWrapped(typeText);
                    if (!string.IsNullOrEmpty(selected.Lore)) AddWrapped(selected.Lore);
                    foreach (var line in statDisplay) AddWrapped(line);
                    foreach (var (affixName, effect, isPrefix) in affixes) { AddWrapped(affixName); AddWrapped(effect); }
                    if (Main.DebugMode && debugLine != null) AddWrapped(debugLine);

                    int maxInfoBoxWidthNoOverlap = screenWidth - infoRegionX - 40;
                    int infoBoxWidth = Math.Max(minInfoBoxWidth, Math.Min(Math.Max(iconBoxWidth, maxTextWidth + infoBoxPadding * 2), Math.Min(maxInfoBoxWidth, maxInfoBoxWidthNoOverlap)));

                    // --- Second pass: wrap all content using final infoBoxWidth ---
                    List<string> nameLines = !string.IsNullOrEmpty(selected.GetName()) ? WrapText(selected.GetName(), infoBoxWidth - infoBoxPadding * 2) : new List<string>();
                    // Rarity text removed from info box content
                    List<string> typeLines = !string.IsNullOrEmpty(typeText) ? WrapText(typeText, infoBoxWidth - infoBoxPadding * 2) : new List<string>();
                    List<string> loreLines = !string.IsNullOrEmpty(selected.Lore) ? WrapText(selected.Lore, infoBoxWidth - infoBoxPadding * 2) : new List<string>();
                    List<List<string>> statLines = statDisplay.Select(s => WrapText(s, infoBoxWidth - infoBoxPadding * 2)).ToList();
                    List<(List<string> name, List<string> effect, bool isPrefix)> affixLines = affixes.Select(a => (WrapText(a.name, infoBoxWidth - infoBoxPadding * 2), WrapText(a.effect, infoBoxWidth - infoBoxPadding * 2), a.isPrefix)).ToList();
                    List<string> debugLines = (Main.DebugMode && debugLine != null) ? WrapText(debugLine, infoBoxWidth - infoBoxPadding * 2) : new List<string>();

                    // --- Icon box is fixed, info content is scrollable ---
                    // Dynamically set info box height based on screen size
                    int totalInfoBoxHeight = Math.Min(infoRegionHeight, screenHeight - infoRegionY);
                    int infoContentBoxHeight = totalInfoBoxHeight - iconBoxHeight;
                    if (infoContentBoxHeight < 40) infoContentBoxHeight = 40;

                    // Calculate actual content height by simulating the rendering
                    int actualContentHeight = infoBoxPadding; // Top padding

                    // 1. 10px gap then item name
                    actualContentHeight += 10;
                    foreach (var nameLine in nameLines)
                        actualContentHeight += (int)font.MeasureString(nameLine).Y;

                    // 2. 10px gap then item type
                    actualContentHeight += 10;
                    foreach (var typeLine in typeLines)
                        actualContentHeight += (int)font.MeasureString(typeLine).Y;

                    // 3. 30px gap then lore
                    actualContentHeight += 30;
                    foreach (var loreLine in loreLines)
                        actualContentHeight += (int)font.MeasureString(loreLine).Y;

                    // 4. 30px gap then stats (10px between each stat)
                    actualContentHeight += 30;
                    for (int i = 0; i < statLines.Count; i++)
                    {
                        if (i > 0) actualContentHeight += 10; // 10px between stats
                        foreach (var statLine in statLines[i])
                            actualContentHeight += (int)font.MeasureString(statLine).Y;
                    }

                    // Item info (separate stat with 10px gap)
                    if (!string.IsNullOrEmpty(selected.Info))
                    {
                        actualContentHeight += 10; // 10px gap before item info
                        var infoLines = WrapText(selected.Info, infoBoxWidth - infoBoxPadding * 2);
                        foreach (var infoLine in infoLines)
                            actualContentHeight += (int)font.MeasureString(infoLine).Y;
                    }

                    // 5 & 6. 10px gap for each prefix/suffix
                    foreach (var affix in affixLines)
                    {
                        actualContentHeight += 10; // 10px gap
                        string affixLabel = affix.isPrefix ? "[Prefix Bonus]" : "[Suffix Bonus]";
                        actualContentHeight += (int)font.MeasureString(affixLabel).Y;
                        foreach (var line in affix.effect)
                            actualContentHeight += (int)font.MeasureString(line).Y;
                    }

                    // 7. 10px gap then debug info
                    if (debugLines.Count > 0)
                    {
                        actualContentHeight += 10;
                        foreach (var dbgLine in debugLines)
                            actualContentHeight += (int)font.MeasureString(dbgLine).Y;
                    }

                    // 8. Fixed bottom padding for consistent spacing
                    actualContentHeight += bottomPadding; // Fixed 25px bottom padding

                    // Debug output
                    bool isEquipped = slotIdx < 0;
                    //System.Diagnostics.Debug.WriteLine($"Item: {selected.GetName()}, IsEquipped: {isEquipped}, ContentHeight: {actualContentHeight}, VisibleHeight: {visibleHeight}");

                    // Create the properly sized render target
                    EnsureInfoBoxRenderTarget(spriteBatch.GraphicsDevice, infoBoxWidth, actualContentHeight);

                    // Render the actual content
                    spriteBatch.GraphicsDevice.SetRenderTarget(infoBoxRenderTarget);
                    spriteBatch.GraphicsDevice.Clear(Color.Transparent);
                    using (SpriteBatch infoBatch = new SpriteBatch(spriteBatch.GraphicsDevice))
                    {
                        infoBatch.Begin();
                        int infoY = infoBoxPadding;

                        // 1. 10px gap then item name (centered)
                        infoY += 10;
                        foreach (var nameLine in nameLines)
                        {
                            Vector2 nameSize = font.MeasureString(nameLine);
                            int nameX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)nameSize.X) / 2;
                            font.DrawString(infoBatch, nameLine, new Vector2(nameX, infoY), rarityInfo.Color);
                            infoY += (int)nameSize.Y;
                        }

                        // 2. 10px gap then item type (centered)
                        infoY += 10;
                        foreach (var typeLine in typeLines)
                        {
                            Vector2 typeSize = font.MeasureString(typeLine);
                            int typeX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)typeSize.X) / 2;
                            font.DrawString(infoBatch, typeLine, new Vector2(typeX, infoY), Color.Yellow);
                            infoY += (int)typeSize.Y;
                        }

                        // 3. 30px gap then lore (centered)
                        infoY += 30;
                        foreach (var loreLine in loreLines)
                        {
                            Vector2 loreSize = font.MeasureString(loreLine);
                            int loreX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)loreSize.X) / 2;
                            font.DrawString(infoBatch, loreLine, new Vector2(loreX, infoY), Color.MediumAquamarine);
                            infoY += (int)loreSize.Y;
                        }

                        // 4. 30px gap then stats (10px between each stat)
                        infoY += 30;
                        for (int i = 0; i < statLines.Count; i++)
                        {
                            if (i > 0) infoY += 10; // 10px between stats

                            int iconIndex = -1;
                            string statRaw = statDisplay.Count > i ? statDisplay[i] : "";
                            if (statRaw.Contains("damage")) iconIndex = 2;
                            else if (statRaw.Contains("knockback")) iconIndex = 3;
                            else if (statRaw.Contains("degrees")) iconIndex = 6;
                            else if (statRaw.Contains("meters")) iconIndex = 6;
                            else if (statRaw.Contains("uses per second")) iconIndex = 7;
                            else if (statRaw.Contains("defense")) iconIndex = 4;
                            else if (statRaw.Contains("knockback resistance")) iconIndex = 5;
                            else if (statRaw.Contains("gold")) iconIndex = 8;

                            foreach (var statLine in statLines[i])
                            {
                                if (statIconsTexture != null && iconIndex >= 0)
                                {
                                    Rectangle statIconSrcRect = new Rectangle(0, iconIndex * StatIconSize, StatIconSize, StatIconSize);
                                    Vector2 iconPos = new Vector2(infoBoxPadding, infoY + 2);
                                    infoBatch.Draw(statIconsTexture, iconPos, statIconSrcRect, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                                }
                                float textOffsetX = statIconsTexture != null && iconIndex >= 0 ? StatIconSize + 8 : 0;
                                Color statColor = GetStatComparisonColor(statRaw, selected, player, slotIdx);
                                font.DrawString(infoBatch, statLine, new Vector2(infoBoxPadding + textOffsetX, infoY + 5), statColor);
                                infoY += (int)font.MeasureString(statLine).Y;
                            }
                        }

                        // Item info (separate stat with 10px gap)
                        if (!string.IsNullOrEmpty(selected.Info))
                        {
                            infoY += 10; // 10px gap before item info
                            var infoLines = WrapText(selected.Info, infoBoxWidth - infoBoxPadding * 2);
                            int infoIconIndex = 9;
                            for (int i = 0; i < infoLines.Count; i++)
                            {
                                float textOffsetX = 0;
                                if (i == 0 && statIconsTexture != null)
                                {
                                    Rectangle infoIconSrcRect = new Rectangle(0, infoIconIndex * StatIconSize, StatIconSize, StatIconSize);
                                    Vector2 iconPos = new Vector2(infoBoxPadding, infoY + 2);
                                    infoBatch.Draw(statIconsTexture, iconPos, infoIconSrcRect, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                                    textOffsetX = StatIconSize + 8;
                                }
                                font.DrawString(infoBatch, infoLines[i], new Vector2(infoBoxPadding + textOffsetX, infoY + 5), Color.White);
                                infoY += (int)font.MeasureString(infoLines[i]).Y;
                            }
                        }
                        // Prefix/suffix with 10px gaps
                        foreach (var affix in affixLines)
                        {
                            infoY += 10;
                            string affixLabel = affix.isPrefix ? "[Prefix Bonus]" : "[Suffix Bonus]";
                            font.DrawString(infoBatch, affixLabel, new Vector2(infoBoxPadding, infoY), Color.Yellow);
                            infoY += (int)font.MeasureString(affixLabel).Y;
                            foreach (var line in affix.effect)
                            {
                                font.DrawString(infoBatch, line, new Vector2(infoBoxPadding, infoY), Color.CornflowerBlue);
                                infoY += (int)font.MeasureString(line).Y;
                            }
                        }

                        // 7. 10px gap then debug info
                        if (debugLines.Count > 0)
                        {
                            infoY += 10;
                            foreach (var dbgLine in debugLines)
                            {
                                font.DrawString(infoBatch, dbgLine, new Vector2(infoBoxPadding, infoY), Color.Red);
                                infoY += (int)font.MeasureString(dbgLine).Y;
                            }
                        }

                        // 8. Consistent bottom spacing
                        // (Bottom padding handled in height calculation)
                        infoBatch.End();
                    }
                    spriteBatch.GraphicsDevice.SetRenderTarget(null);
                    // --- Draw icon box (fixed at top of info region) ---
                    if (statBoxTexture != null && selected.Texture != null)
                    {
                        // Make icon box width match info box width
                        DrawNineSliceBox(spriteBatch, statBoxTexture, new Rectangle(infoRegionX, infoRegionY, infoBoxWidth, iconBoxHeight + IconBoxPadding), StatBoxSpriteSize, rarityInfo.Color);

                        // Draw rarity text 10px below the icon box top, centered
                        if (!string.IsNullOrEmpty(rarityText))
                        {
                            Vector2 raritySize = font.MeasureString(rarityText);
                            int rarityX = infoRegionX + ((infoBoxWidth - (int)raritySize.X) / 2);
                            int rarityY = infoRegionY + 10;
                            font.DrawString(spriteBatch, rarityText, new Vector2(rarityX, rarityY), rarityInfo.Color);
                            if (starTexture != null)
                            {
                                int starCount = selected.Rarity;
                                int totalStarsWidth = starCount * StarSmallIconSize + (starCount - 1) * StarSmallGap;
                                int starsStartX = infoRegionX + (infoBoxWidth - totalStarsWidth) / 2;
                                int starsY = rarityY + (int)raritySize.Y + 4; // 4px below rarity text
                                for (int i = 0; i < starCount; i++)
                                {
                                    int starX = starsStartX + i * (StarSmallIconSize + StarSmallGap);
                                    spriteBatch.Draw(starTexture, new Rectangle(starX, starsY, StarSmallIconSize, StarSmallIconSize), Color.White);
                                }
                            }
                        }
                    }
                    if (selected.Texture != null)
                    {
                        double time = DateTime.Now.TimeOfDay.TotalSeconds;
                        float pulse = (float)Math.Sin(time * 2.2f) * 0.07f + 1.0f;
                        float scale = iconScale * pulse;
                        float angle = (float)Math.Sin(time * 1.7f) * MathHelper.ToRadians(12f);
                        int texW = selected.Texture.Width;
                        int texH = selected.Texture.Height;
                        int drawWidth = (int)(texW * scale);
                        int drawHeight = (int)(texH * scale);
                        int iconX = infoRegionX + (infoBoxWidth - drawWidth) / 2;
                        int iconY = infoRegionY + (iconBoxHeight - drawHeight) / 2 + IconBoxPadding - itemIconPadding;
                        Vector2 origin = new Vector2(texW / 2f, texH / 2f);
                        Vector2 drawPos = new Vector2(iconX + drawWidth / 2f, iconY + drawHeight / 2f);
                        spriteBatch.Draw(selected.Texture, drawPos + new Vector2(0, 15), null, Color.Black * 0.5f, angle, origin, scale * 1.1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(selected.Texture, drawPos, null, Color.White, angle, origin, scale, SpriteEffects.None, 0f);
                    }
                    // --- Draw info content (scrollable) ---
                    // Clamp info box X so it never exceeds screen borders
                    int infoBoxScreenX = Math.Max(marginX, Math.Min(screenWidth - infoBoxWidth - marginX, infoRegionX));
                    int infoContentX = infoBoxScreenX;
                    int infoContentY = infoRegionY + iconBoxHeight;
                    // Cap info content box height to fit on screen (Y axis untouched)
                    int cappedInfoContentBoxHeight = infoContentBoxHeight;
                    int visibleHeight = cappedInfoContentBoxHeight;
                    int actualScrollableHeight = visibleHeight - IconBoxPadding;

                    Rectangle infoContentDestRect = new Rectangle(infoContentX, infoContentY, infoBoxWidth, visibleHeight);
                    Rectangle infoContentSrcRect = new Rectangle(0, (int)infoBoxScrollOffset, infoBoxWidth, visibleHeight);
                    if (statBoxTexture != null)
                    {
                        DrawNineSliceBox(spriteBatch, statBoxTexture, infoContentDestRect, StatBoxSpriteSize, Color.White);
                    }
                    spriteBatch.Draw(infoBoxRenderTarget, infoContentDestRect, infoContentSrcRect, Color.White);
                    // --- Draw scrollbar for info content if needed ---
                    if (infoBoxRenderTarget.Height > actualScrollableHeight)
                    {
                        int infoScrollbarWidth = 15;
                        int infoScrollbarX = infoContentDestRect.Right;
                        int infoScrollbarY = infoContentDestRect.Y;
                        int infoScrollbarHeight = infoContentDestRect.Height;
                        float infoVisibleRatio = (float)visibleHeight / infoBoxRenderTarget.Height;
                        float infoThumbHeight = infoScrollbarHeight * infoVisibleRatio;
                        float infoMaxScroll = Math.Max(0, infoBoxRenderTarget.Height - infoScrollbarHeight);
                        if (infoMaxScroll < 1) infoMaxScroll = 1;
                        float infoThumbY = infoScrollbarY + (infoBoxScrollOffset / infoMaxScroll) * (infoScrollbarHeight - infoThumbHeight);
                        Rectangle infoThumbRect = new Rectangle(infoScrollbarX, (int)infoThumbY, infoScrollbarWidth, (int)infoThumbHeight);
                        // Draw scrollbar background
                        spriteBatch.Draw(Main.Pixel, new Rectangle(infoScrollbarX, infoScrollbarY, infoScrollbarWidth, infoScrollbarHeight), Color.Black * 0.45f);
                        // Use unique names for 9-slice thumb variables to avoid shadowing
                        int infoThumbTopHeight = 20;
                        int infoThumbMidHeight = 5;
                        int infoThumbBotHeight = 20;
                        Rectangle infoThumbSrcTop = new Rectangle(0, 0, infoScrollbarWidth, infoThumbTopHeight);
                        Rectangle infoThumbSrcMid = new Rectangle(0, infoThumbTopHeight, infoScrollbarWidth, infoThumbMidHeight);
                        Rectangle infoThumbSrcBot = new Rectangle(0, infoThumbTopHeight + infoThumbMidHeight, infoScrollbarWidth, infoThumbBotHeight);
                        if (infoThumbRect.Height >= infoThumbTopHeight + infoThumbBotHeight)
                        {
                            Rectangle destTop = new Rectangle(infoThumbRect.X, infoThumbRect.Y, infoScrollbarWidth, infoThumbTopHeight);
                            Rectangle destMid = new Rectangle(infoThumbRect.X, infoThumbRect.Y + infoThumbTopHeight, infoScrollbarWidth, infoThumbRect.Height - infoThumbTopHeight - infoThumbBotHeight);
                            Rectangle destBot = new Rectangle(infoThumbRect.X, infoThumbRect.Y + infoThumbRect.Height - infoThumbBotHeight, infoScrollbarWidth, infoThumbBotHeight);
                            spriteBatch.Draw(thumbTexture, destTop, infoThumbSrcTop, Color.White);
                            spriteBatch.Draw(thumbTexture, destMid, infoThumbSrcMid, Color.White);
                            spriteBatch.Draw(thumbTexture, destBot, infoThumbSrcBot, Color.White);
                        }
                        else if (infoThumbRect.Height > infoThumbTopHeight)
                        {
                            Rectangle destTop = new Rectangle(infoThumbRect.X, infoThumbRect.Y, infoScrollbarWidth, infoThumbTopHeight);
                            int remaining = infoThumbRect.Height - infoThumbTopHeight;
                            Rectangle destBotSmall = new Rectangle(infoThumbRect.X, infoThumbRect.Y + infoThumbTopHeight, infoScrollbarWidth, remaining);
                            spriteBatch.Draw(thumbTexture, destTop, infoThumbSrcTop, Color.White);
                            spriteBatch.Draw(thumbTexture, destBotSmall, infoThumbSrcBot, Color.White);
                        }
                        else
                        {
                            Rectangle destTopSmall = new Rectangle(infoThumbRect.X, infoThumbRect.Y, infoScrollbarWidth, infoThumbRect.Height);
                            spriteBatch.Draw(thumbTexture, destTopSmall, infoThumbSrcTop, Color.White);
                        }
                    }

                    DrawNineSliceBox(spriteBatch, statBoxOutlineTexture, infoContentDestRect, StatBoxSpriteSize, Color.White);

                    // Draw equip/unequip button above info box
                    bool isEquippedItem = slotIdx < 0;
                    string buttonText = "Equip";
                    Color textColor = Color.White;

                    if (isEquippedItem)
                    {
                        // Check if inventory is full
                        bool inventoryFull = true;
                        for (int i = 0; i < items.Count; i++)
                        {
                            if (items[i] == null)
                            {
                                inventoryFull = false;
                                break;
                            }
                        }

                        if (inventoryFull)
                        {
                            buttonText = "No Space!";
                            textColor = Color.Red;
                        }
                        else
                        {
                            buttonText = "Unequip";
                        }
                    }

                    int buttonWidth = 190;
                    int buttonHeight = 50;
                    int buttonX = infoContentX;
                    int buttonY = infoRegionY - buttonHeight;
                    Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

                    // Button background (gray)
                    spriteBatch.Draw(Main.Pixel, buttonRect, Color.Gray);

                    // Button borders
                    spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX, buttonY, buttonWidth, 2), Color.White); // Top
                    spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX, buttonY, 2, buttonHeight), Color.White); // Left
                    spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX, buttonY + buttonHeight - 2, buttonWidth, 2), Color.Black); // Bottom
                    spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX + buttonWidth - 2, buttonY, 2, buttonHeight), Color.Black); // Right

                    // Button text
                    Vector2 textSize = font.MeasureString(buttonText);
                    Vector2 textPos = new Vector2(
                        buttonX + (buttonWidth - textSize.X) / 2,
                        buttonY + (buttonHeight - textSize.Y) / 2
                    );
                    font.DrawString(spriteBatch, buttonText, textPos, textColor);
                }
            }
            int equipmentBoxWidth = 570;
            int equipmentBoxHeight = slotHeight * VisibleRows;
            int equipmentBoxX = startX - equipmentBoxWidth; // 30px gap to the left of inventory
            int equipmentBoxY = startY;
            Rectangle equipmentBoxRect = new Rectangle(equipmentBoxX, equipmentBoxY, equipmentBoxWidth, equipmentBoxHeight);

            // Draw stats button above equipment box
            int statsButtonWidth = 190;
            int statsButtonHeight = 50;
            int statsButtonX = equipmentBoxX;
            int statsButtonY = equipmentBoxY - statsButtonHeight;
            Rectangle statsButtonRect = new Rectangle(statsButtonX, statsButtonY, statsButtonWidth, statsButtonHeight);

            spriteBatch.Draw(Main.Pixel, statsButtonRect, Color.Gray);
            spriteBatch.Draw(Main.Pixel, new Rectangle(statsButtonX, statsButtonY, statsButtonWidth, 2), Color.White);
            spriteBatch.Draw(Main.Pixel, new Rectangle(statsButtonX, statsButtonY, 2, statsButtonHeight), Color.White);
            spriteBatch.Draw(Main.Pixel, new Rectangle(statsButtonX, statsButtonY + statsButtonHeight - 2, statsButtonWidth, 2), Color.Black);
            spriteBatch.Draw(Main.Pixel, new Rectangle(statsButtonX + statsButtonWidth - 2, statsButtonY, 2, statsButtonHeight), Color.Black);

            string statsButtonText = "Details";
            Vector2 statsTextSize = font.MeasureString(statsButtonText);
            Vector2 statsTextPos = new Vector2(
                statsButtonX + (statsButtonWidth - statsTextSize.X) / 2,
                statsButtonY + (statsButtonHeight - statsTextSize.Y) / 2
            );
            font.DrawString(spriteBatch, statsButtonText, statsTextPos, Color.White);

            // Update stats box animation
            float targetOffset = isStatsVisible ? 0f : -220f;
            statsBoxOffset = MathHelper.Lerp(statsBoxOffset, targetOffset, STATS_ANIMATION_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds);
            isAnimatingStats = Math.Abs(statsBoxOffset - targetOffset) > 1f;

            int statsBoxWidth = equipmentBoxWidth;
            int statsBoxHeight = 220;
            int statsBoxX = equipmentBoxX;
            int statsBoxY = equipmentBoxY + equipmentBoxHeight + (int)statsBoxOffset;
            Rectangle statsBoxRect = new Rectangle(statsBoxX, statsBoxY, statsBoxWidth, statsBoxHeight);

            DrawNineSliceBox(spriteBatch, statBoxTexture, statsBoxRect, StatBoxSpriteSize, Color.White);
            DrawNineSliceBox(spriteBatch, statBoxOutlineTexture, statsBoxRect, StatBoxSpriteSize, Color.White);

            int statsY = statsBoxY + 20;
            int statsX = statsBoxX + 20;
            int statSpacingX = StatIconSize + 5;
            int statSpacingY = 30;

            for (int i = 0; i <= 5; i++)
            {
                string statText = i switch
                {
                    0 => player.MaxHealth.ToString(),
                    1 => player.Speed.ToString(),
                    2 => player.Damage.ToString(),
                    3 => player.Knockback.ToString(),
                    4 => player.Defense.ToString(),
                    5 => (player.KnockbackResistance * 100f).ToString() + "%",
                    _ => "Unknown Stat"
                };

                Rectangle statsIconSrc = new Rectangle(0, i * StatIconSize, StatIconSize, StatIconSize);
                spriteBatch.Draw(statIconsTexture, new Vector2(statsX, statsY + i * statSpacingY), statsIconSrc, Color.White);
                font.DrawString(spriteBatch, statText, new Vector2(statsX + statSpacingX, statsY + i * statSpacingY + 2), Color.White);
            }

            DrawNineSliceBox(spriteBatch, statBoxTexture, equipmentBoxRect, StatBoxSpriteSize, Color.White, false);

            int eqSlotSize = 100;
            int eqSlotPaddingY = 30;
            int eqSlotPaddingX = 40;
            int eqSlotGapY = ((equipmentBoxHeight - eqSlotPaddingY * 2) - (eqSlotSize * 4)) / 3;

            EnsurePlayerPortraitRenderTarget(spriteBatch.GraphicsDevice);
            RenderPlayerPortrait(spriteBatch.GraphicsDevice, player, gameTime);

            // Draw player portrait in center of equipment box
            Vector2 playerPortraitPos = new Vector2(
                equipmentBoxRect.X + equipmentBoxRect.Width / 2,
                equipmentBoxRect.Y + equipmentBoxRect.Height / 2
            );

            float portraitScale = 1f;
            Vector2 portraitOrigin = new Vector2(PortraitSize / 2, PortraitSize / 2);
            spriteBatch.Draw(playerPortraitRenderTarget, playerPortraitPos, null, Color.White, 0f, portraitOrigin, portraitScale, SpriteEffects.None, 0.32f);
            for (int col = 0; col < 2; col++)
            {
                for (int i = 0; i < 4; i++)
                {
                    int slotY = equipmentBoxY + eqSlotPaddingY + i * (eqSlotSize + eqSlotGapY);
                    int slotX = (col == 0)
                        ? equipmentBoxX + eqSlotPaddingX
                        : equipmentBoxX + equipmentBoxWidth - eqSlotPaddingX - eqSlotSize;
                    Item equipped = null;
                    if (col == 0)
                    {
                        if (i == 0) player.EquippedItems.TryGetValue(EquipmentSlot.Weapon, out equipped);
                        else if (i == 1) player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out equipped);
                    }
                    else
                    {
                        if (i == 0) player.EquippedItems.TryGetValue(EquipmentSlot.Offhand, out equipped);
                        else if (i == 1) player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out equipped);
                    }
                    int rarity = equipped != null ? equipped.Rarity : 0;
                    if (rarity < 0 || rarity > 6) rarity = 0;
                    var rarityInfo = equipped != null ? Item.GetRarityInfo(equipped.Rarity) : Item.GetRarityInfo(0);
                    Rectangle eqSlotSrcRect = new Rectangle(0, 0, eqSlotSize, eqSlotSize);
                    Rectangle eqSlotRect = new Rectangle(slotX, slotY, eqSlotSize, eqSlotSize);
                    spriteBatch.Draw(slotTexture, eqSlotRect, eqSlotSrcRect, Color.White);
                    if (equipped != null && equipped.Texture != null)
                    {
                        int itemAreaSize = 100;
                        int itemAreaOffset = (slotWidth - itemAreaSize) / 2;
                        var tex = equipped.Texture;
                        int srcW = tex.Width;
                        int srcH = tex.Height;
                        string type = equipped.Type ?? "";
                        bool isReducedScale = type.Contains("[Chestplate]") || type.Contains("[Helmet]") || type.Contains("[Offhand]") || type.Contains("Gun");
                        Rectangle srcRectSwordStaff = new Rectangle(0, 0, srcW, srcH);
                        float baseScale = Math.Min((float)itemAreaSize / srcW, (float)itemAreaSize / srcH) * 0.8f;
                        float scale = isReducedScale ? baseScale * 0.8f : baseScale;
                        int drawWidth = (int)(srcW * scale);
                        int drawHeight = (int)(srcH * scale);
                        int drawX = slotX + itemAreaOffset + (itemAreaSize - drawWidth) / 2;
                        int drawXShadow = slotX + itemAreaOffset + (int)(itemAreaSize - drawWidth * 1.1f) / 2;
                        int drawY = slotY + itemAreaOffset + (itemAreaSize - drawHeight) / 2;
                        Rectangle drawRect = new Rectangle(drawX, drawY, drawWidth, drawHeight);
                        Rectangle drawRectShadow = new Rectangle(drawXShadow, drawY + 10, (int)(drawWidth * 1.1f), (int)(drawHeight * 1.1f));
                        spriteBatch.Draw(tex, drawRectShadow, srcRectSwordStaff, Color.Black * 0.3f);
                        spriteBatch.Draw(tex, drawRect, Color.White);
                    }
                }
            }

            for (int i = 0; i <= 4; i++)
            {
                float targetOffsetSort = (chosenSortingMethod == i) ? TARGET_OFFSET : 0f;
                sortBoxOffsets[i] = MathHelper.Lerp(
                    sortBoxOffsets[i],
                    targetOffsetSort,
                    STATS_ANIMATION_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds
                );

                int buttonWidth = slotWidth / 2;
                int buttonHeight = slotHeight / 2;
                int buttonX = (screenWidth - (slotWidth * Columns)) / 2 + buttonWidth * i;
                int buttonY = (int)((screenHeight - (slotHeight * VisibleRows)) / 2 - buttonHeight - sortBoxOffsets[i]);

                Rectangle buttonRect = new Rectangle(
                    buttonX,
                    buttonY,
                    buttonWidth,
                    (int)(buttonHeight + sortBoxOffsets[i])
                );

                // Pick color
                Color buttonColor = i switch
                {
                    0 => Color.LightCoral,
                    1 => Color.LightSkyBlue,
                    2 => Color.LightGoldenrodYellow,
                    3 => Color.Coral,
                    4 => Color.LightGreen,
                    _ => Color.Black
                };

                // Draw button background
                spriteBatch.Draw(Main.Pixel, buttonRect, buttonColor);

                // Draw borders
                spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX, buttonY, buttonWidth, 2), Color.White); // Top
                spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX, buttonY, 2, buttonHeight), Color.White); // Left
                spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX, buttonY + buttonHeight - 2, buttonWidth, 2), Color.Black); // Bottom
                spriteBatch.Draw(Main.Pixel, new Rectangle(buttonX + buttonWidth - 2, buttonY, 2, buttonHeight), Color.Black); // Right

                // Draw icon
                int textureID = i switch
                {
                    0 => 2,  // Damage
                    1 => 4,  // Defense
                    2 => 8,  // Value/Gold
                    3 => 10, // Rarity
                    4 => 9,  // Item Type
                    _ => -1
                };
                Rectangle statsIconSrc = new Rectangle(0, textureID * StatIconSize, StatIconSize, StatIconSize);
                spriteBatch.Draw(statIconsTexture, new Vector2(
                    buttonX + (buttonWidth - StatIconSize) / 2,
                    buttonY + (buttonHeight - StatIconSize) / 2
                ), statsIconSrc, Color.White);
            }
        }

        #region Helper Methods

        public Item GetItem(int slot) => (slot >= 0 && slot < TotalSlots) ? items[slot] : null;

        public void SetItem(int slot, Item item)
        {
            if (slot >= 0 && slot < TotalSlots) items[slot] = item;
        }

        public int SlotWidth => slotWidth;

        private static readonly Dictionary<string, EquipmentSlot> ItemTypeMap = new Dictionary<string, EquipmentSlot>
        {
            ["sword"] = EquipmentSlot.Weapon,
            ["staff"] = EquipmentSlot.Weapon,
            ["gun"] = EquipmentSlot.Weapon,
            ["weapon"] = EquipmentSlot.Weapon,
            ["helmet"] = EquipmentSlot.Helmet,
            ["chestplate"] = EquipmentSlot.Chestplate,
            ["offhand"] = EquipmentSlot.Offhand
        };

        private EquipmentSlot? GetEquipmentSlotForItem(Item item)
        {
            if (item?.Type == null) return null;
            string type = item.Type.ToLower();
            return ItemTypeMap.FirstOrDefault(kvp => type.Contains(kvp.Key)).Value;
        }

        private static readonly Dictionary<string, Func<Item, float>> StatExtractors = new Dictionary<string, Func<Item, float>>
        {
            ["damage"] = item => item.Damage,
            ["defense"] = item => item.Defense,
            ["knockback"] = item => item.Knockback,
            ["range"] = item => item.ShootSpeed,
            ["uses per second"] = item => item.UseTime > 0 ? 1f / item.UseTime : 0f,
            ["knockback resistance"] = item => item.KnockbackResistance,
            ["gold"] = item => item.Value
        };

        private Color GetStatComparisonColor(string statText, Item item, Player player, int slotIdx)
        {
            if (slotIdx < 0) return Color.White;
            var targetSlot = GetEquipmentSlotForItem(item);
            if (!targetSlot.HasValue || !player.EquippedItems.TryGetValue(targetSlot.Value, out var equipped) || equipped == null) return Color.White;

            var statKey = StatExtractors.Keys.FirstOrDefault(key => statText.Contains(key) && (key != "knockback" || !statText.Contains("resistance")));
            if (statKey == null) return Color.White;

            float itemStat = StatExtractors[statKey](item);
            float equippedStat = StatExtractors[statKey](equipped);
            return itemStat > equippedStat ? Color.LightGreen : itemStat < equippedStat ? Color.LightCoral : Color.White;
        }

        private void SortInventory()
        {
            if (chosenSortingMethod < 0 || chosenSortingMethod > 4) return;

            // Create list of non-null items with their original indices
            var itemsWithIndices = new List<(Item item, int originalIndex)>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    itemsWithIndices.Add((items[i], i));
                }
            }

            // Sort based on chosen method (descending order - best items first)
            switch (chosenSortingMethod)
            {
                case 0: // Damage
                    itemsWithIndices.Sort((a, b) => b.item.Damage.CompareTo(a.item.Damage));
                    break;

                case 1: // Defense
                    itemsWithIndices.Sort((a, b) => b.item.Defense.CompareTo(a.item.Defense));
                    break;

                case 2: // Value
                    itemsWithIndices.Sort((a, b) => b.item.Value.CompareTo(a.item.Value));
                    break;

                case 3: // Rarity
                    itemsWithIndices.Sort((a, b) => b.item.Rarity.CompareTo(a.item.Rarity));
                    break;

                case 4: // Item Type
                    itemsWithIndices.Sort((a, b) => string.Compare(a.item.Type ?? "", b.item.Type ?? "", StringComparison.OrdinalIgnoreCase));
                    break;
            }

            // Clear inventory
            for (int i = 0; i < items.Count; i++)
            {
                items[i] = null;
            }

            // Place sorted items back starting from the beginning
            for (int i = 0; i < itemsWithIndices.Count; i++)
            {
                items[i] = itemsWithIndices[i].item;
            }

            // Update selected item slot if an item was selected
            if (selectedItemSlot.HasValue && selectedItemSlot.Value >= 0)
            {
                var selectedItem = itemsWithIndices.FirstOrDefault(x => x.originalIndex == selectedItemSlot.Value).item;
                if (selectedItem != null)
                {
                    // Find new position of the selected item
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i] == selectedItem)
                        {
                            selectedItemSlot = i;
                            break;
                        }
                    }
                }
                else
                {
                    selectedItemSlot = null;
                }
            }
        }

        #endregion Helper Methods

        public void Update(TouchCollection touches, Player player)
        {
            if (!IsOpen) return;

            // Check if sorting method changed and trigger sort
            if (chosenSortingMethod != previousSortingMethod && chosenSortingMethod >= 0)
            {
                SortInventory();
                previousSortingMethod = chosenSortingMethod;
            }
            bool mouseClicked = false;
            Point mousePoint = Point.Zero;
            // PC: Mouse click and wheel support for item selection and scrolling
            var mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            int mouseWheelDelta = mouseState.ScrollWheelValue - previousWheelValue;
            previousWheelValue = mouseState.ScrollWheelValue;
            if (mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                mouseClicked = true;
                mousePoint = new Point(mouseState.X, mouseState.Y);
            }
            // Mouse wheel scrolling
            if (mouseWheelDelta != 0)
            {
                // Inventory region
                int startY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                int startX = (screenWidth - (slotWidth * Columns)) / 2;
                Rectangle inventoryRect = new Rectangle(startX, startY, slotWidth * Columns, slotHeight * VisibleRows);
                // Info box region
                int statBoxStartX = (screenWidth - (slotWidth * Columns)) / 2;
                int statBoxScrollbarWidth = 15;
                int statBoxScrollbarX = statBoxStartX + slotWidth * Columns;
                int infoRegionX = statBoxScrollbarX + statBoxScrollbarWidth;
                int infoRegionY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                int iconBoxHeight = 0;
                if (selectedItemSlot.HasValue)
                {
                    int slotIdx = selectedItemSlot.Value;
                    Item selected = null;
                    if (slotIdx >= 0 && slotIdx < items.Count)
                    {
                        selected = items[slotIdx];
                    }
                    else if (slotIdx == -100)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Weapon, out selected);
                    }
                    else if (slotIdx == -101)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out selected);
                    }
                    else if (slotIdx == -102)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Offhand, out selected);
                    }
                    else if (slotIdx == -103)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out selected);
                    }
                    if (selected != null && selected.Texture != null)
                    {
                        float iconScale = 0.8f;
                        int texH = selected.Texture.Height;
                        iconBoxHeight = (int)(texH * iconScale) + 35 * 2;
                    }
                }
                int infoBoxScreenW = infoBoxRenderTarget != null ? infoBoxRenderTarget.Width : 0;
                int infoBoxScreenH = slotHeight * VisibleRows;
                int infoContentBoxHeight = infoBoxScreenH - iconBoxHeight;
                if (infoContentBoxHeight < 40) infoContentBoxHeight = 40;
                Rectangle infoContentRect = new Rectangle(infoRegionX, infoRegionY + iconBoxHeight, infoBoxScreenW, infoContentBoxHeight);
                // Mouse over inventory: scroll inventory
                if (inventoryRect.Contains(mouseState.X, mouseState.Y))
                {
                    int scrollAmount = mouseWheelDelta / 120; // 120 units per notch
                    Scroll(-scrollAmount);
                }
                // Mouse over info box: scroll info box
                if (infoContentRect.Contains(mouseState.X, mouseState.Y))
                {
                    int scrollAmount = mouseWheelDelta / 120; // 120 units per notch
                    infoBoxScrollOffset -= scrollAmount * 30; // 30px per notch
                    float maxScroll = Math.Max(0, infoBoxRenderTarget != null ? infoBoxRenderTarget.Height - (infoContentBoxHeight - IconBoxPadding) : 0);
                    if (infoBoxScrollOffset > maxScroll) infoBoxScrollOffset = maxScroll;
                    if (infoBoxScrollOffset < 0) infoBoxScrollOffset = 0;
                }
            }
            if (touches.Count > 0 || mouseClicked)
            {
                var touch = touches.Count > 0 ? touches[0] : default(TouchLocation);
                int statBoxStartX = (screenWidth - (slotWidth * Columns)) / 2;
                int statBoxScrollbarWidth = 15;
                int statBoxScrollbarX = statBoxStartX + slotWidth * Columns;
                int infoRegionX = statBoxScrollbarX + statBoxScrollbarWidth;
                int infoRegionY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                int iconBoxHeight = 0;
                if (selectedItemSlot.HasValue)
                {
                    int slotIdx = selectedItemSlot.Value;
                    Item selected = null;
                    if (slotIdx >= 0 && slotIdx < items.Count)
                    {
                        selected = items[slotIdx];
                    }
                    else if (slotIdx == -100)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Weapon, out selected);
                    }
                    else if (slotIdx == -101)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out selected);
                    }
                    else if (slotIdx == -102)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Offhand, out selected);
                    }
                    else if (slotIdx == -103)
                    {
                        player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out selected);
                    }
                    if (selected != null && selected.Texture != null)
                    {
                        float iconScale = 0.8f;
                        int texH = selected.Texture.Height;
                        iconBoxHeight = (int)(texH * iconScale) + 35 * 2; // match draw logic
                    }
                }
                int infoBoxScreenW = infoBoxRenderTarget != null ? infoBoxRenderTarget.Width : 0;
                int infoBoxScreenH = slotHeight * VisibleRows;
                int infoContentBoxHeight = infoBoxScreenH - iconBoxHeight;
                if (infoContentBoxHeight < 40) infoContentBoxHeight = 40;
                Rectangle infoContentRect = new Rectangle(infoRegionX, infoRegionY + iconBoxHeight, infoBoxScreenW, infoContentBoxHeight);
                bool isPressed = touches.Count > 0 ? touch.State == TouchLocationState.Pressed : mouseClicked;
                Vector2 inputPos = touches.Count > 0 ? touch.Position : new Vector2(mousePoint.X, mousePoint.Y);
                if (isPressed)
                {
                    var equipmentField = typeof(Player).GetField("Equipment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var equipment = (Dictionary<EquipmentSlot, Item>)equipmentField.GetValue(player);
                    if (selectedItemSlot.HasValue)
                    {
                        int slotIdx = selectedItemSlot.Value;
                        Item selected = null;
                        if (slotIdx >= 0 && slotIdx < items.Count)
                        {
                            selected = items[slotIdx];
                        }
                        else if (slotIdx < 0)
                        {
                            // Get equipped item for button display
                            if (slotIdx == -100) equipment.TryGetValue(EquipmentSlot.Weapon, out selected);
                            else if (slotIdx == -101) equipment.TryGetValue(EquipmentSlot.Helmet, out selected);
                            else if (slotIdx == -102) equipment.TryGetValue(EquipmentSlot.Offhand, out selected);
                            else if (slotIdx == -103) equipment.TryGetValue(EquipmentSlot.Chestplate, out selected);
                        }

                        if (selected != null || slotIdx < 0)
                        {
                            int buttonWidth = 190;
                            int buttonHeight = 50;
                            int marginX = 40;
                            int infoBoxW = infoBoxRenderTarget != null ? infoBoxRenderTarget.Width : 0;
                            int infoBoxScreenX = Math.Max(marginX, Math.Min(screenWidth - infoBoxW - marginX, infoRegionX));
                            int buttonX = infoBoxScreenX;
                            int buttonY = infoRegionY - buttonHeight;
                            Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

                            if (buttonRect.Contains(inputPos))
                            {
                                if (slotIdx < 0) // Unequip equipped item
                                {
                                    // Check if inventory has space first
                                    bool hasSpace = false;
                                    for (int i = 0; i < items.Count; i++)
                                    {
                                        if (items[i] == null)
                                        {
                                            hasSpace = true;
                                            break;
                                        }
                                    }

                                    if (!hasSpace) return; // Don't unequip if no space

                                    // Get the equipped item directly
                                    Item equippedItem = null;
                                    EquipmentSlot slot = EquipmentSlot.Weapon;

                                    if (slotIdx == -100) { equipment.TryGetValue(EquipmentSlot.Weapon, out equippedItem); slot = EquipmentSlot.Weapon; }
                                    else if (slotIdx == -101) { equipment.TryGetValue(EquipmentSlot.Helmet, out equippedItem); slot = EquipmentSlot.Helmet; }
                                    else if (slotIdx == -102) { equipment.TryGetValue(EquipmentSlot.Offhand, out equippedItem); slot = EquipmentSlot.Offhand; }
                                    else if (slotIdx == -103) { equipment.TryGetValue(EquipmentSlot.Chestplate, out equippedItem); slot = EquipmentSlot.Chestplate; }

                                    if (equippedItem != null)
                                    {
                                        // Find empty inventory slot
                                        for (int i = 0; i < items.Count; i++)
                                        {
                                            if (items[i] == null)
                                            {
                                                items[i] = equippedItem;
                                                equipment[slot] = null;
                                                selectedItemSlot = null;
                                                return;
                                            }
                                        }
                                    }
                                }
                                else // Equip inventory item
                                {
                                    EquipmentSlot? targetSlot = GetEquipmentSlotForItem(selected);
                                    if (targetSlot.HasValue)
                                    {
                                        player.EquippedItems.TryGetValue(targetSlot.Value, out Item currentlyEquipped);
                                        equipment[targetSlot.Value] = selected;
                                        items[slotIdx] = currentlyEquipped;
                                        selectedItemSlot = null;
                                    }
                                }
                                return;
                            }
                        }
                    }

                    if (selectedItemSlot.HasValue && infoContentRect.Contains(inputPos))
                    {
                        infoBoxIsTouching = true;
                        infoBoxLastTouchY = inputPos.Y;
                    }
                    else
                    {
                        isTouching = true;
                        lastTouchY = inputPos.Y;
                        // Only allow selection if inside inventory grid area or equipment slots
                        int startY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                        int startX = (screenWidth - (slotWidth * Columns)) / 2;
                        Rectangle inventoryRect = new Rectangle(startX, startY, slotWidth * Columns, slotHeight * VisibleRows);
                        int equipmentBoxWidth = 570;
                        int equipmentBoxHeight = slotHeight * VisibleRows;
                        int equipmentBoxX = startX - equipmentBoxWidth;
                        int equipmentBoxY = startY;

                        // Check stats button
                        int statsButtonWidth = 190;
                        int statsButtonHeight = 50;
                        int statsButtonX = equipmentBoxX;
                        int statsButtonY = equipmentBoxY - statsButtonHeight;
                        Rectangle statsButtonRect = new Rectangle(statsButtonX, statsButtonY, statsButtonWidth, statsButtonHeight);

                        if (statsButtonRect.Contains((int)inputPos.X, (int)inputPos.Y) && !isAnimatingStats)
                        {
                            isStatsVisible = !isStatsVisible;
                        }

                        for (int i = 0; i <= 4; i++)
                        {
                            int sortButtonWidth = slotWidth / 2;
                            int sortButtonHeight = slotHeight / 2;
                            int sortButtonX = (screenWidth - (slotWidth * Columns)) / 2 + sortButtonWidth * i;
                            int sortButtonY = (screenHeight - (slotHeight * VisibleRows)) / 2 - sortButtonHeight;

                            Rectangle sortButtonRect = new Rectangle(sortButtonX, sortButtonY, sortButtonWidth, sortButtonHeight);

                            if (sortButtonRect.Contains((int)inputPos.X, (int)inputPos.Y))
                            {
                                chosenSortingMethod = i;
                            }
                        }

                        int eqSlotSize = 120;
                        int eqSlotPaddingY = 30;
                        int eqSlotPaddingX = 40;
                        int eqSlotGapY = ((equipmentBoxHeight - eqSlotPaddingY * 2) - (eqSlotSize * 4)) / 3;
                        bool foundEquipSlot = false;
                        // Check equipment slots (left column)
                        for (int i = 0; i < 4; i++)
                        {
                            int slotY = equipmentBoxY + eqSlotPaddingY + i * (eqSlotSize + eqSlotGapY);
                            int slotX = equipmentBoxX + eqSlotPaddingX;
                            Rectangle eqSlotRect = new Rectangle(slotX, slotY, eqSlotSize, eqSlotSize);
                            if (eqSlotRect.Contains((int)inputPos.X, (int)inputPos.Y))
                            {
                                // Weapon (i==0), Helmet (i==1)
                                if (i == 0 && player.EquippedItems.TryGetValue(EquipmentSlot.Weapon, out var weaponItem) && weaponItem != null)
                                    selectedItemSlot = -100;
                                else if (i == 1 && player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out var helmetItem) && helmetItem != null)
                                    selectedItemSlot = -101;
                                else
                                    selectedItemSlot = null;
                                foundEquipSlot = true;
                                break;
                            }
                        }
                        // Check equipment slots (right column) if not found
                        if (!foundEquipSlot)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                int slotY = equipmentBoxY + eqSlotPaddingY + i * (eqSlotSize + eqSlotGapY);
                                int slotX = equipmentBoxX + equipmentBoxWidth - eqSlotPaddingX - eqSlotSize;
                                Rectangle eqSlotRect = new Rectangle(slotX, slotY, eqSlotSize, eqSlotSize);
                                if (eqSlotRect.Contains((int)inputPos.X, (int)inputPos.Y))
                                {
                                    // Offhand (i==0), Chestplate (i==1)
                                    if (i == 0 && player.EquippedItems.TryGetValue(EquipmentSlot.Offhand, out var offhandItem) && offhandItem != null)
                                        selectedItemSlot = -102;
                                    else if (i == 1 && player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out var chestItem) && chestItem != null)
                                        selectedItemSlot = -103;
                                    else
                                        selectedItemSlot = null;
                                    foundEquipSlot = true;
                                    break;
                                }
                            }
                        }
                        // If not found in equipment, check inventory
                        if (!foundEquipSlot)
                        {
                            if (inventoryRect.Contains((int)inputPos.X, (int)inputPos.Y))
                            {
                                int localX = (int)inputPos.X - startX;
                                int localY = (int)inputPos.Y - startY;
                                int col = localX / slotWidth;
                                int row = localY / slotHeight;
                                if (col >= 0 && col < Columns && row >= 0 && row < VisibleRows)
                                {
                                    int slotIndex = ((int)scrollOffset + row) * Columns + col;
                                    if (slotIndex >= 0 && slotIndex < TotalSlots && GetItem(slotIndex) != null)
                                        selectedItemSlot = slotIndex;
                                    else
                                        selectedItemSlot = null;
                                }
                                else
                                {
                                    selectedItemSlot = null;
                                }
                            }
                            else
                            {
                                selectedItemSlot = null;
                            }
                        }
                    }
                }
                else if (touches.Count > 0 && touch.State == TouchLocationState.Moved)
                {
                    if (infoBoxIsTouching)
                    {
                        float deltaY = touch.Position.Y - infoBoxLastTouchY;
                        infoBoxScrollOffset -= deltaY;
                        float maxScroll = Math.Max(0, infoBoxRenderTarget != null ? infoBoxRenderTarget.Height - (infoContentBoxHeight - IconBoxPadding) : 0);
                        if (infoBoxScrollOffset > maxScroll) infoBoxScrollOffset = maxScroll;
                        if (infoBoxScrollOffset < 0) infoBoxScrollOffset = 0;
                        infoBoxLastTouchY = touch.Position.Y;
                    }
                    else if (isTouching)
                    {
                        float deltaY = touch.Position.Y - lastTouchY;
                        float deltaRows = -deltaY / slotHeight * 0.5f;
                        Scroll(deltaRows);
                        lastTouchY = touch.Position.Y;
                    }
                }
                else if (touches.Count > 0 && touch.State == TouchLocationState.Released)
                {
                    infoBoxIsTouching = false;
                    isTouching = false;
                }
            }
            else
            {
                infoBoxIsTouching = false;
                isTouching = false;
            }
            // Reset scroll when changing selection
            if (selectedItemSlot != lastSelectedItemSlot)
            {
                infoBoxScrollOffset = 0f;
                lastSelectedItemSlot = selectedItemSlot ?? -1;
                // Dispose old render target to force recalculation of content bounds
                infoBoxRenderTarget?.Dispose();
                infoBoxRenderTarget = null;
            }
        }

        public void TryPickingItem(Player player, List<ItemDrop> drops, FloatingTextManager floatingTextManager)
        {
            var playerHitbox = player.Hitbox;
            foreach (var drop in drops.ToList())
            {
                Rectangle itemRect = new Rectangle((int)drop.Position.X, (int)drop.Position.Y, drop.Item.Texture.Width, drop.Item.Texture.Height);
                if (playerHitbox.Intersects(itemRect))
                {
                    bool picked = TryAddItemToInventory(drop.Item);
                    if (picked)
                    {
                        drops.Remove(drop);
                        return;
                    }
                    else
                    {
                        floatingTextManager.Add(
                            "Inventory Full",
                            player.Position,
                            Color.Red,
                            Color.Red,
                            1.2f,
                            player,
                            true
                        );
                        return;
                    }
                }
            }
        }

        private bool TryAddItemToInventory(Item item)
        {
            if (item.IsStackable)
            {
                for (int i = 0; i < TotalSlots; i++)
                {
                    var invItem = GetItem(i);
                    if (invItem != null && invItem.ID == item.ID && invItem.CurrentStack < invItem.MaxStack)
                    {
                        int space = invItem.MaxStack - invItem.CurrentStack;
                        int toAdd = Math.Min(space, item.CurrentStack);
                        invItem.CurrentStack += toAdd;
                        item.CurrentStack -= toAdd;
                        if (item.CurrentStack <= 0) return true;
                    }
                }
                for (int i = 0; i < TotalSlots; i++)
                {
                    if (GetItem(i) == null)
                    {
                        SetItem(i, item);
                        return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < TotalSlots; i++)
            {
                if (GetItem(i) == null)
                {
                    SetItem(i, item);
                    return true;
                }
            }
            return false;
        }

        #region Portrait Rendering

        private void RenderPlayerPortrait(GraphicsDevice graphicsDevice, Player player, GameTime gameTime)
        {
            var prevRenderTargets = graphicsDevice.GetRenderTargets();
            graphicsDevice.SetRenderTarget(playerPortraitRenderTarget);
            graphicsDevice.Clear(Color.Transparent);

            using (SpriteBatch portraitBatch = new SpriteBatch(graphicsDevice))
            {
                portraitBatch.Begin();

                // Set portrait rendering flag
                Item.IsRenderingPortrait = true;

                // Temporarily disable animation freeze for portrait effects
                bool originalFreezeState = Item.FreezeGameWorldAnimations;
                Item.FreezeGameWorldAnimations = false;

                // Store original hitboxes to prevent teleportation
                Rectangle originalHitbox = player.Hitbox;
                Rectangle originalWeaponHitbox = player.WeaponHitbox;
                Rectangle originalOffhandHitbox = player.OffhandHitbox;
                Rectangle originalPlayerSpriteHitbox = player.PlayerSpriteHitbox;
                float originalWeaponRotation = player.WeaponHitboxRotation;

                // Create continuous GameTime for item animations
                TimeSpan totalTime = DateTime.Now - portraitStartTime;
                GameTime continuousGameTime = new GameTime(totalTime, TimeSpan.FromMilliseconds(16.67)); // ~60fps

                // Store original player values
                Vector2 originalPosition = player.Position;
                float originalScale = player.CurrentScale;
                bool originalFacingLeft = player.IsFacingLeft;
                bool originalIsJumping = player.IsJumping;
                bool originalIsMoving = player.IsMoving;
                bool originalIsKnocked = player.IsKnocked;
                bool originalIsImmune = player.IsImmune;
                float originalJumpTime = player.JumpTime;
                float originalWalkTimer = player.WalkTimer;

                // Store timer values using reflection
                var hurtFlashTimerField = typeof(Player).GetField("hurtFlashTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var knockbackTimerField = typeof(Player).GetField("KnockbackTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var knockbackRotationField = typeof(Player).GetField("KnockbackRotation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var headRotationField = typeof(Player).GetField("headRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var eyeRotationField = typeof(Player).GetField("eyeRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var eyeTimerField = typeof(Player).GetField("EyeTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isEyeOpenField = typeof(Player).GetField("IsEyeOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var eyeBlinkIntervalField = typeof(Player).GetField("EyeBlinkInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                float originalHurtFlashTimer = (float)(hurtFlashTimerField?.GetValue(player) ?? 0f);
                float originalKnockbackTimer = (float)(knockbackTimerField?.GetValue(player) ?? 0f);
                float originalKnockbackRotation = (float)(knockbackRotationField?.GetValue(player) ?? 0f);
                float originalHeadRotation = (float)(headRotationField?.GetValue(player) ?? 0f);
                float originalEyeRotation = (float)(eyeRotationField?.GetValue(player) ?? 0f);
                float originalEyeTimer = (float)(eyeTimerField?.GetValue(player) ?? 0f);
                bool originalIsEyeOpen = (bool)(isEyeOpenField?.GetValue(player) ?? true);
                float originalEyeBlinkInterval = (float)(eyeBlinkIntervalField?.GetValue(player) ?? 1f);
                bool originalIsAttacking = player.IsAttacking;

                // Set portrait values temporarily
                Vector2 portraitCenter = new Vector2(PortraitSize / 2 + 30, PortraitSize / 2 + 160);
                player.Position = portraitCenter - new Vector2(player.T_Body.Width / 2, player.T_Body.Height / 2);
                player.CurrentScale = 1.0f;
                player.IsFacingLeft = false;

                // Disable all animations and effects for static portrait
                typeof(Player).GetField("IsJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(player, false);
                typeof(Player).GetField("IsMoving", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, false);
                typeof(Player).GetField("IsKnocked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, false);
                typeof(Player).GetField("IsImmune", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, false);
                typeof(Player).GetField("jumpTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, 0.75f);
                hurtFlashTimerField?.SetValue(player, 0f);
                knockbackTimerField?.SetValue(player, 0f);
                knockbackRotationField?.SetValue(player, 0f);
                headRotationField?.SetValue(player, 0f);
                eyeRotationField?.SetValue(player, 0f);

                // Force eyes to stay open
                eyeTimerField?.SetValue(player, 0f);
                isEyeOpenField?.SetValue(player, true);
                eyeBlinkIntervalField?.SetValue(player, 999f); // Very long interval to prevent blinking

                // Force player to not be attacking for weapon idle animations
                typeof(Player).GetField("IsAttacking", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, false);

                // Update hitboxes for portrait rendering so item effects can draw properly
                player.UpdateHitboxes(continuousGameTime);

                // Disable head rotation by clearing movement and attack directions
                var movementDirectionField = typeof(Player).GetField("MovementDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var attackDirectionField = typeof(Player).GetField("attackDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Vector2 originalMovementDirection = (Vector2)(movementDirectionField?.GetValue(player) ?? Vector2.Zero);
                Vector2 originalAttackDirection = (Vector2)(attackDirectionField?.GetValue(player) ?? Vector2.Zero);
                movementDirectionField?.SetValue(player, Vector2.Zero);
                attackDirectionField?.SetValue(player, Vector2.Zero);

                // Update only portrait particles and items
                float deltaTime = (float)continuousGameTime.ElapsedGameTime.TotalSeconds;
                portraitParticleManager.Update(deltaTime);

                // Temporarily replace player's particle manager with portrait one
                var originalParticleManager = player.particle;
                var particleField = typeof(Player).GetField("particle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                particleField?.SetValue(player, portraitParticleManager);

                foreach (var item in player.EquippedItems.Values)
                {
                    if (item != null)
                    {
                        // Temporarily replace item's particle manager
                        var itemParticleField = typeof(Item).GetField("particle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var originalItemParticle = itemParticleField?.GetValue(item);
                        itemParticleField?.SetValue(item, portraitParticleManager);

                        item.UpdateParticles(deltaTime, continuousGameTime, player);

                        // Restore original particle manager
                        itemParticleField?.SetValue(item, originalItemParticle);
                    }
                }

                // Restore original particle manager
                particleField?.SetValue(player, originalParticleManager);

                portraitBatch.End();

                // Draw particles with identity camera (no transform)
                var identityCamera = new Camera(Vector2.Zero, PortraitSize, PortraitSize * 2);
                // Position camera to center particles on player in portrait
                identityCamera.Position = Vector2.Zero;
                identityCamera.Zoom = 1.0f; // Ensure true 1:1 coordinate system
                portraitParticleManager.Draw(portraitBatch.GraphicsDevice, identityCamera, 2);
                portraitParticleManager.Draw(portraitBatch.GraphicsDevice, identityCamera, 0);

                portraitBatch.Begin();

                player.DrawShadow(portraitBatch, 0.3f);

                foreach (var item in player.EquippedItems.Values)
                {
                    if (item != null && item.DrawSlot == DrawSlot.BelowBody)
                    {
                        item.PreDraw(portraitBatch, continuousGameTime, player, 0.309f);
                        item.PostDraw(portraitBatch, continuousGameTime, player, 0.309f);
                    }
                }
                player.DrawBaseBody(portraitBatch, continuousGameTime, 0.31f);

                foreach (var item in player.EquippedItems.Values)
                {
                    if (item != null && item.DrawSlot == DrawSlot.AboveBody)
                    {
                        item.PreDraw(portraitBatch, continuousGameTime, player, 0.312f);
                        item.PostDraw(portraitBatch, continuousGameTime, player, 0.312f);
                    }
                }

                Vector2 headOffset = new Vector2(0f, 5f);
                Vector2 headEyeOrigin = new Vector2(player.T_Head.Width / 2, player.T_Head.Height);
                portraitBatch.Draw(player.T_Head, player.Position + headOffset, null, player.GetColor(), 0f, headEyeOrigin, 1f,
                    SpriteEffects.None, 0.31f + 0.002f);
                portraitBatch.Draw(player.T_Eye, player.Position + headOffset * 2, new Rectangle(0, 0, player.T_Eye.Width, player.T_Eye.Height / 2),
                    Color.White, 0f, new Vector2(player.T_Eye.Width / 2, player.T_Eye.Height / 2),
                    1f, SpriteEffects.None, 0.31f + 0.003f);

                foreach (var item in player.EquippedItems.Values)
                {
                    if (item != null && item.DrawSlot == DrawSlot.BelowHead)
                    {
                        item.PreDraw(portraitBatch, continuousGameTime, player, 0.313f);
                        item.PostDraw(portraitBatch, continuousGameTime, player, 0.313f);
                    }
                }
                foreach (var item in player.EquippedItems.Values)
                {
                    if (item != null && item.DrawSlot == DrawSlot.AboveHead)
                    {
                        item.PreDraw(portraitBatch, continuousGameTime, player, 0.314f);
                        item.PostDraw(portraitBatch, continuousGameTime, player, 0.314f);
                    }
                }

                foreach (var item in player.EquippedItems.Values)
                {
                    if (item != null && item.DrawSlot == DrawSlot.Offhand)
                    {
                        item.PreDraw(portraitBatch, continuousGameTime, player, 0.315f);
                        item.PostDraw(portraitBatch, continuousGameTime, player, 0.315f);
                    }
                }

                portraitBatch.End();

                portraitParticleManager.Draw(portraitBatch.GraphicsDevice, identityCamera, 1);

                portraitBatch.Begin();

                player.Position = originalPosition;
                player.CurrentScale = originalScale;
                player.IsFacingLeft = originalFacingLeft;
                typeof(Player).GetField("IsJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(player, originalIsJumping);
                typeof(Player).GetField("IsMoving", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, originalIsMoving);
                typeof(Player).GetField("IsKnocked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, originalIsKnocked);
                typeof(Player).GetField("IsImmune", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, originalIsImmune);
                typeof(Player).GetField("jumpTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, originalJumpTime);

                hurtFlashTimerField?.SetValue(player, originalHurtFlashTimer);
                knockbackTimerField?.SetValue(player, originalKnockbackTimer);
                knockbackRotationField?.SetValue(player, originalKnockbackRotation);
                headRotationField?.SetValue(player, originalHeadRotation);
                eyeRotationField?.SetValue(player, originalEyeRotation);
                eyeTimerField?.SetValue(player, originalEyeTimer);
                isEyeOpenField?.SetValue(player, originalIsEyeOpen);
                eyeBlinkIntervalField?.SetValue(player, originalEyeBlinkInterval);
                typeof(Player).GetField("IsAttacking", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(player, originalIsAttacking);
                movementDirectionField?.SetValue(player, originalMovementDirection);
                attackDirectionField?.SetValue(player, originalAttackDirection);

                // Restore original hitboxes
                typeof(Player).GetProperty("Hitbox")?.SetValue(player, originalHitbox);
                typeof(Player).GetProperty("WeaponHitbox")?.SetValue(player, originalWeaponHitbox);
                typeof(Player).GetProperty("OffhandHitbox")?.SetValue(player, originalOffhandHitbox);
                typeof(Player).GetProperty("PlayerSpriteHitbox")?.SetValue(player, originalPlayerSpriteHitbox);
                typeof(Player).GetProperty("WeaponHitboxRotation")?.SetValue(player, originalWeaponRotation);

                // Restore animation freeze state
                Item.FreezeGameWorldAnimations = originalFreezeState;

                // Clear portrait rendering flag
                Item.IsRenderingPortrait = false;

                portraitBatch.End();
            }

            // Restore previous render target
            if (prevRenderTargets != null && prevRenderTargets.Length > 0 && prevRenderTargets[0].RenderTarget != null)
                graphicsDevice.SetRenderTarget(prevRenderTargets[0].RenderTarget as RenderTarget2D);
            else
                graphicsDevice.SetRenderTarget(null);
        }

        #endregion Portrait Rendering

        #region Nine Slice Box Drawing

        private void DrawNineSliceBox(SpriteBatch spriteBatch, Texture2D texture, Rectangle dest, int spriteSize, Color color, bool keepCenterClear = false)
        {
            // Draw all 9-slice regions as normal
            Rectangle srcTopLeft = new Rectangle(0, 0, spriteSize, spriteSize);
            Rectangle srcTop = new Rectangle(spriteSize, 0, 5, spriteSize);
            Rectangle srcTopRight = new Rectangle(spriteSize + 5, 0, spriteSize, spriteSize);
            Rectangle srcLeft = new Rectangle(0, spriteSize, spriteSize, 5);
            Rectangle srcCenter = new Rectangle(spriteSize, spriteSize, 5, 5);
            Rectangle srcRight = new Rectangle(spriteSize + 5, spriteSize, spriteSize, 5);
            Rectangle srcBottomLeft = new Rectangle(0, spriteSize + 5, spriteSize, spriteSize);
            Rectangle srcBottom = new Rectangle(spriteSize, spriteSize + 5, 5, spriteSize);
            Rectangle srcBottomRight = new Rectangle(spriteSize + 5, spriteSize + 5, spriteSize, spriteSize);

            int x = dest.X, y = dest.Y, w = dest.Width, h = dest.Height;
            int s = spriteSize;
            // Corners
            spriteBatch.Draw(texture, new Rectangle(x, y, s, s), srcTopLeft, color);
            spriteBatch.Draw(texture, new Rectangle(x + w - s, y, s, s), srcTopRight, color);
            spriteBatch.Draw(texture, new Rectangle(x, y + h - s, s, s), srcBottomLeft, color);
            spriteBatch.Draw(texture, new Rectangle(x + w - s, y + h - s, s, s), srcBottomRight, color);
            // Edges
            spriteBatch.Draw(texture, new Rectangle(x + s, y, w - 2 * s, s), srcTop, color);
            spriteBatch.Draw(texture, new Rectangle(x + s, y + h - s, w - 2 * s, s), srcBottom, color);
            spriteBatch.Draw(texture, new Rectangle(x, y + s, s, h - 2 * s), srcLeft, color);
            spriteBatch.Draw(texture, new Rectangle(x + w - s, y + s, s, h - 2 * s), srcRight, color);
            // Center
            Rectangle centerRect = new Rectangle(x + s, y + s, w - 2 * s, h - 2 * s);
            if (!keepCenterClear)
            {
                spriteBatch.Draw(texture, centerRect, srcCenter, color);
            }
            else
            {
                int gapWidth = (int)(centerRect.Width * 0.4); // 40% of center width
                int gapX = centerRect.X + (centerRect.Width - gapWidth) / 2;
                int gapY = centerRect.Y + (int)(centerRect.Height * 0.2f); // 22% from top
                int gapHeight = (int)(centerRect.Height * 0.56f); // 56% of center height

                // Fill area above the gap
                Rectangle aboveGapRect = new Rectangle(centerRect.X, centerRect.Y, centerRect.Width, gapY - centerRect.Y);
                spriteBatch.Draw(texture, aboveGapRect, srcCenter, color);

                // Fill area below the gap
                Rectangle belowGapRect = new Rectangle(centerRect.X, gapY + gapHeight, centerRect.Width, centerRect.Bottom - (gapY + gapHeight));
                spriteBatch.Draw(texture, belowGapRect, srcCenter, color);

                // Fill left area
                Rectangle leftRect = new Rectangle(centerRect.X, gapY, gapX - centerRect.X, gapHeight);
                spriteBatch.Draw(texture, leftRect, srcCenter, color);

                // Fill right area
                int rightX = gapX + gapWidth;
                Rectangle rightRect = new Rectangle(rightX, gapY, centerRect.Right - rightX, gapHeight);
                spriteBatch.Draw(texture, rightRect, srcCenter, color);

                // Middle vertical gap
                Rectangle gapRect = new Rectangle(gapX, gapY, gapWidth, gapHeight);
                if (statBoxOutlineTextureReversed != null && texture == statBoxTexture)
                {
                    DrawNineSliceBox(spriteBatch, statBoxOutlineTextureReversed, gapRect, spriteSize, Color.White, false);
                }
            }
        }

        #endregion Nine Slice Box Drawing
    }
}