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
        private readonly int unitSize;
        private readonly int padding;
        private readonly int paddedUnitSize;
        private readonly int unitsPerAtlasLimit;

        public TexturePacker (ITextureSerializer serializer, float uvInset, bool square, int sizeLimit, int unitSize, int padding)
        {
            this.serializer = serializer;
            this.uvInset = uvInset;
            this.square = square;
            this.unitSize = unitSize;
            this.padding = padding;
            paddedUnitSize = unitSize + padding * 2;
            unitsPerAtlasLimit = Mathf.FloorToInt(Mathf.Pow(Mathf.FloorToInt(sizeLimit / (float)paddedUnitSize), 2));
        }

        public List<AtlasTexture> Pack (IEnumerable<DicedTexture> dicedTextures)
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
            var packedUnits = new HashSet<DicedUnit>();
            while (FindTextureToPack(texturesToPack, packedUnits) is DicedTexture textureToPack)
            {
                texturesToPack.Remove(textureToPack);
                packedTextures.Add(textureToPack);
                packedUnits.UnionWith(textureToPack.Units);
            }
            var atlasTexture = CreateAtlasTexture(packedUnits.Count);
            var contentToUV = MapContent(packedUnits, atlasTexture);
            return new AtlasTexture(atlasTexture, contentToUV, packedTextures);
        }

        private DicedTexture FindTextureToPack (IReadOnlyCollection<DicedTexture> texturesToPack, HashSet<DicedUnit> packedUnits)
        {
            var optimalTexture = default(DicedTexture);
            var minUnitsToPack = int.MaxValue;
            foreach (var texture in texturesToPack)
            {
                var unitsToPack = texture.UniqueUnits.Count(c => !packedUnits.Contains(c));
                if (unitsToPack >= minUnitsToPack) continue;
                optimalTexture = texture;
                minUnitsToPack = unitsToPack;
            }
            return packedUnits.Count + minUnitsToPack <= unitsPerAtlasLimit ? optimalTexture : null;
        }

        private Texture2D CreateAtlasTexture (int unitsCount)
        {
            var size = Mathf.Sqrt(unitsCount * paddedUnitSize);
            var width = Mathf.CeilToInt(size);
            var height = square ? width : Mathf.FloorToInt(size);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return serializer.Serialize(texture);
        }

        private Dictionary<Hash128, Rect> MapContent (HashSet<DicedUnit> packedUnits, Texture2D atlasTexture)
        {
            var contentToUV = new Dictionary<Hash128, Rect>();
            var atlasSize = new Vector2(atlasTexture.width, atlasTexture.height);
            var unitsPerRow = atlasTexture.width / paddedUnitSize;
            var unitIndex = 0;
            foreach (var unit in packedUnits)
            {
                var row = ++unitIndex / unitsPerRow + 1;
                var column = unitIndex % unitsPerRow + 1;
                SetPixels(row, column, unit.PaddedPixels, atlasTexture);
                contentToUV[unit.ContentHash] = GetUV(column, row, atlasSize);
            }
            return contentToUV;
        }

        private void SetPixels (int column, int row, Color[] pixels, Texture2D texture)
        {
            var x = column * paddedUnitSize;
            var y = row * paddedUnitSize;
            texture.SetPixels(x, y, paddedUnitSize, paddedUnitSize, pixels);
        }

        private Rect GetUV (int column, int row, Vector2 atlasSize)
        {
            var width = (unitSize - uvInset) / atlasSize.x;
            var height = (unitSize - uvInset) / atlasSize.y;
            var posX = (column * paddedUnitSize - padding + uvInset) / atlasSize.x;
            var posY = (row * paddedUnitSize - padding + uvInset) / atlasSize.y;
            return new Rect(posX, posY, width, height);
        }
    }
}
