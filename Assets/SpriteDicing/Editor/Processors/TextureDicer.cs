using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
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

        private int sourceWidth, sourceHeight;
        private Color32[] sourcePixels;

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
            var unitCountX = Mathf.CeilToInt((float)sourceWidth / unitSize);
            var unitCountY = Mathf.CeilToInt((float)sourceHeight / unitSize);
            for (int unitX = 0; unitX < unitCountX; unitX++)
            for (int unitY = 0; unitY < unitCountY; unitY++)
                DiceAt(unitX * unitSize, unitY * unitSize);
            return new DicedTexture(source, units);
        }

        private void PrepareToDice (SourceTexture source)
        {
            units.Clear();
            sourceWidth = source.Texture.width;
            sourceHeight = source.Texture.height;
            sourcePixels = source.Texture.GetPixels32();
        }

        private void DiceAt (int x, int y)
        {
            var rect = new RectInt(x, y, unitSize, unitSize);
            var pixels = GetSourcePixels(rect);
            if (AreAllPixelsTransparent(pixels)) return;
            var paddedRect = PadRect(rect);
            var paddedPixels = GetSourcePixels(paddedRect);
            var quadVerts = CropOverBorders(rect, x, y);
            var hash = GetHash(pixels);
            units.Add(new DicedUnit(quadVerts, paddedPixels, hash));
        }

        private Color32[] GetSourcePixels (RectInt rect)
        {
            var endX = rect.x + rect.width;
            var endY = rect.y + rect.height;
            var pixels = new Color32[rect.width * rect.height];
            for (int y = rect.y, i = 0; y < endY; y++)
            for (int x = rect.x; x < endX; x++, i++)
                pixels[i] = GetSourcePixel(x, y);
            return pixels;
        }

        private Color32 GetSourcePixel (int x, int y)
        {
            if (x < 0) x = 0;
            else if (x >= sourceWidth) x = sourceWidth - 1;
            if (y < 0) y = 0;
            else if (y >= sourceHeight) y = sourceHeight - 1;
            return sourcePixels[x + sourceWidth * y];
        }

        private RectInt PadRect (RectInt rect)
        {
            var delta = Vector2Int.one * padding;
            return new RectInt(rect.position - delta, rect.size + delta * 2);
        }

        private RectInt CropOverBorders (RectInt rect, int x, int y)
        {
            rect.width = Mathf.Min(rect.width, sourceWidth - x);
            rect.height = Mathf.Min(rect.height, sourceHeight - y);
            return rect;
        }

        private static bool AreAllPixelsTransparent (Color32[] pixels)
        {
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].a > 0)
                    return false;
            return true;
        }

        private static unsafe Hash128 GetHash (Color32[] pixels)
        {
            var hash = new Hash128();
            fixed (byte* data = &pixels[0].r)
            {
                var dataSize = (ulong)pixels.Length * 4;
                var hashPtr = (Hash128*)UnsafeUtility.AddressOf(ref hash);
                HashUnsafeUtilities.ComputeHash128(data, dataSize, hashPtr);
            }
            return hash;
        }
    }
}
