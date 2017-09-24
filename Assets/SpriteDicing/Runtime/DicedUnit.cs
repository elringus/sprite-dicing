using UnityCommon;
using UnityEngine;

/// <summary>
/// Represents a single piece diced off a sprite texture.
/// </summary>
public class DicedUnit
{
    /// <summary>
    /// Name of the original texture (sprite) from which this unit was diced off.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Positions of the quad vertices used to represent the unit in (local) space.
    /// </summary>
    public Rect QuadVerts { get; set; }

    /// <summary>
    /// UV rect on the atlas texture containing unit's texture.
    /// </summary>
    public Rect QuadUVs { get; set; }

    /// <summary>
    /// Colors of the diced unit (equivalent to the unit's texture).
    /// </summary>
    public Color[] Colors { get; set; }

    /// <summary>
    /// Copy of the Colors, plus colors from the padding rect.
    /// Will be written to the padded areas of the generated atlas to prevent texture bleeding.
    /// </summary>
    public Color[] PaddedColors { get; set; }

    /// <summary>
    /// Unique hash based on the Colors array.
    /// </summary>
    public int ColorsHashCode { get { return Colors.GetArrayHashCode(); } }
}
