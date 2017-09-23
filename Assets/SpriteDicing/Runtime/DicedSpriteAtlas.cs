using System.Collections.Generic;
using UnityCommon;
using UnityEngine;

/// <summary>
/// Manages diced sprite data and atlas texture.
/// </summary>
[CreateAssetMenu(menuName = "Sprites/Diced Sprite Atlas")]
public class DicedSpriteAtlas : ScriptableObject
{
    /// <summary>
    /// Number of diced sprites stored in this atlas.
    /// </summary>
    public int SpritesCount { get { return dicedSprites.Count; } }

    /// <summary>
    /// Whether the atlas is built and ready to be used.
    /// </summary>
    public bool IsBuilt { get { return atlasTexture && SpritesCount > 0; } }

    [SerializeField, ReadOnly] private Texture2D atlasTexture = null;
    [SerializeField, ReadOnly] private List<DicedSprite> dicedSprites = new List<DicedSprite>();

    #if UNITY_EDITOR
    // Editor-only data to track source sprite textures and store build configuration.
    // Disabled warnings are about 'unused' variables (managed by the editor script via reflection).
    #pragma warning disable 0169, 0414, 1635
    [IntPopup(1024, 2048, 4096, 8192)]
    [SerializeField] private int atlasTextureSizeLimit = 2048;
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

    /// <summary>
    /// Get size of the generated atlas texture.
    /// </summary>
    public Vector2 GetAtlasTextureSize ()
    {
        return atlasTexture ? new Vector2(atlasTexture.width, atlasTexture.height) : Vector2.zero;
    }
}
