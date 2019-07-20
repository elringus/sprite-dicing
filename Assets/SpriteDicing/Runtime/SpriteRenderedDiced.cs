using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Rendering;

namespace SpriteDicing.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteRenderedDiced : MonoBehaviour
    {
        [SerializeField]private SpriteRenderer spriteRenderer;
        public DicedSprite dicedSprite;
    
        // Start is called before the first frame update
        void Start()
        {
            RenderSpriteMesh();
        }

        private void RenderSpriteMesh()
        {
            if(spriteRenderer == null)
                spriteRenderer = GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;

            if (dicedSprite == null) return;
            var spriteGenerated =
                Sprite.Create(dicedSprite.AtlasTexture, dicedSprite.EvaluateSpriteRect(), dicedSprite.Pivot, 100);
            spriteGenerated.name = name;
            spriteGenerated.SetVertexCount(dicedSprite.Vertices.Count);
            spriteGenerated.SetIndices(new NativeArray<ushort>(dicedSprite.TrianglesData.Select(t => (ushort) t).ToArray(),
                Allocator.Temp));
            spriteGenerated.SetVertexAttribute(VertexAttribute.Position,
                new NativeArray<Vector3>(dicedSprite.VerticesData.Select(v => new Vector3(v.x, v.y, 0)).ToArray(),
                    Allocator.Temp));
            spriteGenerated.SetVertexAttribute(VertexAttribute.TexCoord0,
                new NativeArray<Vector2>(dicedSprite.UVsData.ToArray(), Allocator.Temp));
            if (spriteRenderer != null) spriteRenderer.sprite = spriteGenerated;
        }

        private void OnValidate()
        {
            RenderSpriteMesh();
        }
    }
}