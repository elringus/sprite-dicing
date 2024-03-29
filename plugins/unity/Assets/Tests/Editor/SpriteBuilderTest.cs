// using System;
// using System.Collections.Generic;
// using System.Linq;
// using NUnit.Framework;
// using UnityEngine;
// using static NUnit.Framework.Assert;
// using static SpriteDicing.Test.Helpers.Paths;
//
// namespace SpriteDicing.Test
// {
//     public class SpriteBuilderTest
//     {
//         [Test]
//         public void WhenInvalidArgumentExceptionIsThrown ()
//         {
//             Throws<ArgumentException>(() => Build(Array.Empty<string>(), ppu: 0));
//         }
//
//         [Test]
//         public void CustomPivotIsAppliedToSprite ()
//         {
//             var pivot = new Vector2(.95f, -.15f);
//             AreEqual(pivot, Build(new[] { B }, pivot: pivot)[0].pivot);
//         }
//
//         [Test]
//         public void OriginalPivotIsPreserved ()
//         {
//             AreEqual(Vector2.one, Build(new[] { RGB4x4 }, keepOriginalPivot: true)[0].pivot);
//         }
//
//         [Test]
//         public void SpriteSizeIsPreserved ()
//         {
//             AreEqual(Vector2.one, Build(new[] { B })[0].rect.size);
//             AreEqual(new Vector2(2, 2), Build(new[] { BGRT })[0].rect.size);
//             AreEqual(new Vector2(1, 3), Build(new[] { RGB1x3 })[0].rect.size);
//             AreEqual(new Vector2(3, 1), Build(new[] { RGB3x1 })[0].rect.size);
//             AreEqual(new Vector2(4, 4), Build(new[] { RGB4x4 })[0].rect.size);
//         }
//
//         [Test]
//         public void SpriteVertsAreScaledByPPU ()
//         {
//             AreEqual(Vector2.one, (Vector2)Build(new[] { B }, ppu: 1)[0].bounds.size);
//             AreEqual(Vector2.one * .5f, (Vector2)Build(new[] { B }, ppu: 2)[0].bounds.size);
//         }
//
//         [Test]
//         public void TransparentAreasAreTrimmed ()
//         {
//             AreEqual(new Vector2(1, 2), Build(new[] { BTGT })[0].rect.size);
//         }
//
//         [Test]
//         public void WhenEmptyAndTrimEnabledSpriteHasDefaultRect ()
//         {
//             var sprite = Build(new[] { TTTT }, trim: true)[0];
//             AreEqual(Vector2.zero, sprite.rect.position);
//             AreEqual(Vector2.one, sprite.rect.size);
//         }
//
//         [Test]
//         public void WhenEmptyAndTrimEnabledSpriteHasSimpleMesh ()
//         {
//             var sprite = Build(new[] { TTTT }, trim: true)[0];
//             AreEqual(1, sprite.vertices.Length);
//             AreEqual(Vector2.zero, sprite.vertices[0]);
//             AreEqual(1, sprite.triangles.Length);
//             AreEqual(0, sprite.triangles[0]);
//             AreEqual(1, sprite.uv.Length);
//             AreEqual(Vector2.zero, sprite.uv[0]);
//         }
//
//         [Test]
//         public void WhenEmptyAndTrimDisabledSpriteHasNormalMesh ()
//         {
//             var sprite = Build(new[] { TTTT }, trim: false)[0];
//             AreEqual(16, sprite.vertices.Length);
//             AreEqual(24, sprite.triangles.Length);
//             AreEqual(16, sprite.uv.Length);
//         }
//
//         private static List<Sprite> Build (string[] texturePaths, float uvInset = 0, bool square = false, bool pot = false, int sizeLimit = 8,
//             int unitSize = 1, int padding = 0, float ppu = 1, Vector2 pivot = default, bool keepOriginalPivot = false, bool trim = true)
//         {
//             // TODO: Don't use loader, dicer and packer here; create mock atlas textures instead.
//             var textureLoader = new TextureLoader();
//             var sourceTextures = texturePaths.Select(textureLoader.Load);
//             var dicer = new TextureDicer(unitSize, padding, trim);
//             var dicedTextures = sourceTextures.Select(dicer.Dice);
//             var atlasTextures = packer.Pack(dicedTextures);
//             var builder = new SpriteBuilder(ppu, pivot, keepOriginalPivot);
//             var sprites = new List<Sprite>();
//             foreach (var atlasTexture in atlasTextures)
//             foreach (var dicedTexture in atlasTexture.DicedTextures)
//                 sprites.Add(builder.Build(atlasTexture, dicedTexture));
//             return sprites;
//         }
//     }
// }
