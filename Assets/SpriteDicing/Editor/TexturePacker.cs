using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for packing diced textures into atlas textures.
    /// </summary>
    public class TexturePacker
    {
        private readonly ITextureSerializer serializer;
        private readonly float uvInset;
        private readonly bool square;
        private readonly int sizeLimit;

        public TexturePacker (ITextureSerializer serializer, float uvInset, bool square, int sizeLimit)
        {
            this.serializer = serializer;
            this.uvInset = uvInset;
            this.square = square;
            this.sizeLimit = sizeLimit;
        }

        public IReadOnlyList<AtlasTexture> Pack (IReadOnlyCollection<DicedTexture> dicedTextures)
        {
            if (dicedTextures is null || dicedTextures.Count == 0)
                throw new ArgumentException("At least one diced texture is required to build atlas.");
            
            var atlases = new List<AtlasTexture>();
            var unitSize = Mathf.Sqrt(dicedTextures.First().Units.First().PaddedPixels.Length);
            var uniqueUnits = dicedTextures.SelectMany(d => d.Units.Distinct());            
            
            return atlases;
        }

        private static Texture2D CreateAtlasTexture (int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
