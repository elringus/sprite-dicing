using UnityEngine;

namespace SpriteDicing
{
    public readonly struct SourceSprite
    {
        public Native.SourceSprite Native { get; init; }
        public Sprite Managed { get; init; }
    }
}
