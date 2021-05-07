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

        public TextureDicer (int unitSize, int padding)
        {
            if (unitSize < 1) throw new ArgumentException("Size should be greater than one.");
            if (padding < 0) throw new ArgumentException("Padding couldn't be negative.");

            this.unitSize = unitSize;
            this.padding = padding;
        }

        public DicedTexture Dice (SourceTexture source)
        {
            var texture = source.Texture;
            var units = new List<DicedUnit>();
            var unitCountX = Mathf.CeilToInt((float)texture.width / unitSize);
            var unitCountY = Mathf.CeilToInt((float)texture.height / unitSize);

            for (int unitX = 0; unitX < unitCountX; unitX++)
            for (int unitY = 0; unitY < unitCountY; unitY++)
            {
                var x = unitX * unitSize;
                var y = unitY * unitSize;
                var pixelsRect = new RectInt(x, y, unitSize, unitSize);
                var pixels = GetPixels(texture, pixelsRect);
                if (pixels.All(p => p.a == 0)) continue;
                var paddedRect = PadRect(pixelsRect, padding);
                var paddedPixels = GetPixels(texture, paddedRect);
                var quadVerts = CropOverBorders(pixelsRect, x, y, texture);
                var hash = GetHash(unitSize, pixels);
                var dicedUnit = new DicedUnit(quadVerts, paddedPixels, hash);
                units.Add(dicedUnit);
            }

            return new DicedTexture(source, units);
        }

        /// <returns>Flattened 2D array, where pixels are laid out left to right, bottom to top.</returns>
        private static Color[] GetPixels (Texture2D texture, RectInt rect)
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

        private static RectInt PadRect (RectInt rect, int padding)
        {
            var delta = Vector2Int.one * padding;
            return new RectInt(rect.position - delta, rect.size + delta * 2);
        }

        private static RectInt CropOverBorders (RectInt rect, int x, int y, Texture texture)
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
