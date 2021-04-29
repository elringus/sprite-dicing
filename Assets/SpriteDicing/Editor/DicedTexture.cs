using System.Collections.Generic;
using System.Linq;

namespace SpriteDicing
{
    /// <summary>
    /// Represents a texture diced of a <see cref="SourceTexture"/>.
    /// </summary>
    public readonly struct DicedTexture
    {
        /// <summary>
        /// Original texture from which this one was generated.
        /// </summary>
        public SourceTexture Source { get; }
        /// <summary>
        /// Associated diced units.
        /// </summary>
        public IReadOnlyList<DicedUnit> Units { get; }

        public DicedTexture (SourceTexture source, IEnumerable<DicedUnit> units)
        {
            Source = source;
            Units = units.ToArray();
        }
    }
}
