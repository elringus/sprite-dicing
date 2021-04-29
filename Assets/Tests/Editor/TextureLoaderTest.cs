using System;
using NUnit.Framework;
using UnityEditor;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;

namespace SpriteDicing.Test
{
    public class TextureLoaderTest
    {
        [Test]
        public void WhenNullPathExceptionIsThrown ()
        {
            Throws<ArgumentNullException>(() => new TextureLoader().Load(null));
        }

        [Test]
        public void WhenEmptyPathExceptionIsThrown ()
        {
            Throws<ArgumentNullException>(() => new TextureLoader().Load(""));
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
            IsTrue(Load(Paths.BGRT).Texture);
        }

        [Test]
        public void WhenNoAssociatedSpriteItsNull ()
        {
            IsNull(Load(Paths.RGBA1x4).Sprite);
        }

        [Test]
        public void WhenAssociatedSpriteExistItsLoaded ()
        {
            IsTrue(Load(Paths.RGBA8x8).Sprite);
        }

        [Test]
        public void WhenNameRootInvalidExceptionIsThrown ()
        {
            Throws<Exception>(() => Load(Paths.BGRT, "N/A"));
        }

        [Test]
        public void WhenNameRootSpecifiedSubfolderNamesArePrepended ()
        {
            AreEqual("2x2.BTGR", Load(Paths.BTGR, TextureFolderPath).Name);
        }

        [Test]
        public void WhenNameRootNotSpecifiedSubfolderNamesAreNotPrepended ()
        {
            AreEqual("TTTT", Load(Paths.TTTT).Name);
        }

        [Test]
        public void WhenNotReadableBecomesReadableAfterLoad ()
        {
            GetImporter(Paths.RGBA4x1).isReadable = false;
            Load(Paths.RGBA4x1);
            IsTrue(GetImporter(Paths.RGBA4x1).isReadable);
        }

        [Test]
        public void WhenCrunchedBecomesNotCrunchedAfterLoad ()
        {
            GetImporter(Paths.RGBA8x8).crunchedCompression = true;
            Load(Paths.RGBA8x8);
            IsFalse(GetImporter(Paths.RGBA8x8).crunchedCompression);
        }

        private static SourceTexture Load (string texturePath, string nameRoot = null)
        {
            return new TextureLoader().Load(texturePath, nameRoot);
        }

        private static TextureImporter GetImporter (string texturePath)
        {
            return (TextureImporter)AssetImporter.GetAtPath(texturePath);
        }
    }
}
