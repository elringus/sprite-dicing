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
    /// Responsible for creating diced sprite assets from atlas texture.
    /// </summary>
    public class SpriteBuilder
    {
        private readonly float ppu;
        private readonly Vector2 defaultPivot;
        private readonly bool keepOriginalPivot;

        public SpriteBuilder (float ppu, Vector2 defaultPivot, bool keepOriginalPivot)
        {
            this.ppu = ppu;
            this.defaultPivot = defaultPivot;
            this.keepOriginalPivot = keepOriginalPivot;
        }

        public List<Sprite> Build (AtlasTexture atlasTexture)
        {
            var dicedSprites = new List<Sprite>();
            foreach (var dicedTexture in atlasTexture.DicedTextures)
                dicedSprites.Add(Build(atlasTexture, dicedTexture));
            return dicedSprites;
        }

        private Sprite Build (AtlasTexture atlasTexture, DicedTexture dicedTexture)
        {
            var vertices = new List<Vector2>();
            var uvs = new List<Vector2>();
            var triangles = new List<ushort>();
            foreach (var dicedUnit in dicedTexture.Units)
                AddDicedUnit(dicedUnit, atlasTexture.ContentToUV[dicedUnit.ContentHash]);
            var pivot = EvaluatePivot();
            var renderRect = EvaluateSpriteRect().Scale(ppu);
            return CreateSprite();

            void AddDicedUnit (DicedUnit dicedUnit, Rect uv)
            {
                AddQuad(dicedUnit.QuadVerts.min, dicedUnit.QuadVerts.max, uv.min, uv.max);
            }

            void AddQuad (Vector2 posMin, Vector2 posMax, Vector2 uvMin, Vector2 uvMax)
            {
                var startIndex = vertices.Count;

                AddVertex(new Vector2(posMin.x, posMin.y), new Vector2(uvMin.x, uvMin.y));
                AddVertex(new Vector2(posMin.x, posMax.y), new Vector2(uvMin.x, uvMax.y));
                AddVertex(new Vector2(posMax.x, posMax.y), new Vector2(uvMax.x, uvMax.y));
                AddVertex(new Vector2(posMax.x, posMin.y), new Vector2(uvMax.x, uvMin.y));

                AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                AddTriangle(startIndex + 2, startIndex + 3, startIndex);
            }

            void AddVertex (Vector2 position, Vector2 uv)
            {
                vertices.Add(position);
                uvs.Add(uv);
            }

            void AddTriangle (int idx0, int idx1, int idx2)
            {
                triangles.Add((ushort)idx0);
                triangles.Add((ushort)idx1);
                triangles.Add((ushort)idx2);
            }

            Vector2 TrimVertices ()
            {
                var rect = EvaluateSpriteRect();
                if (rect.min.x > 0 || rect.min.y > 0)
                    for (int i = 0; i < vertices.Count; i++)
                        vertices[i] -= rect.min;
                if (!dicedTexture.Source.Sprite) return -rect.min / rect.size;
                return (dicedTexture.Source.Sprite.pivot / ppu - rect.min) / rect.size;
            }

            Rect EvaluateSpriteRect ()
            {
                var minVertPos = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
                var maxVertPos = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
                var spriteSizeX = Mathf.Abs(maxVertPos.x - minVertPos.x);
                var spriteSizeY = Mathf.Abs(maxVertPos.y - minVertPos.y);
                var spriteSize = new Vector2(spriteSizeX, spriteSizeY);
                return new Rect(minVertPos, spriteSize);
            }

            Vector2 EvaluatePivot ()
            {
                var originalPivot = TrimVertices();
                var result = keepOriginalPivot ? originalPivot : defaultPivot;
                ApplyPivotChange(result);
                return result;
            }

            void ApplyPivotChange (Vector2 newPivot)
            {
                var spriteRect = EvaluateSpriteRect();
                var curPivot = new Vector2(-spriteRect.min.x / spriteRect.size.x, -spriteRect.min.y / spriteRect.size.y);
                var curDeltaX = spriteRect.size.x * curPivot.x;
                var curDeltaY = spriteRect.size.y * curPivot.y;
                var newDeltaX = spriteRect.size.x * newPivot.x;
                var newDeltaY = spriteRect.size.y * newPivot.y;
                var deltaPos = new Vector2(newDeltaX - curDeltaX, newDeltaY - curDeltaY);

                for (int i = 0; i < vertices.Count; i++)
                    vertices[i] -= deltaPos;
            }

            Sprite CreateSprite ()
            {
                // Public sprite ctor won't allow using a rect that is larger than the texture:
                // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/2D/Common/ScriptBindings/Sprites.bindings.cs#L271
                var sprite = typeof(Sprite).GetMethod("CreateSprite", BindingFlags.NonPublic | BindingFlags.Static)
                    // (texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape)
                    ?.Invoke(null, new object[] { atlasTexture.Texture, renderRect, pivot, ppu, (uint)0, SpriteMeshType.Tight, Vector4.zero, false }) as Sprite;
                if (sprite is null) throw new Exception($"Failed to create `{dicedTexture.Source.Name}` sprite.");
                sprite.name = dicedTexture.Source.Name;
                sprite.SetVertexCount(vertices.Count);
                sprite.SetIndices(new NativeArray<ushort>(triangles.ToArray(), Allocator.Temp));
                sprite.SetVertexAttribute(VertexAttribute.Position, new NativeArray<Vector3>(vertices.Select(v => new Vector3(v.x, v.y)).ToArray(), Allocator.Temp));
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp));
                return sprite;
            }
        }
    }
}
