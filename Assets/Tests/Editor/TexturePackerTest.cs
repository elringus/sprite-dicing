using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers.Textures;

namespace SpriteDicing.Test
{
    public class TexturePackerTest
    {
        [Test]
        public void WhenInvalidArgumentExceptionIsThrown ()
        {
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), uvInset: -.1f));
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), uvInset: .51f));
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), sizeLimit: 0));
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), unitSize: 0));
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), sizeLimit: 1, unitSize: 2));
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), padding: -1));
            Throws<ArgumentException>(() => Pack(Array.Empty<Texture2D>(), unitSize: 1, padding: 2));
            // ReSharper disable ObjectCreationAsStatement
            Throws<ArgumentNullException>(() => new TexturePacker(null, 0, false, false, 1, 1, 0));
            Throws<ArgumentNullException>(() => new TexturePacker(new MockTextureSerializer(), 0, false, false, 1, 1, 0).Pack(null));
            Throws<ArgumentNullException>(() => new AtlasTexture(null, new Dictionary<Hash128, Rect>(), Array.Empty<DicedTexture>()));
            Throws<ArgumentNullException>(() => new AtlasTexture(Texture2D.redTexture, null, Array.Empty<DicedTexture>()));
            Throws<ArgumentNullException>(() => new AtlasTexture(Texture2D.redTexture, new Dictionary<Hash128, Rect>(), null));
            // ReSharper restore ObjectCreationAsStatement
        }

        [Test]
        public void WhenNoTexturesToPackEmptyAtlasCollectionIsReturned ()
        {
            IsEmpty(Pack(Array.Empty<Texture2D>()));
        }

        [Test]
        public void DicedTexturesArePreserved ()
        {
            var dicedTexture = new DicedTexture(new SourceTexture("B", B), Array.Empty<DicedUnit>());
            var atlasTexture = new TexturePacker(new MockTextureSerializer(), 0, false, false, 1, 1, 0).Pack(new[] { dicedTexture })[0];
            AreEqual(dicedTexture, atlasTexture.DicedTextures[0]);
        }

        [Test]
        public void WhenContentDoesntFitMultipleAtlasTexturesAreCreated ()
        {
            AreEqual(2, Pack(new[] { B, R }, sizeLimit: 1).Count);
        }

        [Test]
        public void WhenContentFromSingleTextureDoesntFitExceptionIsThrown ()
        {
            Throws<InvalidOperationException>(() => Pack(new[] { RGB4x4 }, sizeLimit: 1));
        }

        [Test]
        public void WhenForcingSquareAtlasTextureIsSquare ()
        {
            var atlas = Pack(new[] { RGB4x4, B }, sizeLimit: 4, square: true)[0];
            IsTrue(atlas.Texture.width == atlas.Texture.height);
        }

        [Test]
        public void WhenNotForcingSquareAndSquareIsNotOptimalAtlasTextureIsNotSquare ()
        {
            var atlas = Pack(new[] { RGB4x4, B }, sizeLimit: 4, square: false)[0];
            IsFalse(atlas.Texture.width == atlas.Texture.height);
        }

        [Test]
        public void WhenNotForcingSquareButSquareIsOptimalAtlasTextureIsSquare ()
        {
            var atlas = Pack(new[] { RGB4x4, B }, sizeLimit: 6, square: false, padding: 1)[0];
            IsTrue(atlas.Texture.width == atlas.Texture.height);
        }

        [Test]
        public void BorderUVsAreCropped ()
        {
            var uv = Pack(new[] { B }, padding: 1, unitSize: 2)[0].ContentToUV.First().Value;
            AreEqual(new Rect(.25f, .25f, .25f, .25f), uv);
        }

        [Test]
        public void AtlasUVsAreMappedCorrectly ()
        {
            AreEqual(new Rect(0, 0, 1, 1), Pack(new[] { B })[0].ContentToUV.First().Value);
        }

        [Test]
        public void UVInsetInsetsTheAtlasUVs ()
        {
            AreEqual(new Rect(.1f, .1f, .8f, .8f), Pack(new[] { B }, uvInset: .2f)[0].ContentToUV.First().Value);
        }

        [Test]
        public void SlackPixelsAreClear ()
        {
            var atlas = Pack(new[] { RGB4x4, B }, sizeLimit: 4, square: true)[0];
            AreEqual(Color.clear, atlas.Texture.GetPixel(4, 1));
            AreEqual(Color.clear, atlas.Texture.GetPixel(4, 2));
            AreEqual(Color.clear, atlas.Texture.GetPixel(4, 3));
        }

        private static List<AtlasTexture> Pack (Texture2D[] textures, float uvInset = 0, bool square = false,
            bool pot = false, int sizeLimit = 8, int unitSize = 1, int padding = 0)
        {
            // TODO: Don't use dicer here; create mock diced textures instead.
            var dicer = new TextureDicer(unitSize, padding, true);
            var dicedTextures = textures.Select(t => new SourceTexture(t.name, t)).Select(dicer.Dice);
            var serializer = new MockTextureSerializer();
            var packer = new TexturePacker(serializer, uvInset, square, pot, sizeLimit, unitSize, padding);
            return packer.Pack(dicedTextures);
        }
    }
}
