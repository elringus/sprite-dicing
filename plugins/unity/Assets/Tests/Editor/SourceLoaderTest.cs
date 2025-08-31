using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;
using static SpriteDicing.Test.Helpers.Paths;

namespace SpriteDicing.Test
{
    public class SourceLoaderTest
    {
        [Test]
        public void WhenNullPathExceptionIsThrown ()
        {
            Throws<ArgumentNullException>(() => Load(null));
        }

        [Test]
        public void WhenEmptyPathExceptionIsThrown ()
        {
            Throws<ArgumentNullException>(() => Load(""));
        }

        [Test]
        public void WhenNoAssetExceptionIsThrown ()
        {
            //TODO: May SourceLoader should throw it when AssetDatabase cannot find or incorrect type of assets.
            Throws<ArgumentException>(() => Load("N/A"));
        }

        [Test]
        public void WhenInvalidAssetExceptionIsThrown ()
        {
            //TODO: May SourceLoader should throw it when AssetDatabase cannot find or incorrect type of assets.
            Throws<ArgumentException>(() => Load(TextureFolderPath));
        }

        [Test]
        public void LoadsPixelsOfTheSourceTexture ()
        {
            var pixels = Load(BGRT).FirstOrDefault().Texture.Pixels;
            AreEqual(4, pixels.Count);
            AreEqual(new Native.Pixel { R = 255, G = 0, B = 0, A = 255 }, pixels[0]);
            // Neighbors of clear pixels leak color components when reading with Unity's API.
            AreEqual(new Native.Pixel { R = 255, G = 0, B = 0, A = 0 }, pixels[1]);
            AreEqual(new Native.Pixel { R = 0, G = 0, B = 255, A = 255 }, pixels[2]);
            AreEqual(new Native.Pixel { R = 0, G = 255, B = 0, A = 255 }, pixels[3]);
        }

        [Test]
        public void WhenNoAssociatedSpritePivotHasNoValue ()
        {
            IsFalse(Load(RGB1x3).FirstOrDefault().Pivot.HasValue);
        }

        [Test]
        public void WhenAssociatedSpriteExistPivotHasValue ()
        {
            IsTrue(Load(RGB4x4).FirstOrDefault().Pivot.HasValue);
        }

        [Test]
        public void WhenAssociatedSpriteExistButKeepPivotDisabledPivotHasNoValue ()
        {
            IsFalse(Load(RGB4x4, keepPivot: false).FirstOrDefault().Pivot.HasValue);
        }

        [Test]
        public void WhenNameRootInvalidExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Load(BGRT, "N/A"));
        }

        [Test]
        public void SubFoldersAreJoinedWithSpecifiedSeparator ()
        {
            AreEqual("2x2.BTGR", Load(BTGR, separator: ".").FirstOrDefault().Id);
        }

        [Test]
        public void WhenNotReadableBecomesReadableAfterLoad ()
        {
            GetImporter(RGB3x1).isReadable = false;
            Load(RGB3x1);
            IsTrue(GetImporter(RGB3x1).isReadable);
        }

        [Test]
        public void WhenCrunchedBecomesNotCrunchedAfterLoad ()
        {
            GetImporter(RGB4x4).crunchedCompression = true;
            Load(RGB4x4);
            IsFalse(GetImporter(RGB4x4).crunchedCompression);
        }

        //TODO: Should take test about multiple type of sprite asset, mostly these are belong to sub-asset to owner texture.

        private static IEnumerable<Native.SourceSprite> Load (string texturePath, string root = TextureFolderPath,
            string separator = ".", bool keepPivot = true)
        {
            return new SourceLoader(root, separator, keepPivot)
                .Load(texturePath)
                .Select(static sourceSprite => sourceSprite.Native)
                .ToArray(); //Forcing evaluate queries for test, because these are not raise exceptions or give results until evaluation.
        }
    }
}
