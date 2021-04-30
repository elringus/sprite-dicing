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
                var pixelsRect = new Rect(x, y, unitSize, unitSize);
                var pixels = GetPixels(texture, pixelsRect);
                if (pixels.All(p => p.a == 0)) continue;
                var paddedRect = pixelsRect.Crop(padding);
                var paddedPixels = GetPixels(texture, paddedRect);
                var quadVerts = pixelsRect.Scale(1f / ppu);
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
        private static Color[] GetPixels (Texture2D texture, Rect rect)
        {
            // TODO: GetPixels() from texture and reuse the array.
            var startX = Mathf.FloorToInt(rect.x);
            var startY = Mathf.FloorToInt(rect.y);
            var rectWidth = Mathf.FloorToInt(rect.width);
            var rectHeight = Mathf.FloorToInt(rect.height);
            var endX = startX + rectWidth;
            var endY = startY + rectHeight;
            var colors = new Color[rectWidth * rectHeight];
            for (int y = startY, colorsIndex = 0; y < endY; y++)
            for (int x = startX; x < endX; x++, colorsIndex++)
                colors[colorsIndex] = texture.GetPixel(x, y);
            return colors;
        }

        private static bool IsWithinTexture (int x, int y, Texture texture)
        {
            if (x < 0 || y < 0) return false;
            return x < texture.width && y < texture.height;
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
