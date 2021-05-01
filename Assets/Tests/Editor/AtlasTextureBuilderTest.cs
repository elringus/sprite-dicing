using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;

namespace SpriteDicing.Test
{
    public class AtlasTextureBuilderTest
    {
        [Test]
        public void WhenArgumentNullExceptionIsThrown ()
        {
            // ReSharper disable ObjectCreationAsStatement
            Throws<ArgumentNullException>(() => new AtlasTexture(null, new Dictionary<Hash128, Rect>(), Array.Empty<DicedTexture>()));
            Throws<ArgumentNullException>(() => new AtlasTexture(Texture2D.redTexture, null, Array.Empty<DicedTexture>()));
            Throws<ArgumentNullException>(() => new AtlasTexture(Texture2D.redTexture, new Dictionary<Hash128, Rect>(), null));
            // ReSharper restore ObjectCreationAsStatement
        }
    }
}
