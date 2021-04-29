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

        private static DicedTexture Dice (Texture2D texture, int size = 1, int padding = 0, float ppu = 100)
        {
            var source = new SourceTexture(texture.name, texture);
            return new TextureDicer().Dice(source, size, padding, ppu);
        }
    }
}
