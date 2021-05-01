using System;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Represents a unit diced of a texture.
    /// </summary>
    public readonly struct DicedUnit : IEquatable<DicedUnit>
    {
        /// <summary>
        /// Positions of the quad vertices used to represent the unit in (local) space.
        /// </summary>
        public Rect QuadVerts { get; }
        /// <summary>
        /// Colors of the diced unit, plus colors from the padding rect.
        /// </summary>
        public Color[] PaddedPixels { get; }
        /// <summary>
        /// Hash based on the non-padded pixels of the unit.
        /// </summary>
        public Hash128 ContentHash { get; }

        public DicedUnit (Rect quadVerts, Color[] paddedPixels, Hash128 contentHash)
        {
            this.QuadVerts = quadVerts;
            this.PaddedPixels = paddedPixels;
            this.ContentHash = contentHash;
        }

        public bool Equals (DicedUnit other) => ContentHash.Equals(other.ContentHash);
        public override bool Equals (object obj) => obj is DicedUnit other && Equals(other);
        public override int GetHashCode () => ContentHash.GetHashCode();
    }
}
