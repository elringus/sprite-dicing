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
        // Public sprite ctor won't allow using a rect that is larger than the texture.
        private static readonly MethodInfo createSpriteMethod = GetCreateSpriteMethod();

        private readonly float ppu;
        private readonly Vector2 defaultPivot;
        private readonly bool keepOriginalPivot;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<ushort> triangles = new List<ushort>();

        public SpriteBuilder (float ppu, Vector2 defaultPivot, bool keepOriginalPivot)
        {
            this.ppu = ppu;
            this.defaultPivot = defaultPivot;
            this.keepOriginalPivot = keepOriginalPivot;
        }

        public Sprite Build (AtlasTexture atlasTexture, DicedTexture dicedTexture)
        {
            ResetState();
            foreach (var unit in dicedTexture.Units)
                AddDicedUnit(unit, atlasTexture.ContentToUV[unit.ContentHash]);
            var originalPivot = TrimVertices(dicedTexture.Source.Sprite);
            var pivot = keepOriginalPivot ? originalPivot : defaultPivot;
            ApplyPivotChange(pivot);
            var renderRect = EvaluateSpriteRect().Scale(ppu);
            return CreateSprite(dicedTexture.Source.Name, atlasTexture.Texture, pivot, renderRect);
        }

        private static MethodInfo GetCreateSpriteMethod ()
        {
            var method = typeof(Sprite).GetMethod("CreateSprite", BindingFlags.NonPublic | BindingFlags.Static);
            return method ?? throw new Exception("Failed to get Unity's internal create sprite method.");
        }

        private void AddDicedUnit (DicedUnit unit, Rect uv)
        {
            AddQuad(unit.QuadVerts.min, unit.QuadVerts.max, uv.min, uv.max);
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

        private Rect EvaluateSpriteRect ()
        {
            var minVertPos = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
            var maxVertPos = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
            var spriteSizeX = Mathf.Abs(maxVertPos.x - minVertPos.x);
            var spriteSizeY = Mathf.Abs(maxVertPos.y - minVertPos.y);
            var spriteSize = new Vector2(spriteSizeX, spriteSizeY);
            return new Rect(minVertPos, spriteSize);
        }

        private Vector2 TrimVertices (Sprite sourceSprite)
        {
            var rect = EvaluateSpriteRect();
            if (rect.min.x > 0 || rect.min.y > 0)
                for (int i = 0; i < vertices.Count; i++)
                    vertices[i] -= (Vector3)rect.min;
            if (!sourceSprite) return -rect.min / rect.size;
            return (sourceSprite.pivot / ppu - rect.min) / rect.size;
        }

        private void ApplyPivotChange (Vector2 newPivot)
        {
            var spriteRect = EvaluateSpriteRect();
            var curPivot = new Vector2(-spriteRect.min.x / spriteRect.size.x, -spriteRect.min.y / spriteRect.size.y);
            var curDeltaX = spriteRect.size.x * curPivot.x;
            var curDeltaY = spriteRect.size.y * curPivot.y;
            var newDeltaX = spriteRect.size.x * newPivot.x;
            var newDeltaY = spriteRect.size.y * newPivot.y;
            var deltaPos = new Vector3(newDeltaX - curDeltaX, newDeltaY - curDeltaY);
            for (int i = 0; i < vertices.Count; i++)
                vertices[i] -= deltaPos;
        }

        private Sprite CreateSprite (string name, Texture texture, Vector2 pivot, Rect renderRect)
        {
            // (texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape)
            var args = new object[] { texture, renderRect, pivot, ppu, (uint)0, SpriteMeshType.Tight, Vector4.zero, false };
            var sprite = (Sprite)createSpriteMethod.Invoke(null, args);
            sprite.name = name;
            sprite.SetVertexCount(vertices.Count);
            sprite.SetIndices(new NativeArray<ushort>(triangles.ToArray(), Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.Position, new NativeArray<Vector3>(vertices.ToArray(), Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.TexCoord0, new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp));
            return sprite;
        }

        private void ResetState ()
        {
            vertices.Clear();
            uvs.Clear();
            triangles.Clear();
        }
    }
}
