using System;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Represents original texture and associated data required to generate sliced sprite.
    /// </summary>
    public class SourceTexture
    {
        /// <summary>
        /// Name of the texture.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The texture asset.
        /// </summary>
        public Texture2D Texture { get; }
        /// <summary>
        /// Custom pivot assigned to the texture or none.
        /// </summary>
        public Vector2? Pivot { get; }

        public SourceTexture (string name, Texture2D texture, Vector2? pivot = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Texture = texture ? texture : throw new ArgumentNullException(nameof(texture));
            Pivot = pivot;
        }
    }
}
