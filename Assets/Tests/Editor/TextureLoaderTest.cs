using System;
using NUnit.Framework;
using UnityEditor;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;
using static SpriteDicing.Test.Helpers.Paths;

namespace SpriteDicing.Test
{
    public class TextureLoaderTest
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
            Throws<Exception>(() => Load("N/A"));
        }

        [Test]
        public void WhenInvalidAssetExceptionIsThrown ()
        {
            Throws<Exception>(() => Load(TextureFolderPath));
        }

        [Test]
        public void LoadedTextureAssetIsValid ()
        {
            IsTrue(Load(BGRT).Texture);
        }

        [Test]
        public void WhenNoAssociatedSpriteItsNull ()
        {
            IsNull(Load(RGB1x3).Sprite);
        }

        [Test]
        public void WhenAssociatedSpriteExistItsLoaded ()
        {
            IsTrue(Load(RGB8x8).Sprite);
        }

        [Test]
        public void WhenNameRootInvalidExceptionIsThrown ()
        {
            Throws<Exception>(() => Load(BGRT, "N/A"));
        }

        [Test]
        public void WhenNameRootSpecifiedSubfolderNamesArePrepended ()
        {
            AreEqual("2x2.BTGR", Load(BTGR, TextureFolderPath).Name);
        }

        [Test]
        public void WhenNameRootNotSpecifiedSubfolderNamesAreNotPrepended ()
        {
            AreEqual("TTTT", Load(TTTT).Name);
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
            GetImporter(RGB8x8).crunchedCompression = true;
            Load(RGB8x8);
            IsFalse(GetImporter(RGB8x8).crunchedCompression);
        }

        private static SourceTexture Load (string texturePath, string nameRoot = null)
        {
            return new TextureLoader(nameRoot).Load(texturePath);
        }

        private static TextureImporter GetImporter (string texturePath)
        {
            return (TextureImporter)AssetImporter.GetAtPath(texturePath);
        }
    }
}
