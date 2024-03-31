using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace SpriteDicing
{
    /// <summary>
    /// Creates sprite assets from the diced data.
    /// </summary>
    public class SpriteBuilder
    {
        // Public sprite ctor won't allow rect larger than the texture. (https://git.io/J31tI)
        private static readonly MethodInfo createSpriteMethod =
            typeof(Sprite).GetMethod("CreateSprite", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly float ppu;
        private readonly IReadOnlyList<Texture2D> atlases;

        public SpriteBuilder (float ppu, IReadOnlyList<Texture2D> atlases)
        {
            this.ppu = ppu;
            this.atlases = atlases;
        }

        public Sprite Build (Native.DicedSprite diced)
        {
            var texture = atlases[diced.Atlas];
            var rect = new Rect(diced.Rect.X * ppu, diced.Rect.Y * ppu, diced.Rect.Width * ppu, diced.Rect.Height * ppu);
            var pivot = new Vector2(diced.Pivot.X, diced.Pivot.Y);
            var vertices = diced.Vertices.Select(v => new Vector3(v.X, diced.Rect.Height * (1 - pivot.y * 2) - v.Y)).ToArray();
            var uvs = diced.UVs.Select(v => new Vector2(v.U, 1 - v.V)).ToArray();
            var triangles = diced.Indices.Select(i => (ushort)i).ToArray();

            var sprite = CreateSprite(texture, pivot, rect);
            sprite.name = diced.Id;
            sprite.SetVertexCount(vertices.Length);
            sprite.SetIndices(new NativeArray<ushort>(triangles, Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.Position, new NativeArray<Vector3>(vertices, Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.TexCoord0, new NativeArray<Vector2>(uvs, Allocator.Temp));

            return sprite;
        }

        private Sprite CreateSprite (Texture texture, Vector2 pivot, Rect rect)
        {
            // (texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape, secondaryTexture)
            var args = new object[] { texture, rect, pivot, ppu, (uint)0, SpriteMeshType.Tight, Vector4.zero, false, null };
            return (Sprite)createSpriteMethod.Invoke(null, args);
        }
    }
}
