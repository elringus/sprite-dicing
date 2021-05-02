using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers.Textures;

namespace SpriteDicing.Test
{
    public class TextureDicerTest
    {
        [Test]
        public void WhenSizeZeroExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Dice(BGRT, size: 0));
        }

        [Test]
        public void WhenPaddingNegativeExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Dice(BGRT, padding: -1));
        }

        [Test]
        public void WhenPPUZeroExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Dice(BGRT, ppu: 0));
        }

        [Test]
        public void UnitCountEqualDoubleTextureSizeDividedByUnitSizeSquare ()
        {
            AreEqual(3, Dice(RGB1x3).Units.Count);
            AreEqual(4, Dice(RGB8x8, 2).Units.Count);
            AreEqual(1, Dice(RGB8x8, 4).Units.Count);
        }

        [Test]
        public void UnitCountDoesntDependOnPadding ()
        {
            var normalUnits = Dice(RGB8x8, padding: 0).Units;
            var paddedUnits = Dice(RGB8x8, padding: 1).Units;
            AreEqual(normalUnits.Count, paddedUnits.Count);
        }

        [Test]
        public void WhenUnitSizeIsLargerThanTextureSingleUnitIsDiced ()
        {
            AreEqual(1, Dice(RGB3x1, 5).Units.Count);
            AreEqual(1, Dice(RGB8x8, 128).Units.Count);
        }

        [Test]
        public void TransparentDicesAreIgnored ()
        {
            IsEmpty(Dice(TTTT).Units);
            IsFalse(Dice(BGRT).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
            IsFalse(Dice(BTGR).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
        }

        [Test]
        public void SourceTextureIsPreserved ()
        {
            AreEqual(RGB8x8, Dice(RGB8x8).Source.Texture);
        }

        [Test]
        public void ContentHashOfEqualPixelsIsEqual ()
        {
            var units = Dice(BGRT).Units;
            foreach (var unit in Dice(BTGR).Units)
                IsTrue(units.Any(u => u.ContentHash == unit.ContentHash));
        }

        [Test]
        public void ContentHashOfDistinctPixelsIsNotEqual ()
        {
            AreNotEqual(Dice(B).Units[0].ContentHash, Dice(R).Units[0].ContentHash);
        }

        [Test]
        public void ContentHashIgnoresPadding ()
        {
            var normalUnits = Dice(RGB8x8, padding: 0).Units;
            var paddedUnits = Dice(RGB8x8, padding: 1).Units;
            foreach (var paddedUnit in paddedUnits)
                IsTrue(normalUnits.Any(u => u.ContentHash == paddedUnit.ContentHash));
        }

        [Test]
        public void UnitVertsAreMappedToSourceTexture ()
        {
            var verts = Dice(BGRT, ppu: 1).Units.Select(u => u.QuadVerts).ToArray();
            Contains(new Rect(0, 0, 1, 1), verts);
            Contains(new Rect(0, 1, 1, 1), verts);
            Contains(new Rect(1, 1, 1, 1), verts);
        }

        [Test]
        public void UnitVertsAreScaledByPPU ()
        {
            var verts = Dice(RGB3x1, ppu: 100).Units.Select(u => u.QuadVerts).ToArray();
            Contains(new Rect(0.00f, 0, 0.01f, 0.01f), verts);
            Contains(new Rect(0.01f, 0, 0.01f, 0.01f), verts);
            Contains(new Rect(0.02f, 0, 0.01f, 0.01f), verts);
        }

        [Test]
        public void WhenNoContentPaddedPixelsAreRepeated ()
        {
            var pixels = Dice(B, padding: 1).Units[0].PaddedPixels;
            var expected = Map3x3(
                Color.blue, Color.blue, Color.blue,
                Color.blue, Color.blue, Color.blue,
                Color.blue, Color.blue, Color.blue);
            AreEqual(expected, pixels);
        }

        [Test]
        public void PaddedPixelsAreNeighbors ()
        {
            var pixels = Dice(BGRT, padding: 1).Units.Select(u => u.PaddedPixels).ToArray();
            var expected = Map3x3(
                Color.blue, Color.blue, Color.green,
                Color.blue, Color.blue, Color.green,
                Color.red, Color.red, new Color(1, 0, 0, 0));
            Contains(expected, pixels);
        }

        [Test]
        public void WhenUnitsNullExceptionIsThrown ()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Throws<ArgumentNullException>(() => new DicedTexture(default, null));
        }

        [Test]
        public void UnitsWithEqualContentHashAreEqual ()
        {
            var unit1 = new DicedUnit(new Rect(0, 0, 1, 1), new[] { Color.green }, new Hash128(1, 1));
            var unit2 = new DicedUnit(new Rect(1, 1, 1, 1), new[] { Color.black }, new Hash128(1, 1));
            AreEqual(unit1, unit2);
        }

        [Test]
        public void BoxedUnitsWithEqualContentHashAreEqual ()
        {
            var unit1 = new DicedUnit(new Rect(0, 0, 1, 1), new[] { Color.green }, new Hash128(1, 1));
            var unit2 = new DicedUnit(new Rect(1, 1, 1, 1), new[] { Color.black }, new Hash128(1, 1));
            IsTrue(unit1.Equals((object)unit2));
        }

        [Test]
        public void UnitsWithDifferentContentHashAreNotEqual ()
        {
            CollectionAssert.AllItemsAreUnique(Dice(RGB1x3).Units);
        }

        private static DicedTexture Dice (Texture2D texture, int size = 1, int padding = 0, float ppu = 100)
        {
            var source = new SourceTexture(texture.name, texture);
            return new TextureDicer(size, padding, ppu).Dice(source);
        }

        private static Color[] Map3x3 (params Color[] colors)
        {
            return new[] {
                colors[6], colors[7], colors[8],
                colors[3], colors[4], colors[5],
                colors[0], colors[1], colors[2]
            };
        }
    }
}
