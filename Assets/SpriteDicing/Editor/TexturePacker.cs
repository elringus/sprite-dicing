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
        private readonly int unitSize;
        private readonly int padding;
        private readonly int paddedUnitSize;
        private readonly int unitsPerAtlasLimit;

        public TexturePacker (ITextureSerializer serializer, float uvInset, bool square, int sizeLimit, int unitSize, int padding)
        {
            this.serializer = serializer;
            this.uvInset = uvInset;
            this.square = square;
            this.sizeLimit = sizeLimit;
            this.unitSize = unitSize;
            this.padding = padding;
            paddedUnitSize = unitSize + padding * 2;
            unitsPerAtlasLimit = Mathf.FloorToInt(Mathf.Pow(Mathf.FloorToInt(sizeLimit / (float)paddedUnitSize), 2));
        }

        public IReadOnlyList<AtlasTexture> Pack (IEnumerable<DicedTexture> dicedTextures)
        {
            var atlases = new List<AtlasTexture>();
            var texturesToPack = new HashSet<DicedTexture>(dicedTextures);
            while (texturesToPack.Count > 0)
                atlases.Add(PackToAtlas(texturesToPack));
            return atlases;
        }

        private AtlasTexture PackToAtlas (HashSet<DicedTexture> texturesToPack)
        {
            var packedTextures = new List<DicedTexture>();
            var packedContent = new HashSet<Hash128>();
            while (FindTextureToPack(texturesToPack, packedContent) is DicedTexture textureToPack)
            {
                texturesToPack.Remove(textureToPack);
                packedTextures.Add(textureToPack);
                packedContent.UnionWith(textureToPack.UniqueContent);
            }
            var contentToUV = MapContentToUV(packedTextures);
            var atlasTexture = BuildAtlasTexture(packedTextures, contentToUV);
            return new AtlasTexture(atlasTexture, contentToUV, packedTextures);
        }

        private DicedTexture FindTextureToPack (IReadOnlyCollection<DicedTexture> textures, HashSet<Hash128> packedContent)
        {
            var optimalTexture = default(DicedTexture);
            var minUnitsToPack = int.MaxValue;
            foreach (var texture in textures)
            {
                var unitsToPack = texture.UniqueContent.Count(c => !packedContent.Contains(c));
                if (unitsToPack >= minUnitsToPack) continue;
                optimalTexture = texture;
                minUnitsToPack = unitsToPack;
            }
            return packedContent.Count + minUnitsToPack <= unitsPerAtlasLimit ? optimalTexture : null;
        }

        private Dictionary<Hash128, Rect> MapContentToUV (IReadOnlyCollection<DicedTexture> packedTextures)
        {
            return new Dictionary<Hash128, Rect>();
        }

        private Texture2D BuildAtlasTexture (IReadOnlyCollection<DicedTexture> packedTextures, IReadOnlyDictionary<Hash128, Rect> contentToUV)
        {
            return null;
        }

        private static Texture2D CreateTexture (int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
