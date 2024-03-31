using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;
using static SpriteDicing.Test.Helpers.Paths;

namespace SpriteDicing.Test
{
    public class SpriteBuilderTest
    {
        [Test]
        public void CustomPivotIsAppliedToSprite ()
        {
            var pivot = new Vector2(.95f, -.15f);
            AreEqual(pivot, Build(new[] { B }, pivot: pivot)[0].pivot);
        }

        [Test]
        public void OriginalPivotIsPreserved ()
        {
            AreEqual(Vector2.one, Build(new[] { RGB4x4 }, keepOriginalPivot: true)[0].pivot);
        }

        [Test]
        public void SpriteSizeIsPreserved ()
        {
            AreEqual(Vector2.one, Build(new[] { B })[0].rect.size);
            AreEqual(new Vector2(2, 2), Build(new[] { BGRT })[0].rect.size);
            AreEqual(new Vector2(1, 3), Build(new[] { RGB1x3 })[0].rect.size);
            AreEqual(new Vector2(3, 1), Build(new[] { RGB3x1 })[0].rect.size);
            AreEqual(new Vector2(4, 4), Build(new[] { RGB4x4 })[0].rect.size);
        }

        [Test]
        public void SpriteVertsAreScaledByPPU ()
        {
            AreEqual(Vector2.one, (Vector2)Build(new[] { B }, ppu: 1)[0].bounds.size);
            AreEqual(Vector2.one * .5f, (Vector2)Build(new[] { B }, ppu: 2)[0].bounds.size);
        }

        [Test]
        public void TransparentAreasAreTrimmed ()
        {
            AreEqual(new Vector2(1, 2), Build(new[] { BTGT })[0].rect.size);
        }

        [Test]
        public void EmptySpritesAreIgnored ()
        {
            IsEmpty(Build(new[] { TTTT }, trim: true));
        }

        
        [Test]
        public void ProgressIsReported ()
        {
            var progress = default(Native.Progress);
            Build(new[] { RGB4x4 }, onProgress: p => progress = p);
            AreEqual(1f, progress.Ratio);
        }

        private Sprite[] Build (string[] texturePaths, float uvInset = 0, bool square = false, bool pot = false, int sizeLimit = 8,
            int unitSize = 1, int padding = 0, float ppu = 1, Vector2 pivot = default, bool keepOriginalPivot = false, bool trim = true,
            Native.ProgressCallback onProgress = null)
        {
            var loader = new SourceLoader(TextureFolderPath, ".", keepOriginalPivot);
            var sources = texturePaths.Select(loader.Load);
            var prefs = new Native.Prefs {
                UnitSize = (uint)unitSize,
                Padding = (uint)padding,
                UVInset = uvInset,
                TrimTransparent = trim,
                AtlasSizeLimit = (uint)sizeLimit,
                AtlasSquare = square,
                AtlasPOT = pot,
                PPU = ppu,
                Pivot = new Native.Pivot { X = pivot.x, Y = pivot.y },
                OnProgress = onProgress
            };
            using var diced = Native.Dice(sources, prefs);
            var atlases = diced.Atlases.Select(bytes => {
                var tex = new Texture2D(2, 2);
                ImageConversion.LoadImage(tex, bytes);
                return tex;
            }).ToArray();
            var builder = new SpriteBuilder(ppu, atlases);
            return diced.Sprites.Select(builder.Build).ToArray();
        }
    }
}
