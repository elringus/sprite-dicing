using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for building atlas textures from diced textures.
    /// </summary>
    public class AtlasTextureBuilder
    {
        private readonly ITextureSerializer textureSerializer;
        private readonly int unitSize;
        private readonly int padding;
        private readonly float uvInset;
        private readonly bool forceSquare;
        private readonly int atlasSizeLimit;

        public AtlasTextureBuilder (ITextureSerializer textureSerializer, int unitSize, int padding,
            float uvInset, bool forceSquare, int atlasSizeLimit)
        {
            this.textureSerializer = textureSerializer;
            this.unitSize = unitSize;
            this.padding = padding;
            this.uvInset = uvInset;
            this.forceSquare = forceSquare;
            this.atlasSizeLimit = atlasSizeLimit;
        }

        public IReadOnlyList<AtlasTexture> Build (IEnumerable<DicedTexture> dicedTextures)
        {
            var atlases = new List<AtlasTexture>();
            var paddedUnitSize = unitSize + padding * 2;
            var unitsPerAtlasLimit = Mathf.FloorToInt(Mathf.Pow(Mathf.FloorToInt(atlasSizeLimit / (float)paddedUnitSize), 2));
            var texturesToPack = new HashSet<DicedTexture>(dicedTextures);

            while (texturesToPack.Count > 0)
            {
                var atlasTexture = CreateAtlasTexture(atlasSizeLimit, atlasSizeLimit);
                var contentToUV = new Dictionary<Hash128, Rect>();
                var packedTextures = new List<DicedTexture>();
                var yToLastXMap = new Dictionary<int, int>(); // Y position of a units row in the current atlas to the x position of the last unit in this row.
                var xLimit = Mathf.NextPowerOfTwo(paddedUnitSize); // Maximum allowed width of the current atlas. Increases by the power of two in the process.

                while (FindTextureToPack(texturesToPack, contentToUV, unitsPerAtlasLimit) is DicedTexture textureToPack)
                {
                    foreach (var unitToPack in textureToPack.UniqueUnits)
                    {
                        if (contentToUV.ContainsKey(unitToPack.ContentHash)) continue;

                        int posX, posY; // Position of the new unit on the atlas texture.
                        // Find row positions that have enough room for more units until next power of two.
                        var suitableYToLastXEnumerable = yToLastXMap.Where(yToLastX => xLimit - yToLastX.Value >= paddedUnitSize * 2).ToArray();
                        if (suitableYToLastXEnumerable.Length == 0) // When no suitable rows found.
                        {
                            // Handle corner case when we just started.
                            if (yToLastXMap.Count == 0)
                            {
                                yToLastXMap.Add(0, 0);
                                posX = 0;
                                posY = 0;
                            }
                            // Determine whether we need to add a new row or increase x limit.
                            else if (xLimit > yToLastXMap.Last().Key)
                            {
                                var newRowYPos = yToLastXMap.Last().Key + paddedUnitSize;
                                yToLastXMap.Add(newRowYPos, 0);
                                posX = 0;
                                posY = newRowYPos;
                            }
                            else
                            {
                                xLimit = Mathf.NextPowerOfTwo(xLimit + 1);
                                posX = yToLastXMap.First().Value + paddedUnitSize;
                                posY = 0;
                                yToLastXMap[0] = posX;
                            }
                        }
                        else // When suitable rows found.
                        {
                            // Find one with the least number of elements and use it.
                            var suitableYToLastX = suitableYToLastXEnumerable.OrderBy(yToLastX => yToLastX.Value).First();
                            posX = suitableYToLastX.Value + paddedUnitSize;
                            posY = suitableYToLastX.Key;
                            yToLastXMap[posY] = posX;
                        }

                        // Write colors of the unit to the current atlas texture.
                        var colorsToPack = unitToPack.PaddedPixels;
                        atlasTexture.SetPixels(posX, posY, paddedUnitSize, paddedUnitSize, colorsToPack);
                        // Evaluate and assign UVs of the unit to the other units in the group.
                        var unitUVRect = new Rect(posX, posY, paddedUnitSize, paddedUnitSize).Crop(-padding).Scale(1f / atlasSizeLimit);
                        if (uvInset > 0) unitUVRect = unitUVRect.Crop(-uvInset * (unitUVRect.width / 2f));
                        contentToUV.Add(unitToPack.ContentHash, unitUVRect);
                    }

                    texturesToPack.Remove(textureToPack);
                    packedTextures.Add(textureToPack);
                }

                if (packedTextures.Count == 0) throw new Exception("Unable to fit diced textures. Consider increasing atlas size limit.");

                // Crop unused atlas texture space.
                var needToCrop = xLimit < atlasSizeLimit || (!forceSquare && yToLastXMap.Last().Key + paddedUnitSize < atlasSizeLimit);
                if (needToCrop)
                {
                    var croppedHeight = forceSquare ? xLimit : yToLastXMap.Last().Key + paddedUnitSize;
                    var croppedPixels = atlasTexture.GetPixels(0, 0, xLimit, croppedHeight);
                    atlasTexture = CreateAtlasTexture(xLimit, croppedHeight);
                    atlasTexture.SetPixels(croppedPixels);

                    // Correct UV rects after crop.
                    foreach (var kv in contentToUV.ToArray())
                        contentToUV[kv.Key] = kv.Value.Scale(new Vector2(atlasSizeLimit / (float)xLimit, atlasSizeLimit / (float)croppedHeight));
                }

                atlasTexture.alphaIsTransparency = true;
                atlasTexture.Apply();
                var textureAsset = textureSerializer.Serialize(atlasTexture);
                atlases.Add(new AtlasTexture(textureAsset, contentToUV, packedTextures));
            }

            return atlases;
        }

        private static Texture2D CreateAtlasTexture (int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static DicedTexture FindTextureToPack (IEnumerable<DicedTexture> textures, IDictionary<Hash128, Rect> contentToUV, int unitsPerAtlasLimit)
        {
            foreach (var texture in textures)
                if (CountUnitsToPack(texture, contentToUV) <= unitsPerAtlasLimit)
                    return texture;
            return null;
        }

        private static int CountUnitsToPack (DicedTexture texture, IDictionary<Hash128, Rect> contentToUV)
        {
            return contentToUV.Keys.Count + texture.UniqueUnits.Count(u => !contentToUV.ContainsKey(u.ContentHash));
        }
    }
}
