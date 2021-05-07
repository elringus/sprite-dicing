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
        /// Sprite asset associated with the texture (if any or null).
        /// </summary>
        public Sprite Sprite { get; }

        public SourceTexture (string name, Texture2D texture, Sprite sprite = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Texture = texture ? texture : throw new ArgumentNullException(nameof(texture));
            Sprite = sprite;
        }
    }
}
