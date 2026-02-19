using System;
using System.Linq;
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
            Throws<ArgumentException>(() => Load(SourceFolderPath));
        }

        [Test]
        public void LoadsPixelsOfTheSourceTexture ()
        {
            var pixels = Load(BGRT)[0].Texture.Pixels;
            AreEqual(4, pixels.Count);
            AreEqual(new Native.Pixel { R = 255, G = 0, B = 0, A = 255 }, pixels[0]);
            // Neighbors of clear pixels leak color components when reading with Unity's API.
            AreEqual(new Native.Pixel { R = 255, G = 0, B = 0, A = 0 }, pixels[1]);
            AreEqual(new Native.Pixel { R = 0, G = 0, B = 255, A = 255 }, pixels[2]);
            AreEqual(new Native.Pixel { R = 0, G = 255, B = 0, A = 255 }, pixels[3]);
        }

        [Test]
        public void CanResolvePivot ()
        {
            IsTrue(Load(RGB4x4)[0].Pivot.HasValue);
        }

        [Test]
        public void WhenAssociatedSpriteExistButKeepPivotDisabledPivotHasNoValue ()
        {
            IsFalse(Load(RGB4x4, keepPivot: false)[0].Pivot.HasValue);
        }

        [Test]
        public void WhenNameRootInvalidExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Load(BGRT, "N/A"));
        }

        [Test]
        public void SubFoldersAreJoinedWithSpecifiedSeparator ()
        {
            AreEqual("2x2.BTGR", Load(BTGR, separator: ".")[0].Id);
        }

        [Test]
        public void MultipleSpritesAreJoinedWithSpecifiedSeparator ()
        {
            var sources = Load(Multiple, separator: ".");
            AreEqual("Multiple.0", sources[0].Id);
            AreEqual("Multiple.1", sources[1].Id);
            AreEqual("Multiple.2", sources[2].Id);
            AreEqual("Multiple.3", sources[3].Id);
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

        private static Native.SourceSprite[] Load (string sourcePath, string root = SourceFolderPath,
            string separator = ".", bool keepPivot = true)
        {
            var sources = new System.Collections.Generic.List<SourceSprite>();
            new SourceLoader(root, separator, keepPivot).Load(sourcePath, sources);
            return sources.Select(s => s.Native).ToArray();
        }
    }
}
