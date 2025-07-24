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
        public bool IsOpen { get; set; }

        private const int SlotPixelSize = 120;

        private int? selectedItemSlot = null;
        private Texture2D statBoxTexture;
        private const int StatBoxSpriteSize = 45; // Each sprite is 45x45
        private Texture2D statIconsTexture;
        private const int StatIconSize = 30;

        // Scrollable info box state
        private RenderTarget2D infoBoxRenderTarget;

        private int infoBoxRenderTargetWidth;
        private int infoBoxRenderTargetHeight;
        private float infoBoxScrollOffset = 0f;
        private float infoBoxLastTouchY = 0f;
        private bool infoBoxIsTouching = false;
        private int lastSelectedItemSlot = -1;
        private Texture2D starTexture;
        private Texture2D starSmallTexture;
        private const int StarSmallIconSize = 33;
        private const int StarSmallGap = 4;
        private const int IconBoxPadding = 40;

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
            try { statIconsTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_Icons"); } catch { statIconsTexture = null; }
            try { starTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_Star"); } catch { starTexture = null; }
            try { starSmallTexture = content.Load<Texture2D>("Textures/UI/t_Inventory_StarSmall"); } catch { starSmallTexture = null; }
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

        public void Scroll(float deltaRows)
        {
            float maxOffsetRows = (TotalSlots - 1) / Columns - VisibleRows + 1;
            if (maxOffsetRows < 0) maxOffsetRows = 0;
            float newOffset = scrollOffset + deltaRows;
            if (newOffset > maxOffsetRows) newOffset = maxOffsetRows;
            if (newOffset < 0) newOffset = 0;
            scrollOffset = newOffset;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
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
                Item selected = slotIdx >= 0 && slotIdx < items.Count ? items[slotIdx] : null;
                if (selected != null)
                {
                    var font = Main.Font;
                    int statBoxStartX = (screenWidth - (slotWidth * Columns)) / 2;
                    int statBoxScrollbarWidth = 15;
                    int statBoxScrollbarX = statBoxStartX + slotWidth * Columns;
                    int infoRegionX = statBoxScrollbarX + statBoxScrollbarWidth;
                    int infoRegionY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                    int infoRegionHeight = slotHeight * VisibleRows;
                    // --- Layout calculation ---
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
                    int infoBoxWidth = iconBoxWidth;
                    int maxTextWidth = 0;
                    // --- Measure info content height/width (excluding icon box) ---
                    int measureY = 0;
                    // Name
                    if (!string.IsNullOrEmpty(selected.GetName()))
                    {
                        Vector2 nameSize = font.MeasureString(selected.GetName());
                        measureY += (int)nameSize.Y + 2;
                        if (nameSize.X > maxTextWidth) maxTextWidth = (int)nameSize.X;
                    }
                    // Rarity
                    var rarityInfo = Item.GetRarityInfo(selected.Rarity);
                    string rarityText = rarityInfo.Name;
                    if (!string.IsNullOrEmpty(rarityText))
                    {
                        Vector2 raritySize = font.MeasureString(rarityText);
                        measureY += (int)raritySize.Y + 20;
                        if (raritySize.X > maxTextWidth) maxTextWidth = (int)raritySize.X;
                    }
                    // Type
                    string typeText = selected.Type ?? "";
                    if (!string.IsNullOrEmpty(typeText))
                    {
                        Vector2 typeSize = font.MeasureString(typeText);
                        measureY += (int)typeSize.Y + 20;
                        if (typeSize.X > maxTextWidth) maxTextWidth = (int)typeSize.X;
                    }
                    measureY += 20;
                    // Lore
                    List<string> loreLines = new List<string>();
                    if (!string.IsNullOrEmpty(selected.Lore))
                    {
                        string lore = selected.Lore;
                        while (lore.Length > 0)
                        {
                            int len = Math.Min(30, lore.Length);
                            int cut = len;
                            if (len < lore.Length)
                            {
                                int lastSpace = lore.LastIndexOf(' ', len);
                                if (lastSpace > 0) cut = lastSpace;
                            }
                            loreLines.Add(lore.Substring(0, cut));
                            lore = lore.Substring(cut).TrimStart();
                        }
                        foreach (var line in loreLines)
                        {
                            Vector2 loreSize = font.MeasureString(line);
                            measureY += (int)loreSize.Y;
                            if (loreSize.X > maxTextWidth) maxTextWidth = (int)loreSize.X;
                        }
                        measureY += 20;
                    }
                    // Stats
                    List<string> statDisplay = new List<string>();
                    if (selected.Damage != 0) statDisplay.Add($"{selected.Damage} damage");
                    if (selected.Knockback != 0) statDisplay.Add($"{(selected.Knockback / 100f):.00} knockback");
                    if (selected.ShootSpeed != 0) statDisplay.Add($"{selected.ShootSpeed} range");
                    if (selected.UseTime != 0) statDisplay.Add($"{(1f / selected.UseTime):0.00} uses per second");
                    if (selected.Defense != 0) statDisplay.Add($"{selected.Defense} defense");
                    if (selected.KnockbackResistance != 0) statDisplay.Add($"{selected.KnockbackResistance * 100f}% knockback resistance");
                    if (selected.Value != 0) statDisplay.Add($"{selected.Value} gold");
                    foreach (var line in statDisplay)
                    {
                        Vector2 statSize = font.MeasureString(line);
                        measureY += (int)statSize.Y;
                        if (statSize.X > maxTextWidth) maxTextWidth = (int)statSize.X;
                    }
                    List<(string name, string effect, bool isPrefix)> affixes = new();
                    if (!string.IsNullOrEmpty(selected.Prefix) && ItemModifier.Prefixes.Values.FirstOrDefault(p => p.Name == selected.Prefix) is Prefix prefixObj)
                        affixes.Add((prefixObj.Name, prefixObj.Effect, true));
                    if (!string.IsNullOrEmpty(selected.Suffix) && ItemModifier.Suffixes.Values.FirstOrDefault(s => s.Name == selected.Suffix) is Suffix suffixObj)
                        affixes.Add((suffixObj.Name, suffixObj.Effect, false));
                    foreach (var (affixName, effect, isPrefix) in affixes)
                    {
                        measureY += 20;
                        Vector2 affixNameSize = font.MeasureString(affixName);
                        measureY += (int)affixNameSize.Y;
                        if (affixNameSize.X > maxTextWidth) maxTextWidth = (int)affixNameSize.X;
                        // Effect (wrap at 30 chars)
                        List<string> effectLines = new List<string>();
                        string effectLeft = effect;
                        while (effectLeft.Length > 0)
                        {
                            int len = Math.Min(30, effectLeft.Length);
                            int cut = len;
                            if (len < effectLeft.Length)
                            {
                                int lastSpace = effectLeft.LastIndexOf(' ', len);
                                if (lastSpace > 0) cut = lastSpace;
                            }
                            effectLines.Add(effectLeft.Substring(0, cut));
                            effectLeft = effectLeft.Substring(cut).TrimStart();
                        }
                        foreach (var line in effectLines)
                        {
                            Vector2 effSize = font.MeasureString(line);
                            measureY += (int)effSize.Y;
                            if (effSize.X > maxTextWidth) maxTextWidth = (int)effSize.X;
                        }
                    }
                    // Debug info
                    string debugLine = null;
                    if (Main.DebugMode)
                    {
                        debugLine = $"#DEBUG-ID: {selected.ID}, #DEBUG-PREFIX: {selected.Prefix}, #DEBUG-SUFFIX: {selected.Suffix}";
                        Vector2 dbgSize = font.MeasureString(debugLine);
                        measureY += (int)dbgSize.Y;
                        if (dbgSize.X > maxTextWidth) maxTextWidth = (int)dbgSize.X;
                    }
                    measureY += IconBoxPadding;
                    infoBoxWidth = Math.Max(infoBoxWidth, maxTextWidth + infoBoxPadding * 2);
                    // --- Icon box is fixed, info content is scrollable ---
                    int totalInfoBoxHeight = infoRegionHeight;
                    int infoContentBoxHeight = totalInfoBoxHeight - iconBoxHeight;
                    if (infoContentBoxHeight < 40) infoContentBoxHeight = 40;
                    int infoBoxRenderTargetHeightFinal = measureY + infoBoxPadding * 2;
                    // --- Render info content to RenderTarget2D ---
                    if (infoBoxRenderTarget == null || infoBoxRenderTarget.Width != infoBoxWidth || infoBoxRenderTarget.Height != infoBoxRenderTargetHeightFinal)
                    {
                        infoBoxRenderTarget?.Dispose();
                        infoBoxRenderTarget = new RenderTarget2D(spriteBatch.GraphicsDevice, infoBoxWidth, infoBoxRenderTargetHeightFinal);
                        infoBoxRenderTargetWidth = infoBoxWidth;
                        infoBoxRenderTargetHeight = infoBoxRenderTargetHeightFinal;
                    }
                    spriteBatch.GraphicsDevice.SetRenderTarget(infoBoxRenderTarget);
                    spriteBatch.GraphicsDevice.Clear(Color.Transparent);
                    using (SpriteBatch infoBatch = new SpriteBatch(spriteBatch.GraphicsDevice))
                    {
                        infoBatch.Begin();
                        int infoY = infoBoxPadding;
                        // Name (wrap)
                        if (!string.IsNullOrEmpty(selected.GetName()))
                        {
                            List<string> nameLines = new List<string>();
                            string nameLeft = selected.GetName();
                            while (nameLeft.Length > 0)
                            {
                                int len = Math.Min(30, nameLeft.Length);
                                int cut = len;
                                if (len < nameLeft.Length)
                                {
                                    int lastSpace = nameLeft.LastIndexOf(' ', len);
                                    if (lastSpace > 0) cut = lastSpace;
                                }
                                nameLines.Add(nameLeft.Substring(0, cut));
                                nameLeft = nameLeft.Substring(cut).TrimStart();
                            }
                            foreach (var nameLine in nameLines)
                            {
                                Vector2 nameSize2 = font.MeasureString(nameLine);
                                int nameX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)nameSize2.X) / 2;
                                font.DrawString(infoBatch, nameLine, new Vector2(nameX, infoY), rarityInfo.Color);
                                infoY += (int)nameSize2.Y;
                            }
                            infoY += 2;
                        }
                        // Type
                        if (!string.IsNullOrEmpty(typeText))
                        {
                            Vector2 typeSize2 = font.MeasureString(typeText);
                            int typeX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)typeSize2.X) / 2 + 20;
                            font.DrawString(infoBatch, typeText, new Vector2(typeX, infoY), Color.Yellow);
                            infoY += (int)typeSize2.Y;
                        }
                        infoY += 20;
                        int loreStartY = infoY;
                        int totalLoreHeight = 0;
                        foreach (var line in loreLines)
                            totalLoreHeight += (int)font.MeasureString(line).Y;
                        if (loreLines.Count > 0) totalLoreHeight += 2;
                        int loreYBottomAligned = loreStartY - totalLoreHeight;
                        int loreYAfterContent = infoY + 20; // 20px padding after last info line
                        int loreY = Math.Max(loreYBottomAligned, loreYAfterContent);
                        infoY = loreY;
                        for (int i = 0; i < loreLines.Count; i++)
                        {
                            var line = loreLines[i];
                            Vector2 lineSize = font.MeasureString(line);
                            if (i == loreLines.Count - 1)
                            {
                                int loreCenterX = infoBoxPadding + ((infoBoxWidth - infoBoxPadding * 2) - (int)lineSize.X) / 2;
                                font.DrawString(infoBatch, line, new Vector2(loreCenterX, infoY), Color.MediumAquamarine);
                            }
                            else
                            {
                                font.DrawString(infoBatch, line, new Vector2(infoBoxPadding, infoY), Color.MediumAquamarine);
                            }
                            infoY += (int)lineSize.Y;
                        }
                        infoY += 20;
                        // Stats
                        for (int i = 0; i < statDisplay.Count; i++)
                        {
                            string line = statDisplay[i];
                            int iconIndex = -1;
                            if (line.Contains("damage")) iconIndex = 0;
                            else if (line.Contains("knockback")) iconIndex = 1;
                            else if (line.Contains("range")) iconIndex = 2;
                            else if (line.Contains("uses per second")) iconIndex = 3;
                            else if (line.Contains("defense")) iconIndex = 4;
                            else if (line.Contains("knockback resistance")) iconIndex = 5;
                            else if (line.Contains("gold")) iconIndex = 6;
                            int statIconY = infoY;
                            if (statIconsTexture != null && iconIndex >= 0)
                            {
                                Rectangle statIconSrcRect = new Rectangle(0, iconIndex * StatIconSize, StatIconSize, StatIconSize);
                                Vector2 iconPos = new Vector2(infoBoxPadding, infoY + 2);
                                infoBatch.Draw(statIconsTexture, iconPos, statIconSrcRect, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            }
                            float textOffsetX = statIconsTexture != null && iconIndex >= 0 ? StatIconSize + 8 : 0;
                            font.DrawString(infoBatch, line, new Vector2(infoBoxPadding + textOffsetX, infoY), Color.White);
                            infoY += (int)font.MeasureString(line).Y;
                        }
                        // Prefix/suffix
                        foreach (var (affixName, effect, isPrefix) in affixes)
                        {
                            infoY += 20;
                            string affixLabel = isPrefix ? "[Prefix Bonus]" : "[Suffix Bonus]";
                            font.DrawString(infoBatch, affixLabel, new Vector2(infoBoxPadding, infoY), Color.Yellow);
                            infoY += (int)font.MeasureString(affixLabel).Y;
                            List<string> effectLines = new List<string>();
                            string effectLeft = effect;
                            while (effectLeft.Length > 0)
                            {
                                int len = Math.Min(30, effectLeft.Length);
                                int cut = len;
                                if (len < effectLeft.Length)
                                {
                                    int lastSpace = effectLeft.LastIndexOf(' ', len);
                                    if (lastSpace > 0) cut = lastSpace;
                                }
                                effectLines.Add(effectLeft.Substring(0, cut));
                                effectLeft = effectLeft.Substring(cut).TrimStart();
                            }
                            foreach (var line in effectLines)
                            {
                                font.DrawString(infoBatch, line, new Vector2(infoBoxPadding, infoY), Color.CornflowerBlue);
                                infoY += (int)font.MeasureString(line).Y;
                            }
                        }
                        // Debug info
                        if (Main.DebugMode && debugLine != null)
                        {
                            font.DrawString(infoBatch, debugLine, new Vector2(infoBoxPadding, infoY), Color.Red);
                            infoY += (int)font.MeasureString(debugLine).Y;
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
                            if (starSmallTexture != null)
                            {
                                int starCount = selected.Rarity + 1;
                                int totalStarsWidth = starCount * StarSmallIconSize + (starCount - 1) * StarSmallGap;
                                int starsStartX = infoRegionX + (infoBoxWidth - totalStarsWidth) / 2;
                                int starsY = rarityY + (int)raritySize.Y + 4; // 4px below rarity text
                                for (int i = 0; i < starCount; i++)
                                {
                                    int starX = starsStartX + i * (StarSmallIconSize + StarSmallGap);
                                    spriteBatch.Draw(starSmallTexture, new Rectangle(starX, starsY, StarSmallIconSize, StarSmallIconSize), Color.White);
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
                    int infoContentX = infoRegionX;
                    int infoContentY = infoRegionY + iconBoxHeight;
                    Rectangle infoContentDestRect = new Rectangle(infoContentX, infoContentY, infoBoxWidth, infoContentBoxHeight - IconBoxPadding);
                    Rectangle infoContentSrcRect = new Rectangle(0, (int)infoBoxScrollOffset, infoBoxWidth, infoContentBoxHeight - IconBoxPadding);
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

                    /*if (selected != null)
                    {
                        if (starTexture != null)
                        {
                            int starCount = selected.Rarity + 1;
                            int totalStarsWidth = starCount * StarIconSize + (starCount - 1) * StarGap;
                            int starsStartX = infoRegionX + (infoBoxWidth - totalStarsWidth) / 2;
                            int seamY = infoRegionY + iconBoxHeight; // seam between icon and info box
                            int centerY = seamY; // vertical center of stars aligns with seam
                            int starCenterY = centerY;
                            int starTopY = starCenterY - StarIconSize / 2;
                            for (int i = 0; i < starCount; i++)
                            {
                                int starX = starsStartX + i * (StarIconSize + StarGap);
                                spriteBatch.Draw(starTexture, new Rectangle(starX, starTopY, StarIconSize, StarIconSize), Color.White);
                            }
                        }
                    }*/
                }
            }
        }

        private int previousWheelValue = 0;

        public void Update(TouchCollection touches)
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
                        // Detect item selection (only if not in info box)
                        int startY = (screenHeight - (slotHeight * VisibleRows)) / 2;
                        int startX = (screenWidth - (slotWidth * Columns)) / 2;
                        int localX = (int)inputPos.X - startX;
                        int localY = (int)inputPos.Y - startY;
                        int col = localX / slotWidth;
                        int row = localY / slotHeight;
                        if (col >= 0 && col < Columns && row >= 0 && row < VisibleRows)
                        {
                            int slotIndex = ((int)scrollOffset + row) * Columns + col;
                            if (slotIndex >= 0 && slotIndex < TotalSlots)
                            {
                                if (GetItem(slotIndex) != null)
                                    selectedItemSlot = slotIndex;
                                else
                                    selectedItemSlot = null;
                            }
                        }
                        else
                        {
                            selectedItemSlot = null;
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

        private void DrawNineSliceBox(SpriteBatch spriteBatch, Texture2D texture, Rectangle dest, int spriteSize, Color color)
        {
            // New 9-slice sizes:
            // Corners: 45x45
            // Top/Bottom mid: 5x45
            // Left/Right mid: 45x5
            // Center: 5x5
            int corner = 45;
            int edgeShort = 5;
            // Source rectangles
            Rectangle srcTL = new Rectangle(0, 0, corner, corner);
            Rectangle srcTM = new Rectangle(corner, 0, edgeShort, corner);
            Rectangle srcTR = new Rectangle(corner + edgeShort, 0, corner, corner);
            Rectangle srcML = new Rectangle(0, corner, corner, edgeShort);
            Rectangle srcMM = new Rectangle(corner, corner, edgeShort, edgeShort);
            Rectangle srcMR = new Rectangle(corner + edgeShort, corner, corner, edgeShort);
            Rectangle srcBL = new Rectangle(0, corner + edgeShort, corner, corner);
            Rectangle srcBM = new Rectangle(corner, corner + edgeShort, edgeShort, corner);
            Rectangle srcBR = new Rectangle(corner + edgeShort, corner + edgeShort, corner, corner);

            int x0 = dest.X;
            int x1 = dest.X + corner;
            int x2 = dest.Right - corner;
            int y0 = dest.Y;
            int y1 = dest.Y + corner;
            int y2 = dest.Bottom - corner;

            // Corners
            spriteBatch.Draw(texture, new Rectangle(x0, y0, corner, corner), srcTL, color); // TL
            spriteBatch.Draw(texture, new Rectangle(x2, y0, corner, corner), srcTR, color); // TR
            spriteBatch.Draw(texture, new Rectangle(x0, y2, corner, corner), srcBL, color); // BL
            spriteBatch.Draw(texture, new Rectangle(x2, y2, corner, corner), srcBR, color); // BR
            // Edges
            spriteBatch.Draw(texture, new Rectangle(x1, y0, x2 - x1, corner), srcTM, color); // Top
            spriteBatch.Draw(texture, new Rectangle(x1, y2, x2 - x1, corner), srcBM, color); // Bottom
            spriteBatch.Draw(texture, new Rectangle(x0, y1, corner, y2 - y1), srcML, color); // Left
            spriteBatch.Draw(texture, new Rectangle(x2, y1, corner, y2 - y1), srcMR, color); // Right
            // Center
            spriteBatch.Draw(texture, new Rectangle(x1, y1, x2 - x1, y2 - y1), srcMM, color);
        }
    }
}