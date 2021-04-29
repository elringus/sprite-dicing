using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Represents a single piece diced of a sprite texture.
    /// </summary>
    public class DicedUnit
    {
        /// <summary>
        /// Atlas texture containing unit's texture.
        /// </summary>
        public Texture2D AtlasTexture { get; set; }
        /// <summary>
        /// Positions of the quad vertices used to represent the unit in (local) space.
        /// </summary>
        public Rect QuadVerts { get; set; }
        /// <summary>
        /// UV rect on the atlas texture containing unit's texture.
        /// </summary>
        public Rect QuadUVs { get; set; }
        /// <summary>
        /// Colors of the diced unit (equivalent to the unit's texture), plus colors from the padding rect.
        /// Padding is required to prevent texture bleeding.
        /// </summary>
        public Color[] PaddedColors { get; set; }
        /// <summary>
        /// Unique hash based on the texture of the diced unit.
        /// </summary>
        public Hash128 ContentHash { get; set; }
    }
}
