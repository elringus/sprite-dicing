using System;
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
            Throws<ArgumentException>(() => Load("N/A"));
        }

        [Test]
        public void WhenInvalidAssetExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Load(TextureFolderPath));
        }

        [Test]
        public void LoadsPixelsOfTheSourceTexture ()
        {
            var pixels = Load(BGRT).Texture.Pixels;
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
            IsFalse(Load(RGB1x3).Pivot.HasValue);
        }

        [Test]
        public void WhenAssociatedSpriteExistPivotHasValue ()
        {
            IsTrue(Load(RGB4x4).Pivot.HasValue);
        }

        [Test]
        public void WhenAssociatedSpriteExistButKeepPivotDisabledPivotHasNoValue ()
        {
            IsFalse(Load(RGB4x4, keepPivot: false).Pivot.HasValue);
        }

        [Test]
        public void WhenNameRootInvalidExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Load(BGRT, "N/A"));
        }

        [Test]
        public void SubFoldersAreJoinedWithSpecifiedSeparator ()
        {
            AreEqual("2x2.BTGR", Load(BTGR, separator: ".").Id);
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

        private static Native.SourceSprite Load (string texturePath, string root = TextureFolderPath,
            string separator = ".", bool keepPivot = true)
        {
            var sources = new System.Collections.Generic.List<SourceSprite>();
            new SourceLoader(root, separator, keepPivot).Load(texturePath, sources);
            return sources[0].Native;
        }
    }
}
