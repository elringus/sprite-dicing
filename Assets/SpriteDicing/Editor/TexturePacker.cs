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
            var atlasSize = EvaluateAtlasSize(packedUnits.Count);
            var atlasTexture = CreateAtlasTexture(atlasSize);
            var contentToUV = MapContent(packedUnits, atlasTexture);
            return new AtlasTexture(serializer.Serialize(atlasTexture), contentToUV, packedTextures);
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

        private Vector2Int EvaluateAtlasSize (int unitsCount)
        {
            var size = Vector2Int.one * Mathf.CeilToInt(Mathf.Sqrt(unitsCount));
            if (square) return size * paddedUnitSize;
            for (var width = size.x; width > 0; width--)
            {
                var height = Mathf.CeilToInt(unitsCount / (float)width);
                if (height * paddedUnitSize > sizeLimit) break;
                if (width * height < size.x * size.y)
                    size = new Vector2Int(width, height);
            }
            return size * paddedUnitSize;
        }

        private Texture2D CreateAtlasTexture (Vector2Int size)
        {
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private Dictionary<Hash128, Rect> MapContent (HashSet<DicedUnit> packedUnits, Texture2D atlasTexture)
        {
            var contentToUV = new Dictionary<Hash128, Rect>();
            var atlasSize = new Vector2(atlasTexture.width, atlasTexture.height);
            var unitsPerRow = atlasTexture.width / paddedUnitSize;
            var unitIndex = 0;
            foreach (var unit in packedUnits)
            {
                var row = unitIndex / unitsPerRow;
                var column = unitIndex++ % unitsPerRow;
                SetPixels(column, row, unit.PaddedPixels, atlasTexture);
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
            var width = unitSize / atlasSize.x;
            var height = unitSize / atlasSize.y;
            var posX = (column * paddedUnitSize + padding) / atlasSize.x;
            var posY = (row * paddedUnitSize + padding) / atlasSize.y;
            return InsetUV(new Rect(posX, posY, width, height));
        }

        private Rect InsetUV (Rect uv)
        {
            var crop = uvInset * (uv.width / 2f);
            var position = uv.position + Vector2.one * crop;
            var size = uv.size - Vector2.one * (crop * 2);
            return new Rect(position, size);
        }
    }
}
