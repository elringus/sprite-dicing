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
    /// Renderer used to draw diced sprite generated mesh.
    /// </summary>
    public MeshRenderer Renderer { get { return GetRenderer(); } }

    /// <summary>
    /// Material used by the renderer. Will reference shared material in edit mode or when ShareMaterial is enabled.
    /// </summary>
    public Material Material { get { return GetMaterial(); } set { SetMaterial(value); } }

    /// <summary>
    /// Whether to use shared material. Enable to allow draw calls batching.
    /// </summary>
    public bool ShareMaterial { get { return _shareMaterial; } set { _shareMaterial = value; } }

    /// <summary>
    /// Generated diced sprite mesh. Will reference shared mesh in edit mode.
    /// </summary>
    public Mesh Mesh { get { return GetMesh(); } }

    [Tooltip("Diced sprite data used for rendering.")]
    [SerializeField] private DicedSprite _dicedSprite;
    [Tooltip("Sprite tint color.")]
    [SerializeField] private Color _color = Color.white;
    [Tooltip("Whether to use shared material. Enable to allow draw calls batching.")]
    [SerializeField] private bool _shareMaterial = true;
    [Tooltip("Material to use for rendering. Default sprite material will be used if not provided.")]
    [SerializeField] private Material customMaterial = null;

    private static Material defaultMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock materialPropertiesCache;
    private int mainTexPropertyId;
    private int colorPropertyId;

    private void Awake ()
    {
        mainTexPropertyId = Shader.PropertyToID("_MainTex");
        colorPropertyId = Shader.PropertyToID("_RendererColor");
        InitializeMeshFilter();
        InitializeMeshRenderer();
    }

    private void OnEnable ()
    {
        SetDicedSprite(DicedSprite);
        SetMaterialColor(Color);
        Renderer.enabled = true;
    }

    private void OnDisable ()
    {
        Renderer.enabled = false;
    }

    private void OnValidate ()
    {
        ValidateMaterial();
        SetDicedSprite(DicedSprite);
        SetMaterialColor(Color);
    }

    private void OnDidApplyAnimationProperties ()
    {
        SetMaterialColor(Color);
    }

    /// <summary>
    /// Assigns new diced sprite data.
    /// </summary>
    public void SetDicedSprite (DicedSprite newDicedSprite)
    {
        #if UNITY_EDITOR
        // Reset sprite after it's data was modified (usually when rebuilding atlas).
        if (DicedSprite) DicedSprite.OnModified -= SetDicedSprite;
        if (newDicedSprite) newDicedSprite.OnModified += SetDicedSprite;
        #endif

        _dicedSprite = newDicedSprite;

        if (!DicedSprite && Mesh.vertexCount > 0)
        {
            Mesh.Clear();
            return;
        }

        DicedSprite.FillMesh(Mesh);
        SetMaterialMainTex(DicedSprite.AtlasTexture);
    }

    private void InitializeMeshFilter ()
    {
        if (!meshFilter)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter) meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.hideFlags = HideFlags.HideInInspector;
        }
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
        if (!meshRenderer)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.hideFlags = HideFlags.HideInInspector;
        }
        if (!Material) ValidateMaterial();
    }

    private void SetMaterialMainTex (Texture2D mainText)
    {
        var materialProperties = GetMaterialProperties();
        materialProperties.SetTexture(mainTexPropertyId, mainText);
        Renderer.SetPropertyBlock(materialProperties);
    }

    private void SetMaterialColor (Color color)
    {
        _color = color;
        var materialProperties = GetMaterialProperties();
        materialProperties.SetColor(colorPropertyId, color);
        Renderer.SetPropertyBlock(materialProperties);
    }

    private MaterialPropertyBlock GetMaterialProperties ()
    {
        if (materialPropertiesCache == null)
            materialPropertiesCache = new MaterialPropertyBlock();
        Renderer.GetPropertyBlock(materialPropertiesCache);
        return materialPropertiesCache;
    }

    private Mesh GetMesh ()
    {
        if (!meshFilter) InitializeMeshFilter();
        return Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
    }

    private MeshRenderer GetRenderer ()
    {
        if (!meshRenderer) InitializeMeshRenderer();
        return meshRenderer;
    }

    private Material GetMaterial ()
    {
        return !Application.isPlaying || ShareMaterial ? Renderer.sharedMaterial : Renderer.material;
    }

    private void SetMaterial (Material material)
    {
        if (!Application.isPlaying || ShareMaterial) Renderer.sharedMaterial = material;
        else Renderer.material = material;
    }

    private void ValidateMaterial ()
    {
        if (!defaultMaterial)
            defaultMaterial = new Material(Shader.Find("Sprites/Default"));
        if (customMaterial && Material != customMaterial)
            Material = customMaterial;
        else if (!customMaterial && Material != defaultMaterial)
            Material = defaultMaterial;
    }
}
