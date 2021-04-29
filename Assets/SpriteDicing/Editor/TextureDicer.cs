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
            var texture = source.Texture;
            var units = new List<DicedUnit>();
            var unitCountX = Mathf.CeilToInt((float)texture.width / unitSize);
            var unitCountY = Mathf.CeilToInt((float)texture.height / unitSize);

            for (int unitX = 0; unitX < unitCountX; unitX++)
            {
                var x = unitX * unitSize;
                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    var y = unitY * unitSize;
                    var pixelsRect = new Rect(x, y, unitSize, unitSize);
                    var colors = texture.GetPixels(pixelsRect); // TODO: Get only padded pixels and evaluate original pixels from them.
                    // Skip transparent units (no need to render them).
                    if (colors.All(color => color.a == 0)) continue;
                    var paddedRect = pixelsRect.Crop(padding);
                    var paddedColors = texture.GetPixels(paddedRect);
                    var quadVerts = pixelsRect.Scale(1f / ppu);
                    // TODO: Find out how Unity builds that hash and replicate. Comparing each element in color arrays has issues with flat color textures and small dice units.
                    var hash = Utilities.CreateTexture(unitSize, colors).imageContentsHash;
                    var dicedUnit = new DicedUnit { QuadVerts = quadVerts, ContentHash = hash, PaddedColors = paddedColors };
                    units.Add(dicedUnit);
                }
            }

            return new DicedTexture(source, units);
        }
    }
}
