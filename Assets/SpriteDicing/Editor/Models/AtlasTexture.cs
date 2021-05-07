using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Represents an atlas texture generated from diced textures.
    /// </summary>
    public class AtlasTexture
    {
        /// <summary>
        /// The atlas texture asset.
        /// </summary>
        public Texture2D Texture { get; }
        /// <summary>
        /// Content (color) hashes mapped to UVs of the atlas texture.
        /// </summary>
        public IReadOnlyDictionary<Hash128, Rect> ContentToUV { get; }
        /// <summary>
        /// Associated diced textures.
        /// </summary>
        public IReadOnlyList<DicedTexture> DicedTextures { get; }

        public AtlasTexture (Texture2D texture, IDictionary<Hash128, Rect> contentToUV, IEnumerable<DicedTexture> dicedTextures)
        {
            if (!texture) throw new ArgumentNullException(nameof(texture));
            if (contentToUV is null) throw new ArgumentNullException(nameof(contentToUV));
            if (dicedTextures is null) throw new ArgumentNullException(nameof(dicedTextures));

            this.Texture = texture;
            this.ContentToUV = new Dictionary<Hash128, Rect>(contentToUV);
            this.DicedTextures = dicedTextures.ToArray();
        }
    }
}
