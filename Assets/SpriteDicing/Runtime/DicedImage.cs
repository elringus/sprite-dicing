using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SpriteDicing.Runtime
{
    [RequireComponent(typeof(Image))]
    public class DicedImage : MonoBehaviour
    {
        [SerializeField]private Image imageComponent;
        public DicedSprite dicedSprite;
    
        // Start is called before the first frame update
        void Start()
        {
            RenderSpriteMesh();
        }

        private void RenderSpriteMesh()
        {
            if(imageComponent == null)
                imageComponent = GetComponent(typeof(Image)) as Image;

            if (dicedSprite == null) return;
            var spriteGenerated =
                Sprite.Create(dicedSprite.AtlasTexture, dicedSprite.EvaluateSpriteRect(100f), new Vector2(0.5f, 0.5f) , 100f);
            spriteGenerated.name = name;
            spriteGenerated.SetVertexCount(dicedSprite.Vertices.Count);
            spriteGenerated.SetIndices(new NativeArray<ushort>(dicedSprite.TrianglesData.Select(t => (ushort) t).ToArray(),
                Allocator.Temp));
            spriteGenerated.SetVertexAttribute(VertexAttribute.Position,
                new NativeArray<Vector3>(dicedSprite.VerticesData.Select(v => new Vector3(v.x, v.y, 0)).ToArray(),
                    Allocator.Temp));
            spriteGenerated.SetVertexAttribute(VertexAttribute.TexCoord0,
                new NativeArray<Vector2>(dicedSprite.UVsData.ToArray(), Allocator.Temp));
            if (imageComponent != null) imageComponent.sprite = spriteGenerated;
        }

        private void OnValidate()
        {
            RenderSpriteMesh();
        }
    }
}
