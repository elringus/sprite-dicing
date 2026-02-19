using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers.Textures;

namespace SpriteDicing.Test
{
    public class DicedSpriteAtlasTest
    {
        [Test]
        public void SpritesAreReturned ()
        {
            var r = Sprite.Create(R.texture, Rect.zero, Vector2.zero);
            var b = Sprite.Create(B.texture, Rect.zero, Vector2.zero);
            var atlas = CreateWith(new[] { r, b });
            IsTrue(atlas.Sprites.Contains(r));
            IsTrue(atlas.Sprites.Contains(b));
        }

        [Test]
        public void TexturesAreReturned ()
        {
            var atlas = CreateWith(null, new[] { R.texture, B.texture });
            IsTrue(atlas.Textures.Contains(R.texture));
            IsTrue(atlas.Textures.Contains(B.texture));
        }

        [Test]
        public void WhenSpriteNotFoundNullIsReturned ()
        {
            IsNull(CreateWith().GetSprite(""));
        }

        [Test]
        public void CanGetSpriteByName ()
        {
            var sprite = Sprite.Create(B.texture, Rect.zero, Vector2.zero);
            sprite.name = nameof(CanGetSpriteByName);
            var atlas = CreateWith(new[] { sprite });
            AreEqual(sprite, atlas.GetSprite(nameof(CanGetSpriteByName)));
        }

        private static DicedSpriteAtlas CreateWith (IList<Sprite> sprites = default, IList<Texture2D> textures = default)
        {
            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.NonPublic;
            var atlas = ScriptableObject.CreateInstance<DicedSpriteAtlas>();
            if (sprites != null) atlas.GetType().GetField("sprites", bindings)?.SetValue(atlas, sprites.ToList());
            if (textures != null) atlas.GetType().GetField("textures", bindings)?.SetValue(atlas, textures.ToList());
            return atlas;
        }
    }
}
