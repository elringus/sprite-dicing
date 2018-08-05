using System.Collections.Generic;
using UnityCommon;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Manages diced sprite data and atlas texture.
    /// </summary>
    [CreateAssetMenu(menuName = "Diced Sprite Atlas", order = 350)]
    public class DicedSpriteAtlas : ScriptableObject
    {
        /// <summary>
        /// Number of diced sprites stored in this atlas.
        /// </summary>
        public int SpritesCount => dicedSprites.Count;
        /// <summary>
        /// Number of textures used by this atlas.
        /// </summary>
        public int TexturesCount => atlasTextures.Count;
        /// <summary>
        /// Whether the atlas is built and ready to be used.
        /// </summary>
        public bool IsBuilt => TexturesCount > 0 && SpritesCount > 0;

        [SerializeField, ReadOnly] private List<Texture2D> atlasTextures = new List<Texture2D>();
        [SerializeField, ReadOnly] private List<DicedSprite> dicedSprites = new List<DicedSprite>();

        #if UNITY_EDITOR
        // Editor-only data to track source sprite textures and store build configuration.
        // Disabled warnings are about 'unused' variables (managed by the editor script via reflection).
        #pragma warning disable 0169, 0414, 1635
        [SerializeField] private int atlasSizeLimit = 2048;
        [SerializeField] private bool forceSquare = false;
        [SerializeField] private float pixelsPerUnit = 100f;
        [IntPopup(8, 16, 32, 64, 128, 256)]
        [SerializeField] private int diceUnitSize = 64;
        [SerializeField] private int padding = 2;
        [SerializeField] private Vector2 defaultPivot = new Vector2(.5f, .5f);
        [SerializeField] private bool keepOriginalPivot;
        [SerializeField] private bool decoupleSpriteData;
        [FolderAsset]
        [SerializeField] private Object inputFolder;
        [SerializeField] private bool includeSubfolders;
        [SerializeField] private bool prependSubfolderNames;
        [HideInInspector]
        [SerializeField] private string generatedSpritesFolderGuid;
        #pragma warning restore 0169, 0414, 1635
        #endif

        /// <summary>
        /// Retrieves stored diced sprite data.
        /// </summary>
        /// <param name="spriteName">Name of the sprite to retrieve.</param>
        /// <returns>Diced sprite data or null if not found.</returns>
        public DicedSprite GetSprite (string spriteName)
        {
            return dicedSprites.Find(sprite => sprite.Name.Equals(spriteName));
        }
    }
}
