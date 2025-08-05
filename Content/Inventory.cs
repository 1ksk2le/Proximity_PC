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
        public const int Columns = 5;
        public const int Rows = 20;
        public const int TotalSlots = Columns * Rows;
        public const int VisibleRows = 5;
        public const int VisibleSlots = Columns * VisibleRows;

        private readonly List<Item> items;
        private float scrollOffset;
        private float lastTouchY;
        private bool isTouching;
        private readonly Texture2D slotTexture;
        private readonly Texture2D thumbTexture;
        private readonly int slotWidth;
        private readonly int slotHeight;
        private readonly int screenWidth;
        private readonly int screenHeight;
        private RenderTarget2D inventoryRenderTarget;
        private readonly int renderTargetWidth;
        private readonly int renderTargetHeight;
        private bool isOpen;

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

        private const int SlotPixelSize = 120;

        private int? selectedItemSlot = null;
        private Texture2D statBoxTexture;
        private Texture2D statBoxOutlineTexture;
        private Texture2D statBoxOutlineTextureReversed;
        private const int StatBoxSpriteSize = 45;
        private Texture2D statIconsTexture;
        private const int StatIconSize = 30;

        // Scrollable info box state
        private RenderTarget2D infoBoxRenderTarget;

        private float infoBoxScrollOffset = 0f;
        private float infoBoxLastTouchY = 0f;
        private bool infoBoxIsTouching = false;
        private int lastSelectedItemSlot = -1;
        private Texture2D starTexture;
        private const int StarSmallIconSize = 33;
        private const int StarSmallGap = 4;
        private const int IconBoxPadding = 40;

        // Player portrait render target
        private RenderTarget2D playerPortraitRenderTarget;

        private const int PortraitSize = 400;

        public Inventory(ContentManager content, int screenWidth, int screenHeight)
        {
            items = new List<Item>(TotalSlots);
            for (int i = 0; i < TotalSlots; i++)
                items.Add(null);
            slotTexture = content.Load<Texture2D>("Textures/UI/t_Inventory");
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            slotWidth = SlotPixelSize;
            slotHeight = SlotPixelSize;
            scrollOffset = 0f;
            lastTouchY = 0f;
            isTouching = false;
            renderTargetWidth = slotWidth * Columns;
            renderTargetHeight = slotHeight * (VisibleRows + 2);
            try { thumbTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_Thumb"); } catch { thumbTexture = slotTexture; }
            try { statBoxTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_Box"); } catch { statBoxTexture = null; }
            try { statBoxOutlineTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_Box_Outline"); } catch { statBoxOutlineTexture = null; }
            try { statBoxOutlineTextureReversed = content.Load<Texture2D>("Textures/UI/t_Inventory_Box_Outline_Reversed"); } catch { statBoxOutlineTexture = null; }
            try { statIconsTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_Icons"); } catch { statIconsTexture = null; }
            try { starTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_StarSmall"); } catch { starTexture = null; }
        }

        private void EnsureRenderTarget(GraphicsDevice graphicsDevice)
        {
            if (inventoryRenderTarget == null ||
                inventoryRenderTarget.Width != renderTargetWidth ||
                inventoryRenderTarget.Height != renderTargetHeight)
            {
                inventoryRenderTarget?.Dispose();
                inventoryRenderTarget = new RenderTarget2D(
                    graphicsDevice,
                    renderTargetWidth,
                    renderTargetHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None
                );
            }
        }

        private void EnsurePlayerPortraitRenderTarget(GraphicsDevice graphicsDevice)
        {
            if (playerPortraitRenderTarget == null ||
                playerPortraitRenderTarget.Width != PortraitSize ||
                playerPortraitRenderTarget.Height != PortraitSize)
            {
                playerPortraitRenderTarget?.Dispose();
                playerPortraitRenderTarget = new RenderTarget2D(
                    graphicsDevice,
                    PortraitSize,
                    PortraitSize,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None
                );
            }
        }

        public void Scroll(float deltaRows)
        {
            float maxOffsetRows = (TotalSlots - 1) / Columns - VisibleRows + 1;
            if (maxOffsetRows < 0) maxOffsetRows = 0;
            float newOffset = scrollOffset + deltaRows;
            if (newOffset > maxOffsetRows) newOffset = maxOffsetRows;
            if (newOffset < 0) newOffset = 0;
            scrollOffset = newOffset;
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime, Player player)
        {
            Item.FreezeGameWorldAnimations = IsOpen;

            if (!IsOpen) return;
            EnsureRenderTarget(spriteBatch.GraphicsDevice);

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
                        if (item != null)
                        {
                            var rarityInfo = Item.GetRarityInfo(item.Rarity);
                            slotColor = rarityInfo.Color;
                        }
                        int rarity = item != null ? item.Rarity : 0;
                        if (rarity < 0 || rarity > 6) rarity = 0;
                        Rectangle srcRect = new Rectangle(0, rarity * SlotPixelSize, SlotPixelSize, SlotPixelSize);
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
                            rtBatch.Draw(tex, drawRectShadow, srcRectSwordStaff, Color.Black * 0.3f);
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
                    selected = player.EquippedItems[EquipmentSlot.Weapon];
                }
                else if (slotIdx == -101)
                {
                    selected = player.EquippedItems[EquipmentSlot.Helmet];
                }
                else if (slotIdx == -102)
                {
                    selected = player.EquippedItems[EquipmentSlot.Offhand];
                }
                else if (slotIdx == -103)
                {
                    selected = player.EquippedItems[EquipmentSlot.Chestplate];
                }
                if (selected != null)
                {
                    var font = Main.Font;
                    int statBoxStartX = (screenWidth - (slotWidth * Columns)) / 2;
                    int statBoxScrollbarWidth = 15;
                    int statBoxScrollbarX = statBoxStartX + slotWidth * Columns;
                    int infoRegionX = statBoxScrollbarX + statBoxScrollbarWidth;
                    int infoRegionY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                    int infoRegionHeight = slotHeight * VisibleRows;
                    int iconBoxPadding = 35;
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
                    if (selected.ShootSpeed != 0) statDisplay.Add($"{selected.ShootSpeed} range");
                    if (selected.UseTime != 0) statDisplay.Add($"{(1f / selected.UseTime):0.00} uses per second");
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

                    // --- Measure total height ---
                    int measureY = 0;
                    foreach (var line in nameLines) measureY += (int)font.MeasureString(line).Y;
                    if (nameLines.Count > 0) measureY += 2;
                    // Rarity text is not rendered in the info box, so do not measure its height
                    foreach (var line in typeLines) measureY += (int)font.MeasureString(line).Y;
                    if (typeLines.Count > 0) measureY += 30;
                    measureY += 30;
                    foreach (var line in loreLines) measureY += (int)font.MeasureString(line).Y;
                    if (loreLines.Count > 0) measureY += 30;
                    foreach (var stat in statLines) foreach (var line in stat) measureY += (int)font.MeasureString(line).Y;
                    // Add item.Info lines to measureY
                    if (!string.IsNullOrEmpty(selected.Info))
                    {
                        var infoLines = WrapText(selected.Info, infoBoxWidth - infoBoxPadding * 2);
                        foreach (var line in infoLines) measureY += (int)font.MeasureString(line).Y;
                    }
                    foreach (var affix in affixLines)
                    {
                        measureY += 30;
                        foreach (var line in affix.name) measureY += (int)font.MeasureString(line).Y;
                        foreach (var line in affix.effect) measureY += (int)font.MeasureString(line).Y;
                    }
                    foreach (var line in debugLines) measureY += (int)font.MeasureString(line).Y;
                    measureY += IconBoxPadding;
                    // --- Icon box is fixed, info content is scrollable ---
                    // Dynamically set info box height based on screen size
                    int totalInfoBoxHeight = Math.Min(infoRegionHeight, screenHeight - infoRegionY - 40); // 40px margin at bottom
                    int infoContentBoxHeight = totalInfoBoxHeight - iconBoxHeight;
                    if (infoContentBoxHeight < 40) infoContentBoxHeight = 40;
                    int infoBoxRenderTargetHeightFinal = measureY + infoBoxPadding * 2;
                    // --- Add 20px gap after last text line ---
                    int infoBoxRenderTargetHeightFinalWithGap = infoBoxRenderTargetHeightFinal;
                    if (infoBoxRenderTarget == null || infoBoxRenderTarget.Width != infoBoxWidth || infoBoxRenderTarget.Height != infoBoxRenderTargetHeightFinalWithGap)
                    {
                        infoBoxRenderTarget?.Dispose();
                        infoBoxRenderTarget = new RenderTarget2D(spriteBatch.GraphicsDevice, infoBoxWidth, infoBoxRenderTargetHeightFinalWithGap);
                    }
                    spriteBatch.GraphicsDevice.SetRenderTarget(infoBoxRenderTarget);
                    spriteBatch.GraphicsDevice.Clear(Color.Transparent);
                    using (SpriteBatch infoBatch = new SpriteBatch(spriteBatch.GraphicsDevice))
                    {
                        infoBatch.Begin();
                        int infoY = infoBoxPadding;
                        // Name (centered)
                        foreach (var nameLine in nameLines)
                        {
                            Vector2 nameSize = font.MeasureString(nameLine);
                            int nameX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)nameSize.X) / 2;
                            font.DrawString(infoBatch, nameLine, new Vector2(nameX, infoY), rarityInfo.Color);
                            infoY += (int)nameSize.Y;
                        }
                        if (nameLines.Count > 0) infoY += 2;
                        // Type (centered)
                        foreach (var typeLine in typeLines)
                        {
                            Vector2 typeSize = font.MeasureString(typeLine);
                            int typeX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)typeSize.X) / 2;
                            font.DrawString(infoBatch, typeLine, new Vector2(typeX, infoY), Color.Yellow);
                            infoY += (int)typeSize.Y;
                        }
                        if (typeLines.Count > 0) infoY += 10;
                        infoY += 10;
                        // Lore (centered)
                        foreach (var loreLine in loreLines)
                        {
                            Vector2 loreSize = font.MeasureString(loreLine);
                            int loreX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)loreSize.X) / 2;
                            font.DrawString(infoBatch, loreLine, new Vector2(loreX, infoY), Color.MediumAquamarine);
                            infoY += (int)loreSize.Y;
                        }
                        if (loreLines.Count > 0) infoY += 30;
                        // Stats (left-aligned, with icons)
                        for (int i = 0; i < statLines.Count; i++)
                        {
                            int iconIndex = -1;
                            string statRaw = statDisplay.Count > i ? statDisplay[i] : "";
                            if (statRaw.Contains("damage")) iconIndex = 0;
                            else if (statRaw.Contains("knockback")) iconIndex = 1;
                            else if (statRaw.Contains("range")) iconIndex = 2;
                            else if (statRaw.Contains("uses per second")) iconIndex = 3;
                            else if (statRaw.Contains("defense")) iconIndex = 4;
                            else if (statRaw.Contains("knockback resistance")) iconIndex = 5;
                            else if (statRaw.Contains("gold")) iconIndex = 6;
                            foreach (var statLine in statLines[i])
                            {
                                if (statIconsTexture != null && iconIndex >= 0)
                                {
                                    Rectangle statIconSrcRect = new Rectangle(0, iconIndex * StatIconSize, StatIconSize, StatIconSize);
                                    Vector2 iconPos = new Vector2(infoBoxPadding, infoY + 2);
                                    infoBatch.Draw(statIconsTexture, iconPos, statIconSrcRect, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                                }
                                float textOffsetX = statIconsTexture != null && iconIndex >= 0 ? StatIconSize + 8 : 0;
                                font.DrawString(infoBatch, statLine, new Vector2(infoBoxPadding + textOffsetX, infoY), Color.White);
                                infoY += (int)font.MeasureString(statLine).Y;
                            }
                        }
                        // Info text (left-aligned, CornflowerBlue)
                        // Info text (left-aligned, with icon, color: White)
                        if (!string.IsNullOrEmpty(selected.Info))
                        {
                            var infoLines = WrapText(selected.Info, infoBoxWidth - infoBoxPadding * 2);
                            int infoIconIndex = 7; // Last frame in statIconsTexture for item.Info
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
                                font.DrawString(infoBatch, infoLines[i], new Vector2(infoBoxPadding + textOffsetX, infoY), Color.White);
                                infoY += (int)font.MeasureString(infoLines[i]).Y;
                            }
                        }
                        // Prefix/suffix (left-aligned)
                        foreach (var affix in affixLines)
                        {
                            infoY += 30;
                            string affixLabel = affix.isPrefix ? "[Prefix Bonus]" : "[Suffix Bonus]";
                            font.DrawString(infoBatch, affixLabel, new Vector2(infoBoxPadding, infoY), Color.Yellow);
                            infoY += (int)font.MeasureString(affixLabel).Y;
                            foreach (var line in affix.effect)
                            {
                                font.DrawString(infoBatch, line, new Vector2(infoBoxPadding, infoY), Color.CornflowerBlue);
                                infoY += (int)font.MeasureString(line).Y;
                            }
                        }
                        // Debug info (left-aligned)
                        foreach (var dbgLine in debugLines)
                        {
                            font.DrawString(infoBatch, dbgLine, new Vector2(infoBoxPadding, infoY), Color.Red);
                            infoY += (int)font.MeasureString(dbgLine).Y;
                        }
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
                                int starCount = selected.Rarity + 1;
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
                        int iconY = infoRegionY + (iconBoxHeight - drawHeight) / 2 + IconBoxPadding; // Move item down by 20px
                        Vector2 origin = new Vector2(texW / 2f, texH / 2f);
                        Vector2 drawPos = new Vector2(iconX + drawWidth / 2f, iconY + drawHeight / 2f);
                        spriteBatch.Draw(selected.Texture, drawPos + new Vector2(0, 15), null, Color.Black * 0.35f, angle, origin, scale * 1.1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(selected.Texture, drawPos, null, Color.White, angle, origin, scale, SpriteEffects.None, 0f);
                        iconBoxHeight += IconBoxPadding;
                    }
                    // --- Draw info content (scrollable) ---
                    // Clamp info box X so it never exceeds screen borders
                    int infoBoxScreenX = Math.Max(marginX, Math.Min(screenWidth - infoBoxWidth - marginX, infoRegionX));
                    int infoContentX = infoBoxScreenX;
                    int infoContentY = infoRegionY + iconBoxHeight;
                    // Cap info content box height to fit on screen (Y axis untouched)
                    int cappedInfoContentBoxHeight = infoContentBoxHeight;
                    Rectangle infoContentDestRect = new Rectangle(infoContentX, infoContentY, infoBoxWidth, cappedInfoContentBoxHeight - IconBoxPadding);
                    Rectangle infoContentSrcRect = new Rectangle(0, (int)infoBoxScrollOffset, infoBoxWidth, cappedInfoContentBoxHeight - IconBoxPadding);
                    if (statBoxTexture != null)
                    {
                        DrawNineSliceBox(spriteBatch, statBoxTexture, infoContentDestRect, StatBoxSpriteSize, Color.White);
                    }
                    spriteBatch.Draw(infoBoxRenderTarget, infoContentDestRect, infoContentSrcRect, Color.White);
                    // --- Draw scrollbar for info content if needed ---
                    if (infoBoxRenderTarget.Height > infoContentBoxHeight)
                    {
                        int infoScrollbarWidth = 15;
                        int infoScrollbarX = infoContentDestRect.Right;
                        int infoScrollbarY = infoContentDestRect.Y;
                        int infoScrollbarHeight = infoContentDestRect.Height;
                        float infoVisibleRatio = (float)infoContentBoxHeight / infoBoxRenderTarget.Height;
                        float infoThumbHeight = infoScrollbarHeight * infoVisibleRatio;
                        float infoMaxScroll = infoBoxRenderTarget.Height - infoContentBoxHeight;
                        float infoThumbY = infoScrollbarY + (infoBoxScrollOffset / (infoMaxScroll > 0 ? infoMaxScroll : 1)) * (infoScrollbarHeight - infoThumbHeight);
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
                }
            }
            int equipmentBoxWidth = 570;
            int equipmentBoxHeight = slotHeight * VisibleRows;
            int equipmentBoxX = startX - equipmentBoxWidth; // 30px gap to the left of inventory
            int equipmentBoxY = startY;
            Rectangle equipmentBoxRect = new Rectangle(equipmentBoxX, equipmentBoxY, equipmentBoxWidth, equipmentBoxHeight);
            if (statBoxOutlineTexture != null)
            {
                DrawNineSliceBox(spriteBatch, statBoxTexture, equipmentBoxRect, StatBoxSpriteSize, Color.White, false);
                int eqSlotSize = 120;
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
                // Helper for slot rendering
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
                            if (i == 0 && player.EquippedItems[EquipmentSlot.Weapon] != null) equipped = player.EquippedItems[EquipmentSlot.Weapon];
                            else if (i == 1 && player.EquippedItems[EquipmentSlot.Helmet] != null) equipped = player.EquippedItems[EquipmentSlot.Helmet];
                        }
                        else
                        {
                            if (i == 0 && player.EquippedItems[EquipmentSlot.Offhand] != null) equipped = player.EquippedItems[EquipmentSlot.Offhand];
                            else if (i == 1 && player.EquippedItems[EquipmentSlot.Chestplate] != null) equipped = player.EquippedItems[EquipmentSlot.Chestplate];
                        }
                        int rarity = equipped != null ? equipped.Rarity : 0;
                        if (rarity < 0 || rarity > 6) rarity = 0;
                        var rarityInfo = equipped != null ? Item.GetRarityInfo(equipped.Rarity) : Item.GetRarityInfo(0);
                        Color slotColor = rarityInfo.Color;
                        Rectangle eqSlotSrcRect = new Rectangle(0, rarity * eqSlotSize, eqSlotSize, eqSlotSize);
                        Rectangle eqSlotRect = new Rectangle(slotX, slotY, eqSlotSize, eqSlotSize);
                        spriteBatch.Draw(slotTexture, eqSlotRect, eqSlotSrcRect, slotColor * 0.85f);
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
            }
        }

        private int previousWheelValue = 0;

        public void Update(TouchCollection touches, Player player)
        {
            if (!IsOpen) return;
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
                    Item selected = slotIdx >= 0 && slotIdx < items.Count ? items[slotIdx] : null;
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
                    float maxScroll = Math.Max(0, infoBoxRenderTarget != null ? infoBoxRenderTarget.Height - infoContentBoxHeight : 0);
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
                    Item selected = slotIdx >= 0 && slotIdx < items.Count ? items[slotIdx] : null;
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
                                if (i == 0 && player.EquippedItems[EquipmentSlot.Weapon] != null)
                                    selectedItemSlot = -100;
                                else if (i == 1 && player.EquippedItems[EquipmentSlot.Helmet] != null)
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
                                    if (i == 0 && player.EquippedItems[EquipmentSlot.Offhand] != null)
                                        selectedItemSlot = -102;
                                    else if (i == 1 && player.EquippedItems[EquipmentSlot.Chestplate] != null)
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
                        float maxScroll = Math.Max(0, infoBoxRenderTarget != null ? infoBoxRenderTarget.Height - infoContentBoxHeight : 0);
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
            }
        }

        public Item GetItem(int slot) => (slot >= 0 && slot < TotalSlots) ? items[slot] : null;

        public void SetItem(int slot, Item item)
        { if (slot >= 0 && slot < TotalSlots) items[slot] = item; }

        public int SlotWidth => slotWidth;

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

        private static DateTime portraitStartTime = DateTime.Now;

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
                Vector2 portraitCenter = new Vector2(PortraitSize / 2 + 30, PortraitSize / 2 + 120);
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

                // Don't update hitboxes during portrait rendering to prevent world hitbox corruption

                // Disable head rotation by clearing movement and attack directions
                var movementDirectionField = typeof(Player).GetField("MovementDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var attackDirectionField = typeof(Player).GetField("attackDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Vector2 originalMovementDirection = (Vector2)(movementDirectionField?.GetValue(player) ?? Vector2.Zero);
                Vector2 originalAttackDirection = (Vector2)(attackDirectionField?.GetValue(player) ?? Vector2.Zero);
                movementDirectionField?.SetValue(player, Vector2.Zero);
                attackDirectionField?.SetValue(player, Vector2.Zero);

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
    }
}