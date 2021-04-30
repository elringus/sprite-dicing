using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for creating <see cref="DicedTexture"/> from <see cref="SourceTexture"/>.
    /// </summary>
    public class TextureDicer
    {
        public DicedTexture Dice (SourceTexture source, int unitSize, int padding, float ppu)
        {
            if (unitSize < 1) throw new ArgumentException("Size should be greater than one.");
            if (padding < 0) throw new ArgumentException("Padding couldn't be negative.");
            if (ppu <= 0) throw new ArgumentException("PPU should be greater than zero.");

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
                var quadVerts = ScaleRect(pixelsRect, ppu);
                var hash = GetHash(unitSize, pixels);
                var dicedUnit = new DicedUnit { QuadVerts = quadVerts, ContentHash = hash, PaddedPixels = paddedPixels };
                units.Add(dicedUnit);
            }

            return new DicedTexture(source, units);
        }

        /// <summary>
        /// Reads pixels inside the specified texture rect.
        /// </summary>
        /// <returns>Flattened 2D array, where pixels are laid out left to right, bottom to top.</returns>
        private static Color[] GetPixels (Texture2D texture, RectInt rect)
        {
            // TODO: GetPixels() from texture and reuse the array.
            var endX = rect.x + rect.width;
            var endY = rect.y + rect.height;
            var colors = new Color[rect.width * rect.height];
            for (int y = rect.x, i = 0; y < endY; y++)
            for (int x = rect.y; x < endX; x++, i++)
                colors[i] = texture.GetPixel(x, y);
            return colors;
        }

        private static RectInt PadRect (RectInt rect, int padding)
        {
            var delta = Vector2Int.one * padding;
            return new RectInt(rect.position - delta, rect.size + delta * 2);
        }

        private static Rect ScaleRect (RectInt rect, float ppu)
        {
            var scale = 1f / ppu;
            return new Rect((Vector2)rect.position * scale, (Vector2)rect.size * scale);
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
