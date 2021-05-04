using System;
using System.Collections.Generic;
using System.Linq;

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
        public IReadOnlyCollection<DicedUnit> Units { get; }
        /// <summary>
        /// Associated diced units with distinct content.
        /// </summary>
        public IReadOnlyCollection<DicedUnit> UniqueUnits { get; }

        public DicedTexture (SourceTexture source, IEnumerable<DicedUnit> units)
        {
            Source = source;
            Units = units?.ToArray() ?? throw new ArgumentNullException(nameof(units));
            UniqueUnits = Units.Distinct().ToArray();
        }
    }
}
