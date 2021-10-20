using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for dicing source textures.
    /// </summary>
    public class TextureDicer
    {
        private readonly int unitSize;
        private readonly int padding;
        private readonly List<DicedUnit> units = new List<DicedUnit>();

        private Texture2D texture;

        public TextureDicer (int unitSize, int padding)
        {
            if (unitSize < 1) throw new ArgumentException("Size should be greater than one.");
            if (padding < 0) throw new ArgumentException("Padding couldn't be negative.");

            this.unitSize = unitSize;
            this.padding = padding;
        }

        public DicedTexture Dice (SourceTexture source)
        {
            PrepareToDice(source);
            var unitCountX = Mathf.CeilToInt((float)texture.width / unitSize);
            var unitCountY = Mathf.CeilToInt((float)texture.height / unitSize);
            for (int unitX = 0; unitX < unitCountX; unitX++)
            for (int unitY = 0; unitY < unitCountY; unitY++)
                DiceAt(unitX * unitSize, unitY * unitSize);
            return new DicedTexture(source, units);
        }

        private void PrepareToDice (SourceTexture source)
        {
            units.Clear();
            texture = source.Texture;
        }

        private void DiceAt (int x, int y)
        {
            var pixelsRect = new RectInt(x, y, unitSize, unitSize);
            var pixels = GetPixels(pixelsRect);
            if (pixels.All(p => p.a == 0)) return;
            var paddedRect = PadRect(pixelsRect);
            var paddedPixels = GetPixels(paddedRect);
            var quadVerts = CropOverBorders(pixelsRect, x, y);
            var hash = GetHash(unitSize, pixels);
            units.Add(new DicedUnit(quadVerts, paddedPixels, hash));
        }

        /// <returns>Flattened 2D array, where pixels are laid out left to right, bottom to top.</returns>
        private Color[] GetPixels (RectInt rect)
        {
            // TODO: GetPixels() from texture and reuse the array.
            var endX = rect.x + rect.width;
            var endY = rect.y + rect.height;
            var colors = new Color[rect.width * rect.height];
            for (int y = rect.y, i = 0; y < endY; y++)
            for (int x = rect.x; x < endX; x++, i++)
                colors[i] = texture.GetPixel(x, y);
            return colors;
        }

        private RectInt PadRect (RectInt rect)
        {
            var delta = Vector2Int.one * padding;
            return new RectInt(rect.position - delta, rect.size + delta * 2);
        }

        private RectInt CropOverBorders (RectInt rect, int x, int y)
        {
            rect.width = Mathf.Min(rect.width, texture.width - x);
            rect.height = Mathf.Min(rect.height, texture.height - y);
            return rect;
        }

        private static Hash128 GetHash (int size, Color[] pixels)
        {
            // TODO: Find out how Unity builds image content hash and replicate.
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture.imageContentsHash;
        }
    }
}
