using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Implementation is able to create persistent assets from textures.
    /// </summary>
    public interface ITextureSerializer
    {
        /// <param name="texture">Transient texture to serialize.</param>
        /// <returns>Persistent texture asset.</returns>
        Texture2D Serialize (Texture2D texture);
    }
}
