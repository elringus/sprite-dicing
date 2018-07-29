using System.Collections.Generic;
using System.Linq;
using UnityCommon;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Contains data for rendering a diced sprite.
    /// </summary>
    public class DicedSprite : ScriptableObject
    {
        /// <summary>
        /// Event invoked when sprite data has been modified.
        /// </summary>
        public event System.Action<DicedSprite> OnModified;

        /// <summary>
        /// Name of the diced sprite object.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Relative pivot point position in 0 to 1 range, counting from the bottom-left corner.
        /// </summary>
        public Vector2 Pivot { get { return pivot; } set { if (pivot != value) { pivot = value; HandlePivotChange(); } } }

        /// <summary>
        /// Reference to the atlas texture where the dices of the original sprite texture are stored.
        /// </summary>
        public Texture2D AtlasTexture => atlasTexture;

        /// <summary>
        /// UV rects to sample diced units on the atlas texture.
        /// </summary>
        public List<Vector2> UVs => uvs;

        /// <summary>
        /// Vertice positions (in local 2D space) to generate diced sprite mesh.
        /// </summary>
        public List<Vector2> Vertices => vertices;

        [SerializeField, ReadOnly] private Texture2D atlasTexture;
        [SerializeField, ReadOnly] private List<Vector2> vertices;
        [SerializeField, ReadOnly] private List<Vector2> uvs;
        [SerializeField, ReadOnly] private List<int> triangles;
        [SerializeField] private Vector2 pivot;

        private const int meshVerticesLimit = 65000; // Unity limitation.

        private void OnValidate ()
        {
            var onModifiedCalled = false;

            onModifiedCalled = HandlePivotChange();

            if (!onModifiedCalled)
                OnModified?.Invoke(this);
        }

        /// <summary>
        /// Creates instance of a diced sprite object.
        /// </summary>
        /// <param name="name">Name of the diced sprite object. Used to find it among others in the parent atlas.</param>
        /// <param name="atlasTexture">Reference to the atlas texture.</param>
        /// <param name="dicedUnits">List of the diced units used to build this sprite.</param>
        /// <param name="pivot">Sprite pivot point in its local space.</param>
        /// <param name="keepOriginalPivot">Whether to preserve original sprite position by correcting its pivot.</param>
        public static DicedSprite CreateInstance (string name, Texture2D atlasTexture, List<DicedUnit> dicedUnits,
            Vector2 pivot = default(Vector2), bool keepOriginalPivot = true)
        {
            var dicedSprite = ScriptableObject.CreateInstance<DicedSprite>();

            dicedSprite.atlasTexture = atlasTexture;
            dicedSprite.name = name;
            dicedSprite.vertices = new List<Vector2>();
            dicedSprite.uvs = new List<Vector2>();
            dicedSprite.triangles = new List<int>();

            foreach (var dicedUnit in dicedUnits)
                dicedSprite.AddDicedUnit(dicedUnit);

            var originalPivot = dicedSprite.TrimVertices();
            dicedSprite.Pivot = keepOriginalPivot ? originalPivot : pivot;

            return dicedSprite;
        }

        /// <summary>
        /// Populates provided mesh with the data to construct sprite's shape and map atlas texture UVs.
        /// </summary>
        public void FillMesh (Mesh mesh)
        {
            if (!mesh) return;

            mesh.Clear();

            if (vertices.Count >= meshVerticesLimit)
            {
                Debug.LogError($"Mesh can't have more than {meshVerticesLimit} vertices. " +
                    "Consider increasing Dice Unit Size of the Diced Sprite Atlas.");
                return;
            }

            mesh.SetVertices(vertices.Select(v2 => new Vector3(v2.x, v2.y)).ToList());
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Calculates sprite rectangle using vertex data.
        /// </summary>
        public Rect EvaluateSpriteRect ()
        {
            var minVertPos = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
            var maxVertPos = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
            var spriteSizeX = Mathf.Abs(maxVertPos.x - minVertPos.x);
            var spriteSizeY = Mathf.Abs(maxVertPos.y - minVertPos.y);
            var spriteSize = new Vector2(spriteSizeX, spriteSizeY);
            return new Rect(minVertPos, spriteSize);
        }

        private void AddDicedUnit (DicedUnit dicedUnit)
        {
            AddQuad(dicedUnit.QuadVerts.min, dicedUnit.QuadVerts.max, dicedUnit.QuadUVs.min, dicedUnit.QuadUVs.max);
        }

        private void AddQuad (Vector2 posMin, Vector2 posMax, Vector2 uvMin, Vector2 uvMax)
        {
            var startIndex = vertices.Count;

            AddVertice(new Vector3(posMin.x, posMin.y, 0), new Vector2(uvMin.x, uvMin.y));
            AddVertice(new Vector3(posMin.x, posMax.y, 0), new Vector2(uvMin.x, uvMax.y));
            AddVertice(new Vector3(posMax.x, posMax.y, 0), new Vector2(uvMax.x, uvMax.y));
            AddVertice(new Vector3(posMax.x, posMin.y, 0), new Vector2(uvMax.x, uvMin.y));

            AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        private void AddVertice (Vector2 position, Vector2 uv)
        {
            vertices.Add(position);
            uvs.Add(uv);
        }

        private void AddTriangle (int idx0, int idx1, int idx2)
        {
            triangles.Add(idx0);
            triangles.Add(idx1);
            triangles.Add(idx2);
        }

        /// <summary>
        /// Repositions all the vertices so that they start at the local origin (0, 0).
        /// </summary>
        /// <returns>Pivot point to preserve original position of the sprite.</returns>
        private Vector2 TrimVertices ()
        {
            var spriteRect = EvaluateSpriteRect();
            if (spriteRect.min.x > 0 || spriteRect.min.y > 0)
                for (int i = 0; i < vertices.Count; i++)
                    vertices[i] -= spriteRect.min;

            OnModified?.Invoke(this);

            var pivotX = spriteRect.min.x / spriteRect.size.x;
            var pivotY = spriteRect.min.y / spriteRect.size.y;
            return new Vector2(-pivotX, -pivotY);
        }

        /// <summary>
        /// Corrects geometry data to to match current pivot value.
        /// </summary>
        /// <returns>Whether geometry data has been changed and OnModified event called.</returns>
        private bool HandlePivotChange ()
        {
            var spriteRect = EvaluateSpriteRect();

            var curPivot = new Vector2(-spriteRect.min.x / spriteRect.size.x, -spriteRect.min.y / spriteRect.size.y);
            if (curPivot == Pivot) return false;

            var curDeltaX = spriteRect.size.x * curPivot.x;
            var curDeltaY = spriteRect.size.y * curPivot.y;
            var newDeltaX = spriteRect.size.x * Pivot.x;
            var newDeltaY = spriteRect.size.y * Pivot.y;

            var deltaPos = new Vector2(newDeltaX - curDeltaX, newDeltaY - curDeltaY);

            for (int i = 0; i < vertices.Count; i++)
                vertices[i] -= deltaPos;

            OnModified?.Invoke(this);
            return true;
        }

    }
}
