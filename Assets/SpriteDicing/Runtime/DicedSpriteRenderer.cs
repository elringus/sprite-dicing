using UnityEngine;

/// <summary>
/// Renders a DicedSprite via MeshRenderer using mesh generated with the sprite's data and atlas texture.
/// </summary>
[AddComponentMenu("Rendering/Diced Sprite Renderer")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode] 
public class DicedSpriteRenderer : MonoBehaviour
{
    /// <summary>
    /// Diced sprite data used for rendering.
    /// </summary>
    public DicedSprite DicedSprite { get { return _dicedSprite; } set { SetDicedSprite(value); } }

    /// <summary>
    /// Sprite tint color.
    /// </summary>
    public Color Color { get { return _color; } set { SetMaterialColor(value); } }

    /// <summary>
    /// Material used by the renderer. Will reference shared material in edit mode.
    /// </summary>
    public Material Material { get { return GetMaterial(); } set { SetMaterial(value); } }

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int mainTexPropertyId;
    private int colorPropertyId;

    [Tooltip("Diced sprite data used for rendering.")]
    [SerializeField] private DicedSprite _dicedSprite;
    [Tooltip("Sprite tint color.")]
    [SerializeField] private Color _color = Color.white;

    private MaterialPropertyBlock _materialPropertiesCache;

    private void Awake ()
    {
        mainTexPropertyId = Shader.PropertyToID("_MainTex");
        colorPropertyId = Shader.PropertyToID("_RendererColor");
    }

    private void OnEnable ()
    {
        InitializeMeshFilter();
        InitializeMeshRenderer();
        SetDicedSprite(DicedSprite);
        SetMaterialColor(Color);
        meshRenderer.enabled = true;
    }

    private void OnDisable ()
    {
        meshRenderer.enabled = false;
    }

    private void OnValidate ()
    {
        if (!isActiveAndEnabled) return;
        SetDicedSprite(DicedSprite);
        SetMaterialColor(Color);
    }

    private void OnDidApplyAnimationProperties ()
    {
        SetMaterialColor(Color);
    }

    /// <summary>
    /// Assigns new diced sprite data and updates mesh to represnt 
    /// </summary>
    public void SetDicedSprite (DicedSprite dicedSprite)
    {
        _dicedSprite = dicedSprite;

        if (!dicedSprite)
        {
            GetMesh().Clear();
            return;
        }

        #if UNITY_EDITOR
        // Reset sprite after it's data was modified (usually when rebuiding atlas).
        dicedSprite.OnModified.RemoveListener(SetDicedSprite);
        dicedSprite.OnModified.AddListener(SetDicedSprite);
        #endif

        dicedSprite.FillMesh(GetMesh());
        SetMaterialMainTex(dicedSprite.AtlasTexture);
    }

    private void InitializeMeshFilter ()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.hideFlags = HideFlags.HideInInspector;
        if (!Application.isPlaying)
        {
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.name = "Generated Diced Sprite Mesh (Shared)";
        }
        else if (!meshFilter.mesh)
        {
            meshFilter.mesh = new Mesh();
            meshFilter.mesh.name = "Generated Diced Sprite Mesh";
        }
    }

    private void InitializeMeshRenderer ()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.hideFlags = HideFlags.HideInInspector;
        if (!Material) Material = new Material(Shader.Find("Sprites/Default"));
    }

    private void SetMaterialMainTex (Texture2D mainText)
    {
        var materialProperties = GetMaterialProperties();
        materialProperties.SetTexture(mainTexPropertyId, mainText);
        meshRenderer.SetPropertyBlock(materialProperties);
    }

    private void SetMaterialColor (Color color)
    {
        _color = color;
        var materialProperties = GetMaterialProperties();
        materialProperties.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(materialProperties);
    }

    private MaterialPropertyBlock GetMaterialProperties ()
    {
        Debug.Assert(meshRenderer);
        if (_materialPropertiesCache == null)
            _materialPropertiesCache = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_materialPropertiesCache);
        return _materialPropertiesCache;
    }

    private Mesh GetMesh ()
    {
        Debug.Assert(meshFilter);
        return Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
    }

    private Material GetMaterial ()
    {
        Debug.Assert(meshRenderer);
        return Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
    }

    private void SetMaterial (Material material)
    {
        Debug.Assert(meshRenderer);
        if (Application.isPlaying) meshRenderer.material = material;
        else meshRenderer.sharedMaterial = material;
    }
}
