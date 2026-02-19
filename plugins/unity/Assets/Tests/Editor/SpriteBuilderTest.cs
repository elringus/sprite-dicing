using System;
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
            IsNotEmpty(progress.Activity);
        }

        [Test]
        public void ThrowsOnDicingError ()
        {
            AreEqual("Unit size can't be zero.", Throws<Exception>(() =>
                Build(new[] { RGB4x4 }, unitSize: 0)).Message);
        }

        [Test]
        public void PixelsHashedByAllComponents ()
        {
            AreEqual(new Native.Pixel(1, 2, 3, 4), new Native.Pixel(1, 2, 3, 4));
            AreNotEqual(new Native.Pixel(1, 2, 3, 0), new Native.Pixel(1, 2, 3, 4));
            IsTrue(new Native.Pixel(1, 2, 3, 4).Equals((object)new Native.Pixel(1, 2, 3, 4)));
            AreEqual(new Native.Pixel(0, 0, 0, 0).GetHashCode(), default(Native.Pixel).GetHashCode());
        }

        private Sprite[] Build (string[] sourcePaths, float uvInset = 0, bool square = false, bool pot = false, int sizeLimit = 8,
            int unitSize = 1, int padding = 0, float ppu = 1, Vector2 pivot = default, bool keepOriginalPivot = false, bool trim = true,
            Native.ProgressCallback onProgress = null)
        {
            var sources = new System.Collections.Generic.List<SourceSprite>();
            var loader = new SourceLoader(SourceFolderPath, ".", keepOriginalPivot);
            foreach (var path in sourcePaths)
                loader.Load(path, sources);
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
            var diced = Native.Dice(sources.Select(s => s.Native), prefs);
            var atlases = diced.Atlases.Select(tex => {
                var asset = new Texture2D((int)tex.Width, (int)tex.Height);
                asset.SetPixels32(tex.Pixels.Select(p => new Color32(p.R, p.G, p.B, p.A)).ToArray());
                asset.Apply(false, true);
                return asset;
            }).ToArray();
            var builder = new SpriteBuilder(ppu, atlases);
            return diced.Sprites.Select(builder.Build).ToArray();
        }
    }
}
