using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;

namespace SpriteDicing.Test
{
    public class TextureDicerTest
    {
        [Test]
        public void WhenSizeZeroExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Dice(Textures.BGRT, size: 0));
        }

        [Test]
        public void WhenPaddingNegativeExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Dice(Textures.BGRT, padding: -1));
        }

        [Test]
        public void WhenPPUZeroExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Dice(Textures.BGRT, ppu: 0));
        }

        [Test]
        public void UnitCountEqualDoubleTextureSizeDividedByUnitSizeSquare ()
        {
            AreEqual(3, Dice(Textures.RGB1x3).Units.Count);
            AreEqual(4, Dice(Textures.RGB8x8, 2).Units.Count);
            AreEqual(1, Dice(Textures.RGB8x8, 4).Units.Count);
        }

        [Test]
        public void UnitCountDoesntDependOnPadding ()
        {
            var normalUnits = Dice(Textures.RGB8x8, padding: 0).Units;
            var paddedUnits = Dice(Textures.RGB8x8, padding: 1).Units;
            AreEqual(normalUnits.Count, paddedUnits.Count);
        }

        [Test]
        public void WhenUnitSizeIsLargerThanTextureSingleUnitIsDiced ()
        {
            AreEqual(1, Dice(Textures.RGB3x1, 5).Units.Count);
            AreEqual(1, Dice(Textures.RGB8x8, 128).Units.Count);
        }

        [Test]
        public void TransparentDicesAreIgnored ()
        {
            IsEmpty(Dice(Textures.TTTT).Units);
            IsFalse(Dice(Textures.BGRT).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
            IsFalse(Dice(Textures.BTGR).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
        }

        [Test]
        public void SourceTextureIsPreserved ()
        {
            AreEqual(Textures.RGB8x8, Dice(Textures.RGB8x8).Source.Texture);
        }

        [Test]
        public void ContentHashOfEqualPixelsIsEqual ()
        {
            var units = Dice(Textures.BGRT).Units;
            foreach (var unit in Dice(Textures.BTGR).Units)
                IsTrue(units.Any(u => u.ContentHash == unit.ContentHash));
        }

        [Test]
        public void ContentHashOfDistinctPixelsIsNotEqual ()
        {
            AreNotEqual(Dice(Textures.B).Units[0].ContentHash, Dice(Textures.R).Units[0].ContentHash);
        }

        [Test]
        public void ContentHashIgnoresPadding ()
        {
            var normalUnits = Dice(Textures.RGB8x8, padding: 0).Units;
            var paddedUnits = Dice(Textures.RGB8x8, padding: 1).Units;
            foreach (var paddedUnit in paddedUnits)
                IsTrue(normalUnits.Any(u => u.ContentHash == paddedUnit.ContentHash));
        }

        [Test]
        public void UnitVertsAreMappedToSourceTexture ()
        {
            var verts = Dice(Textures.BGRT, ppu: 1).Units.Select(u => u.QuadVerts).ToArray();
            Contains(new Rect(0, 0, 1, 1), verts);
            Contains(new Rect(0, 1, 1, 1), verts);
            Contains(new Rect(1, 1, 1, 1), verts);
        }

        [Test]
        public void UnitVertsAreScaledByPPU ()
        {
            var verts = Dice(Textures.RGB3x1, ppu: 100).Units.Select(u => u.QuadVerts).ToArray();
            Contains(new Rect(0.00f, 0, 0.01f, 0.01f), verts);
            Contains(new Rect(0.01f, 0, 0.01f, 0.01f), verts);
            Contains(new Rect(0.02f, 0, 0.01f, 0.01f), verts);
        }

        [Test]
        public void WhenNoContentPaddedPixelsAreRepeated ()
        {
            var pixels = Dice(Textures.B, padding: 1).Units[0].PaddedPixels;
            var expected = new[] {
                Color.blue, Color.blue, Color.blue,
                Color.blue, Color.blue, Color.blue,
                Color.blue, Color.blue, Color.blue
            };
            AreEqual(expected, pixels);
        }
        
        [Test]
        public void PaddedPixelsAreNeighbors ()
        {
            var pixels = Dice(Textures.BGRT, padding: 1).Units.Select(u => u.PaddedPixels).ToArray();
            var expected = new[] {
                Color.clear, Color.red, Color.clear,
                Color.green, Color.blue, Color.green,
                Color.clear, Color.red, Color.clear
            };
            Contains(expected, pixels);
        }

        private static DicedTexture Dice (Texture2D texture, int size = 1, int padding = 0, float ppu = 100)
        {
            var source = new SourceTexture(texture.name, texture);
            return new TextureDicer().Dice(source, size, padding, ppu);
        }
    }
}
