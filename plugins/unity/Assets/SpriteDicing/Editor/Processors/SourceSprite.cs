using UnityEngine;

namespace SpriteDicing
{
    public readonly struct SourceSprite
    {
        public Native.SourceSprite Native { get; init; }
        public Texture2D Texture { get; init; }
    }
}
