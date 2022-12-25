using System;
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
    /// Responsible for creating sprite asset from the diced data.
    /// </summary>
    public class SpriteBuilder
    {
        // Public sprite ctor won't allow rect larger than the texture. (https://git.io/J31tI)
        private static readonly MethodInfo createSpriteMethod =
            typeof(Sprite).GetMethod("CreateSprite", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly float ppu;
        private readonly Vector2 defaultPivot;
        private readonly bool keepOriginalPivot;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<ushort> triangles = new List<ushort>();

        public SpriteBuilder (float ppu, Vector2 defaultPivot, bool keepOriginalPivot)
        {
            this.ppu = ppu <= 0 ? throw new ArgumentException("PPU should be greater than zero.") : ppu;
            this.defaultPivot = defaultPivot;
            this.keepOriginalPivot = keepOriginalPivot;
        }

        public Sprite Build (AtlasTexture atlasTexture, DicedTexture dicedTexture)
        {
            ResetState();
            foreach (var unit in dicedTexture.Units)
                AddDicedUnit(unit, atlasTexture.ContentToUV[unit.ContentHash]);
            var spriteRect = EvaluateSpriteRect(1);
            var originalPivot = GetOriginalPivot(spriteRect, dicedTexture.Source.Pivot);
            var pivot = keepOriginalPivot ? originalPivot : defaultPivot;
            ApplyPivotChange(spriteRect, pivot);
            var renderRect = EvaluateSpriteRect(ppu);
            return CreateSprite(dicedTexture.Source.Name, atlasTexture.Texture, pivot, renderRect);
        }

        private void ResetState ()
        {
            vertices.Clear();
            uvs.Clear();
            triangles.Clear();
        }

        private void AddDicedUnit (DicedUnit unit, Rect uv)
        {
            var rect = ScaleRect(unit.QuadVerts);
            AddQuad(rect.min, rect.max, uv.min, uv.max);
        }

        private Rect ScaleRect (RectInt rect)
        {
            var scale = 1f / ppu;
            return new Rect((Vector2)rect.position * scale, (Vector2)rect.size * scale);
        }

        private void AddQuad (Vector2 posMin, Vector2 posMax, Vector2 uvMin, Vector2 uvMax)
        {
            var startIndex = vertices.Count;

            AddVertex(new Vector2(posMin.x, posMin.y), new Vector2(uvMin.x, uvMin.y));
            AddVertex(new Vector2(posMin.x, posMax.y), new Vector2(uvMin.x, uvMax.y));
            AddVertex(new Vector2(posMax.x, posMax.y), new Vector2(uvMax.x, uvMax.y));
            AddVertex(new Vector2(posMax.x, posMin.y), new Vector2(uvMax.x, uvMin.y));

            AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        private void AddVertex (Vector2 position, Vector2 uv)
        {
            vertices.Add(position);
            uvs.Add(uv);
        }

        private void AddTriangle (int idx0, int idx1, int idx2)
        {
            triangles.Add((ushort)idx0);
            triangles.Add((ushort)idx1);
            triangles.Add((ushort)idx2);
        }

        private Rect EvaluateSpriteRect (float scale)
        {
            if (vertices.Count == 0) return new Rect(Vector2.zero, Vector2.one * scale);

            var minVertPos = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
            var maxVertPos = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
            var spriteSizeX = Mathf.Abs(maxVertPos.x - minVertPos.x);
            var spriteSizeY = Mathf.Abs(maxVertPos.y - minVertPos.y);
            var spriteSize = new Vector2(spriteSizeX, spriteSizeY);
            return new Rect(minVertPos * scale, spriteSize * scale);
        }

        private Vector2 GetOriginalPivot (Rect rect, Vector2? sourcePivot)
        {
            if (!sourcePivot.HasValue) return -rect.min / rect.size;
            return (sourcePivot.Value / ppu - rect.min) / rect.size;
        }

        private void ApplyPivotChange (Rect rect, Vector2 newPivot)
        {
            var curPivot = new Vector2(-rect.min.x / rect.size.x, -rect.min.y / rect.size.y);
            var deltaPos = (Vector3)(rect.size * newPivot - rect.size * curPivot);
            for (int i = 0; i < vertices.Count; i++)
                vertices[i] -= deltaPos;
        }

        private Sprite CreateSprite (string name, Texture texture, Vector2 pivot, Rect renderRect)
        {
            #if UNITY_2022_2_OR_NEWER
            // (texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape, secondaryTexture)
            var args = new object[] { texture, renderRect, pivot, ppu, (uint)0, SpriteMeshType.Tight, Vector4.zero, false, null };
            #else
            // (texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape)
            var args = new object[] { texture, renderRect, pivot, ppu, (uint)0, SpriteMeshType.Tight, Vector4.zero, false };
            #endif
            var sprite = (Sprite)createSpriteMethod.Invoke(null, args);
            sprite.name = name;
            sprite.SetVertexCount(vertices.Count);
            sprite.SetIndices(new NativeArray<ushort>(triangles.ToArray(), Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.Position, new NativeArray<Vector3>(vertices.ToArray(), Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.TexCoord0, new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp));
            return sprite;
        }
    }
}
