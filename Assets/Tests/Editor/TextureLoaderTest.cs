using System;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers;

namespace SpriteDicing.Test
{
    public class TextureLoaderTest
    {
        [Test]
        public void WhenNullPathsExceptionIsThrown ()
        {
            Throws<ArgumentNullException>(() => new TextureLoader().Load(null));
        }

        [Test]
        public void WhenEmptyPathsEmptyCollectionIsReturned ()
        {
            IsEmpty(new TextureLoader().Load(Array.Empty<string>()));
        }

        [Test]
        public void WhenTextureNotLoadedExceptionIsThrown ()
        {
            Throws<Exception>(() => Load("N/A"));
        }

        [Test]
        public void WhenNoAssociatedSpriteItsNull ()
        {
            IsNull(Load(Paths.BGRT).Sprite);
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
            AreEqual("2x2.BGRT", Load(Paths.BGRT, TextureFolderPath).Name);
        }

        [Test]
        public void WhenNameRootNotSpecifiedSubfolderNamesAreNotPrepended ()
        {
            AreEqual("BGRT", Load(Paths.BGRT).Name);
        }

        [Test]
        public void LoadedTextureAssetsAreValid ()
        {
            var textures = new TextureLoader().Load(Paths.All);
            AreEqual(Paths.All.Count, textures.Count);
            foreach (var texture in textures)
                IsTrue(texture.Texture);
        }

        private static SourceTexture Load (string texturePath, string nameRoot = null)
        {
            return new TextureLoader().Load(new[] { texturePath }, nameRoot)[0];
        }
    }
}
