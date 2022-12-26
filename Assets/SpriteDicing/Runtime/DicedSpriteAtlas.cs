using System.Collections.Generic;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Stores diced sprites and associated atlas textures.
    /// </summary>
    [CreateAssetMenu(menuName = "Diced Sprite Atlas", order = 350)]
    public class DicedSpriteAtlas : ScriptableObject
    {
        /// <summary>
        /// Diced sprites stored in the atlas.
        /// </summary>
        public IReadOnlyList<Sprite> Sprites => sprites;
        /// <summary>
        /// Atlas textures used by the diced sprites.
        /// </summary>
        public IReadOnlyList<Texture2D> Textures => textures;

        [SerializeField] private List<Sprite> sprites = new List<Sprite>();
        [SerializeField] private List<Texture2D> textures = new List<Texture2D>();

        #if UNITY_EDITOR
        // Editor-only data to track source sprite textures and store build configuration.
        // ReSharper disable NotAccessedField.Local (used by the editor scripts via reflection)
        #pragma warning disable 0169, 0414, 1635, IDE0052
        [SerializeField] private int atlasSizeLimit = 2048;
        [SerializeField] private bool forceSquare;
        [SerializeField] private bool forcePot;
        [SerializeField] private float pixelsPerUnit = 100f;
        [SerializeField] private int diceUnitSize = 64;
        [SerializeField] private int padding = 2;
        [SerializeField] private float uvInset;
        [SerializeField] private Vector2 defaultPivot = new Vector2(.5f, .5f);
        [SerializeField] private bool keepOriginalPivot;
        [SerializeField] private bool decoupleSpriteData;
        [SerializeField] private bool trimTransparent = true;
        [SerializeField] private Object inputFolder;
        [SerializeField] private bool includeSubfolders;
        [SerializeField] private bool prependSubfolderNames;
        [SerializeField] private string generatedSpritesFolderGuid;
        [SerializeField] private string lastRatioValue = "Unknown (build atlas to update)";
        #pragma warning restore 0169, 0414, 1635, IDE0052
        // ReSharper restore NotAccessedField.Local
        #endif

        /// <summary>
        /// Retrieves a diced sprite with the specified name.
        /// </summary>
        /// <param name="spriteName">Name of the sprite to retrieve.</param>
        /// <returns>Diced sprite with the specified name or null if not found.</returns>
        public Sprite GetSprite (string spriteName) => sprites.Find(s => s.name == spriteName);
    }
}
