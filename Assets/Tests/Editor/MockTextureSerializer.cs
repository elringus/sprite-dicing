using UnityEngine;

namespace SpriteDicing.Test
{
    public class MockTextureSerializer : ITextureSerializer
    {
        public Texture2D Serialize (Texture2D texture) => texture;
    }
}
