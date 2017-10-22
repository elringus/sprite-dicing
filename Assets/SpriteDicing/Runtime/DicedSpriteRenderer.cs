using UnityEngine;

/// <summary>
/// Renders a DicedSprite via MeshRenderer using mesh generated with the sprite's data and atlas texture.
/// </summary>
[AddComponentMenu("Rendering/Diced Sprite Renderer")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode, DisallowMultipleComponent]
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
    /// Flip sprite by X-axis.
    /// </summary>
    public bool FlipX { get { return _flipX; } set { SetMaterialFlip(value, FlipY); } }

    /// <summary>
    /// Flip sprite by Y-axis.
    /// </summary>
    public bool FlipY { get { return _flipY; } set { SetMaterialFlip(FlipX, value); } }

    /// <summary>
    /// Renderer used to draw diced sprite generated mesh.
    /// </summary>
    public MeshRenderer Renderer { get { return GetRenderer(); } }

    /// <summary>
    /// Material used by the renderer. Will reference shared material in edit mode or when ShareMaterial is enabled.
    /// </summary>
    public Material Material { get { return GetMaterial(); } set { SetMaterial(value); } }

    /// <summary>
    /// Whether to use shared material. Enable to allow batching.
    /// </summary>
    public bool ShareMaterial { get { return _shareMaterial; } set { _shareMaterial = value; } }

    /// <summary>
    /// Generated diced sprite mesh. Will reference shared mesh in edit mode.
    /// </summary>
    public Mesh Mesh { get { return GetMesh(); } }

    [Tooltip("Diced sprite data used for rendering.")]
    [SerializeField] private DicedSprite _dicedSprite = null;
    [Tooltip("Sprite tint color.")]
    [SerializeField] private Color _color = Color.white;
    [Tooltip("Flip sprite by X-axis.")]
    [SerializeField] private bool _flipX = false;
    [Tooltip("Flip sprite by Y-axis.")]
    [SerializeField] private bool _flipY = false;
    [Tooltip("Whether to use shared material. Enable to allow batching.")]
    [SerializeField] private bool _shareMaterial = true;
    [Tooltip("Material to use for rendering. Default diced sprite material will be used if not provided.")]
    [SerializeField] private Material customMaterial = null;

    private const string DEFAULT_SHADER_PATH = "SpriteDicing/Default";

    private static Material defaultMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock materialPropertiesCache;
    private int mainTexPropertyId;
    private int colorPropertyId;
    private int flipPropertyId;

    private void Awake ()
    {
        mainTexPropertyId = Shader.PropertyToID("_MainTex");
        colorPropertyId = Shader.PropertyToID("_TintColor");
        flipPropertyId = Shader.PropertyToID("_Flip");
        InitializeMeshFilter();
        InitializeMeshRenderer();
    }

    private void OnEnable ()
    {
        SetDicedSprite(DicedSprite);
        SetMaterialColor(Color);
        SetMaterialFlip(FlipX, FlipY);
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
        SetMaterialFlip(FlipX, FlipY);
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

        if (!DicedSprite)
        {
            if (Mesh.vertexCount > 0) Mesh.Clear();
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
        meshFilter.sharedMesh = new Mesh();
        meshFilter.sharedMesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        meshFilter.sharedMesh.name = "Generated Diced Sprite Mesh (Shared)";
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

    private void SetMaterialMainTex (Texture2D newMainText)
    {
        var materialProperties = GetMaterialProperties();
        materialProperties.SetTexture(mainTexPropertyId, newMainText);
        Renderer.SetPropertyBlock(materialProperties);
    }

    private void SetMaterialColor (Color newColor)
    {
        _color = newColor;
        var materialProperties = GetMaterialProperties();
        materialProperties.SetColor(colorPropertyId, newColor);
        Renderer.SetPropertyBlock(materialProperties);
    }

    private void SetMaterialFlip (bool flipX, bool flipY)
    {
        _flipX = flipX;
        _flipY = flipY;
        var materialProperties = GetMaterialProperties();
        materialProperties.SetVector(flipPropertyId, new Vector4(FlipX ? -1 : 1, FlipY ? -1 : 1));
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
        return meshFilter.sharedMesh;
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

    private void SetMaterial (Material newMaterial)
    {
        if (!Application.isPlaying || ShareMaterial) Renderer.sharedMaterial = newMaterial;
        else Renderer.material = newMaterial;
    }

    private void ValidateMaterial ()
    {
        if (!defaultMaterial)
            defaultMaterial = new Material(Shader.Find(DEFAULT_SHADER_PATH));
        if (customMaterial && Material != customMaterial)
            Material = customMaterial;
        else if (!customMaterial && Material != defaultMaterial)
            Material = defaultMaterial;
    }
}
