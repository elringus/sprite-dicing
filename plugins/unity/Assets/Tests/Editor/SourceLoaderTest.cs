using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
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
        public void LoadsBytesOfTheSourceTexture ()
        {
            CollectionAssert.AreEqual(File.ReadAllBytes(BGRT), Load(BGRT).Bytes);
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

        private static Native.SourceSprite Load (string texturePath, string root = TextureFolderPath,
            string separator = ".", bool keepPivot = true)
        {
            return new SourceLoader(root, separator, keepPivot).Load(texturePath);
        }

        private static TextureImporter GetImporter (string texturePath)
        {
            return (TextureImporter)AssetImporter.GetAtPath(texturePath);
        }
    }
}
