using System.Collections.Generic;
using System.Linq;
using UnityCommon;
using UnityEngine;
using UnityEngine.Events;

public class OnDicedSpriteModified : UnityEvent<DicedSprite> { }

/// <summary>
/// Contains data for rendering a diced sprite.
/// </summary>
public class DicedSprite : ScriptableObject
{
    /// <summary>
    /// Event executed when sprite data has been modified.
    /// </summary>
    public readonly OnDicedSpriteModified OnModified = new OnDicedSpriteModified();

    /// <summary>
    /// Name of the diced sprite object.
    /// </summary>
    public string Name { get { return name; } }

    /// <summary>
    /// Sprite pivot point in its local space.
    /// </summary>
    public Vector2 Pivot { get { return _pivot; } set { SetPivot(value); } }

    /// <summary>
    /// Reference to the atlas texture where the dices of the original sprite texture are stored.
    /// </summary>
    public Texture2D AtlasTexture { get { return atlasTexture; } }

    [SerializeField, ReadOnly] private Texture2D atlasTexture;
    [SerializeField, ReadOnly] private List<Vector3> vertices;
    [SerializeField, ReadOnly] private List<Vector2> uvs;
    [SerializeField, ReadOnly] private List<int> triangles;

    [SerializeField] private Vector2 _pivot;

    private const int MESH_VERTICES_LIMIT = 65000; // Unity limitation.

    /// <summary>
    /// Creates instance of a diced sprite object.
    /// </summary>
    /// <param name="name">Name of the diced sprite object. Used to find it among others in the parent atlas.</param>
    /// <param name="atlasTexture">Reference to the atlas texture.</param>
    /// <param name="dicedUnits">List of the diced units used to build this sprite.</param>
    /// <param name="pivot">Sprite pivot point in its local space.</param>
    public static DicedSprite CreateInstance (string name, Texture2D atlasTexture, List<DicedUnit> dicedUnits, Vector2 pivot)
    {
        var dicedSprite = ScriptableObject.CreateInstance<DicedSprite>();

        dicedSprite.atlasTexture = atlasTexture;
        dicedSprite.name = name;
        dicedSprite.vertices = new List<Vector3>();
        dicedSprite.uvs = new List<Vector2>();
        dicedSprite.triangles = new List<int>();

        foreach (var dicedUnit in dicedUnits)
            dicedSprite.AddDicedUnit(dicedUnit);

        dicedSprite.TrimVertices();
        dicedSprite.SetPivot(pivot);

        return dicedSprite;
    }

    /// <summary>
    /// Populates provided mesh with the data to construct sprite's shape and map atlas texture UVs.
    /// </summary>
    public void FillMesh (Mesh mesh)
    {
        Debug.Assert(mesh);

        mesh.Clear();

        if (vertices.Count >= MESH_VERTICES_LIMIT)
        {
            Debug.LogError(string.Format("Mesh can't have more than {0} vertices. Consider increasing Dice Unit Size of the Diced Sprite Atlas.", MESH_VERTICES_LIMIT));
            return;
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Set new pivot point for the sprite.
    /// </summary>
    /// <param name="pivot">Relative pivot point position in 0 to 1 range, counting from the bottom-left corner.</param>
    public void SetPivot (Vector2 pivot)
    {
        HandlePivotChange(_pivot, pivot);
        _pivot = pivot;
    }

    private void Clear ()
    {
        vertices.Clear();
        uvs.Clear();
        triangles.Clear();
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

    private void AddVertice (Vector3 position, Vector2 uv)
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

    private void TrimVertices ()
    {
        var minPosX = vertices.Min(pos => pos.x);
        var minPosY = vertices.Min(pos => pos.y);
        var minPos = new Vector3(minPosX, minPosY);

        if (minPosX > 0 || minPosY > 0)
            for (int i = 0; i < vertices.Count; i++)
                vertices[i] -= minPos;
    }

    private void HandlePivotChange (Vector2 oldPivot, Vector2 newPivot)
    {
        if (oldPivot == newPivot) return;

        var minPosX = vertices.Min(pos => pos.x);
        var maxPosX = vertices.Max(pos => pos.x);
        var minPosY = vertices.Min(pos => pos.y);
        var maxPosY = vertices.Max(pos => pos.y);

        var oldDeltaX = Mathf.Abs(maxPosX - minPosX) * oldPivot.x;
        var oldDeltaY = Mathf.Abs(maxPosY - minPosY) * oldPivot.y;
        var newDeltaX = Mathf.Abs(maxPosX - minPosX) * newPivot.x;
        var newDeltaY = Mathf.Abs(maxPosY - minPosY) * newPivot.y;

        var deltaPos = new Vector3(newDeltaX - oldDeltaX, newDeltaY - oldDeltaY);

        for (int i = 0; i < vertices.Count; i++)
            vertices[i] -= deltaPos;

        OnModified.Invoke(this);
    }

    #if UNITY_EDITOR
    private void OnValidate ()
    {
        OnModified.Invoke(this);
    }
    #endif
}
