using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers.Textures;
using static SpriteDicing.Test.Helpers.Colors;

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
        public void UnitCountEqualDoubleTextureSizeDividedByUnitSizeSquare ()
        {
            AreEqual(3, Dice(RGB1x3).Units.Count);
            AreEqual(4, Dice(RGB4x4, 2).Units.Count);
            AreEqual(1, Dice(RGB4x4, 4).Units.Count);
        }

        [Test]
        public void UnitCountDoesntDependOnPadding ()
        {
            var normalUnits = Dice(RGB4x4, padding: 0).Units;
            var paddedUnits = Dice(RGB4x4, padding: 1).Units;
            AreEqual(normalUnits.Count, paddedUnits.Count);
        }

        [Test]
        public void WhenUnitSizeIsLargerThanTextureSingleUnitIsDiced ()
        {
            AreEqual(1, Dice(RGB3x1, 5).Units.Count);
            AreEqual(1, Dice(RGB4x4, 128).Units.Count);
        }

        [Test]
        public void TransparentDicesAreIgnoredWhenTrimEnabled ()
        {
            IsEmpty(Dice(TTTT, trim: true).Units);
            IsFalse(Dice(BGRT, trim: true).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
            IsFalse(Dice(BTGR, trim: true).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
        }

        [Test]
        public void TransparentDicesArePreservedWhenTrimDisabled ()
        {
            IsNotEmpty(Dice(TTTT, trim: false).Units);
            IsTrue(Dice(BGRT, trim: false).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
            IsTrue(Dice(BTGR, trim: false).Units.Any(u => u.PaddedPixels.Any(p => p.a == 0)));
        }

        [Test]
        public void SourceTextureIsPreserved ()
        {
            AreEqual(RGB4x4, Dice(RGB4x4).Source.Texture);
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
            AreNotEqual(Dice(B).Units.First().ContentHash, Dice(R).Units.First().ContentHash);
        }

        [Test]
        public void ContentHashIgnoresPadding ()
        {
            var normalUnits = Dice(RGB4x4, padding: 0).Units;
            var paddedUnits = Dice(RGB4x4, padding: 1).Units;
            foreach (var paddedUnit in paddedUnits)
                IsTrue(normalUnits.Any(u => u.ContentHash == paddedUnit.ContentHash));
        }

        [Test]
        public void UnitVertsAreMappedToSourceTexture ()
        {
            var verts = Dice(BGRT).Units.Select(u => u.QuadVerts).ToArray();
            Contains(new RectInt(0, 0, 1, 1), verts);
            Contains(new RectInt(0, 1, 1, 1), verts);
            Contains(new RectInt(1, 1, 1, 1), verts);
        }

        [Test]
        public void WhenNoContentPaddedPixelsAreRepeated ()
        {
            var pixels = Dice(B, padding: 1).Units.First().PaddedPixels;
            var expected = Map3x3(
                Blue, Blue, Blue,
                Blue, Blue, Blue,
                Blue, Blue, Blue);
            AreEqual(expected, pixels);
        }

        [Test]
        public void PaddedPixelsAreNeighbors ()
        {
            var pixels = Dice(BGRT, padding: 1).Units.Select(u => u.PaddedPixels).ToArray();
            var expected = Map3x3(
                Blue, Blue, Green,
                Blue, Blue, Green,
                Red, Red, new Color32(255, 0, 0, 0));
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
            var unit1 = new DicedUnit(new RectInt(0, 0, 1, 1), new[] { Green }, new Hash128(1, 1));
            var unit2 = new DicedUnit(new RectInt(1, 1, 1, 1), new[] { Black }, new Hash128(1, 1));
            AreEqual(unit1, unit2);
        }

        [Test]
        public void BoxedUnitsWithEqualContentHashAreEqual ()
        {
            var unit1 = new DicedUnit(new RectInt(0, 0, 1, 1), new[] { Green }, new Hash128(1, 1));
            var unit2 = new DicedUnit(new RectInt(1, 1, 1, 1), new[] { Black }, new Hash128(1, 1));
            IsTrue(unit1.Equals((object)unit2));
        }

        [Test]
        public void UnitsWithDifferentContentHashAreNotEqual ()
        {
            CollectionAssert.AllItemsAreUnique(Dice(RGB1x3).Units);
        }

        [Test]
        public void DistinctUnitsAreNotEqual ()
        {
            CollectionAssert.AllItemsAreUnique(Dice(RGB4x4).Units.Distinct());
        }

        [Test]
        public void UniqueUnitsAreSubsetOfUnits ()
        {
            var dicedTexture = Dice(RGB4x4);
            CollectionAssert.IsSubsetOf(dicedTexture.UniqueUnits, dicedTexture.Units);
        }

        private static DicedTexture Dice (Texture2D texture, int size = 1, int padding = 0, bool trim = true)
        {
            var source = new SourceTexture(texture.name, texture);
            return new TextureDicer(size, padding, trim).Dice(source);
        }

        private static Color32[] Map3x3 (params Color32[] colors)
        {
            return new[] {
                colors[6], colors[7], colors[8],
                colors[3], colors[4], colors[5],
                colors[0], colors[1], colors[2]
            };
        }
    }
}
