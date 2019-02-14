using UnityEngine;

namespace UnityCommon
{
    public static class TextureUtils
    {
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
    }
}
