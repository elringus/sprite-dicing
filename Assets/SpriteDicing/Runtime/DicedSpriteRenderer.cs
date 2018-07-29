using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Renders a <see cref="SpriteDicing.DicedSprite"/> via <see cref="MeshRenderer"/> using mesh generated with the sprite's data and atlas texture.
    /// </summary>
    [AddComponentMenu("Rendering/Diced Sprite Renderer")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteInEditMode, DisallowMultipleComponent]
    public class DicedSpriteRenderer : MonoBehaviour
    {
        /// <summary>
        /// Diced sprite data used for rendering.
        /// </summary>
        public DicedSprite DicedSprite { get { return dicedSprite; } set { SetDicedSprite(value); } }

        /// <summary>
        /// Sprite tint color.
        /// </summary>
        public Color Color { get { return color; } set { SetMaterialColor(value); } }

        /// <summary>
        /// Flip sprite by X-axis.
        /// </summary>
        public bool FlipX { get { return flipX; } set { SetMaterialFlip(value, FlipY); } }

        /// <summary>
        /// Flip sprite by Y-axis.
        /// </summary>
        public bool FlipY { get { return flipY; } set { SetMaterialFlip(FlipX, value); } }

        /// <summary>
        /// Renderer used to draw diced sprite generated mesh.
        /// </summary>
        public MeshRenderer Renderer => GetRenderer();

        /// <summary>
        /// Material used by the renderer. Will reference shared material in edit mode or when <see cref="ShareMaterial"/> is enabled.
        /// </summary>
        public Material Material { get { return GetMaterial(); } set { SetMaterial(value); } }

        /// <summary>
        /// Whether to use shared material. Enable to allow batching.
        /// </summary>
        public bool ShareMaterial { get { return shareMaterial; } set { shareMaterial = value; } }

        /// <summary>
        /// Generated diced sprite mesh. Will reference shared mesh in editor mode.
        /// </summary>
        public Mesh Mesh => GetMesh();

        [Tooltip("Diced sprite data used for rendering.")]
        [SerializeField] private DicedSprite dicedSprite = null;
        [Tooltip("Sprite tint color.")]
        [SerializeField] private Color color = Color.white;
        [Tooltip("Flip sprite by X-axis.")]
        [SerializeField] private bool flipX = false;
        [Tooltip("Flip sprite by Y-axis.")]
        [SerializeField] private bool flipY = false;
        [Tooltip("Whether to use shared material. Enable to allow batching.")]
        [SerializeField] private bool shareMaterial = true;
        [Tooltip("Material to use for rendering. Default diced sprite material will be used if not provided.")]
        [SerializeField] private Material customMaterial = null;

        private const string defaultShaderPath = "Sprites/Default";
        private static readonly int mainTexPropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int colorPropertyId = Shader.PropertyToID("_RendererColor");
        private static readonly int flipPropertyId = Shader.PropertyToID("_Flip");

        private static Material defaultMaterial;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock materialPropertiesCache;

        private void Awake ()
        {
            InitializeMeshFilter();
            InitializeMeshRenderer();
        }

        private void OnEnable ()
        {
            Renderer.enabled = true;
            SetDicedSprite(DicedSprite);
            SetMaterialColor(Color);
            SetMaterialFlip(FlipX, FlipY);
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

            dicedSprite = newDicedSprite;

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
            color = newColor;
            var materialProperties = GetMaterialProperties();
            materialProperties.SetColor(colorPropertyId, newColor);
            Renderer.SetPropertyBlock(materialProperties);
        }

        private void SetMaterialFlip (bool flipX, bool flipY)
        {
            this.flipX = flipX;
            this.flipY = flipY;
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
                defaultMaterial = new Material(Shader.Find(defaultShaderPath));
            if (customMaterial && Material != customMaterial)
                Material = customMaterial;
            else if (!customMaterial && Material != defaultMaterial)
                Material = defaultMaterial;
        }
    }
}
