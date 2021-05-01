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
        public IReadOnlyList<DicedUnit> Units { get; }
        /// <summary>
        /// A subset of <see cref="Units"/> with distinct content hash.
        /// </summary>
        public IReadOnlyList<DicedUnit> UniqueUnits { get; }

        public DicedTexture (SourceTexture source, IEnumerable<DicedUnit> units)
        {
            if (units is null) throw new ArgumentNullException(nameof(units));

            Source = source;
            Units = units.ToArray();
            UniqueUnits = Units.Distinct().ToArray();
        }
    }
}
