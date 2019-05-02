using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteDicing
{
    public static class Utilities
    {
        /// <summary>
        /// For 'source' rectangle inside 'target' rectangle, get the maximum scale factor 
        /// that permits the 'source' rectangle to be scaled without stretching or squashing.
        /// </summary>
        /// <param name="targetSize">Size of the rectangle to be scaled to.</param>
        /// <param name="sourceSize">Size of the rectangle to scale.</param>
        /// <returns>Maximum scale factor preserving aspect ratio.</returns>
        public static float MaxScaleKeepAspect (Vector2 targetSize, Vector2 sourceSize)
        {
            var targetAspect = targetSize.x / targetSize.y;
            var sourceAspect = sourceSize.x / sourceSize.y;

            if (targetAspect > sourceAspect)
                return targetSize.y / sourceSize.y;
            return targetSize.x / sourceSize.x;
        }

        public static int ToNearestEven (this int value, int upperLimit = int.MaxValue)
        {
            return (value % 2 == 0) ? value : Mathf.Min(value + 1, upperLimit);
        }

        /// <summary>
        /// Attempts to extract content before the specified match (on first occurence).
        /// </summary>
        public static string GetBefore (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var endIndex = content.IndexOf(matchString, comp);
                return content.Substring(0, endIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content before the specified match (on last occurence).
        /// </summary>
        public static string GetBeforeLast (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var endIndex = content.LastIndexOf(matchString, comp);
                return content.Substring(0, endIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content after the specified match (on last occurence).
        /// </summary>
        public static string GetAfter (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var startIndex = content.LastIndexOf(matchString, comp) + matchString.Length;
                if (content.Length <= startIndex) return string.Empty;
                return content.Substring(startIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content after the specified match (on first occurence).
        /// </summary>
        public static string GetAfterFirst (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var startIndex = content.IndexOf(matchString, comp) + matchString.Length;
                if (content.Length <= startIndex) return string.Empty;
                return content.Substring(startIndex);
            }
            else return null;
        }

        public static float ProgressOf<T> (this List<T> list, T currentItem)
        {
            return list.IndexOf(currentItem) / (float)list.Count;
        }

        public static bool Contains (this Rect rect, float x, float y)
        {
            return rect.Contains(new Vector2(x, y));
        }

        public static Rect Scale (this Rect rect, float scale)
        {
            return new Rect(rect.position * scale, rect.size * scale);
        }

        public static Rect Scale (this Rect rect, Vector2 scale)
        {
            return new Rect(new Vector2(rect.position.x * scale.x, rect.position.y * scale.y),
                new Vector2(rect.size.x * scale.x, rect.size.y * scale.y));
        }

        public static Rect Scale (this Rect rect, float scaleX, float scaleY)
        {
            return rect.Scale(new Vector2(scaleX, scaleY));
        }

        public static Rect Crop (this Rect rect, float cropAmount)
        {
            return new Rect(rect.position - Vector2.one * cropAmount, rect.size + Vector2.one * cropAmount * 2f);
        }

        /// <summary>
        /// Creates a texture.
        /// </summary>
        public static Texture2D CreateTexture (int width, int height, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            var texture = new Texture2D(width, height, textureFormat, mipmap, linear);
            texture.wrapMode = wrapMode;
            if (!string.IsNullOrEmpty(name))
                texture.name = name;
            return texture;
        }

        /// <summary>
        /// Creates a square texture.
        /// </summary>
        public static Texture2D CreateTexture (int size, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            return CreateTexture(size, size, wrapMode, textureFormat, mipmap, linear, name);
        }

        /// <summary>
        /// Creates a texture and fills it with provided colors.
        /// </summary>
        public static Texture2D CreateTexture (int width, int height, Color[] pixelColors, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            var texture = CreateTexture(width, height, wrapMode, textureFormat, mipmap, linear, name);
            texture.SetPixels(pixelColors);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a square texture and fills it with provided colors.
        /// </summary>
        public static Texture2D CreateTexture (int size, Color[] pixelColors, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            return CreateTexture(size, size, pixelColors, wrapMode, textureFormat, mipmap, linear, name);
        }

        /// <summary>
        /// Read pixels from the texture, filling overbound regions with the provided color.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="pixelsRect">Rect on the texture to read pixels from.</param>
        /// <param name="overboundColor">If rect is outside of texture bounds, overbound regions will be filled with this color.</param>
        /// <returns>Flattened 2D array, where pixels are laid out left to right, bottom to top.</returns>
        public static Color[] GetPixels (this Texture2D texture, Rect pixelsRect, Color overboundColor = default)
        {
            var startX = Mathf.FloorToInt(pixelsRect.x);
            var startY = Mathf.FloorToInt(pixelsRect.y);
            var rectWidth = Mathf.FloorToInt(pixelsRect.width);
            var rectHeight = Mathf.FloorToInt(pixelsRect.height);
            var endX = startX + rectWidth;
            var endY = startY + rectHeight;
            var colors = new Color[rectWidth * rectHeight];
            var colorsIndex = 0;
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (x > texture.width || y > texture.height || !pixelsRect.Contains(x, y))
                        colors[colorsIndex] = overboundColor;
                    else colors[colorsIndex] = texture.GetPixel(x, y);
                    colorsIndex++;
                }
            }
            return colors;
        }

        public static Texture2D ToTexture2D (this RenderTexture renderTexture)
        {
            var texture2d = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            texture2d.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2d.Apply();
            return texture2d;
        }

        public static int GetArrayHashCode<T> (this T[] array)
        {
            return ArrayEqualityComparer<T>.GetHashCode(array);
        }
    }

    /// <summary>
    /// Allows comparing arrays using equality comparer of the array items.
    /// Type of the array items should provide a valid comparer for this to work.
    /// Implementation based on: https://stackoverflow.com/a/7244729/1202251
    /// </summary>
    /// <typeparam name="T">Type of the items contained in the array.</typeparam>
    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private static readonly EqualityComparer<T> ITEMS_COMPARER = EqualityComparer<T>.Default;

        public static bool Equals (T[] first, T[] second)
        {
            if (first == second) return true;
            if (first == null || second == null) return false;
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (!ITEMS_COMPARER.Equals(first[i], second[i])) return false;
            return true;
        }

        public static int GetHashCode (T[] array)
        {
            unchecked
            {
                if (array == null) return 0;
                var hash = 17;
                foreach (T item in array)
                    hash = hash * 31 + ITEMS_COMPARER.GetHashCode(item);
                return hash;
            }
        }

        bool IEqualityComparer<T[]>.Equals (T[] first, T[] second)
        {
            return Equals(first, second);
        }

        int IEqualityComparer<T[]>.GetHashCode (T[] array)
        {
            return GetHashCode(array);
        }
    }
}
