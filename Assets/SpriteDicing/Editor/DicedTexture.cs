using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Represents a texture diced of a <see cref="SourceTexture"/>.
    /// </summary>
    public class DicedTexture
    {
        /// <summary>
        /// Original texture from which this one was generated.
        /// </summary>
        public SourceTexture Source { get; }
        /// <summary>
        /// Associated diced units.
        /// </summary>
        public IReadOnlyList<DicedUnit> Units { get; }
        /// <summary>
        /// Hashes of the unique colors contained in the associated diced units.
        /// </summary>
        public HashSet<Hash128> UniqueContent { get; }

        public DicedTexture (SourceTexture source, IEnumerable<DicedUnit> units)
        {
            Source = source;
            Units = units?.ToArray() ?? throw new ArgumentNullException(nameof(units));
            UniqueContent = new HashSet<Hash128>(Units.Select(u => u.ContentHash));
        }
    }
}
