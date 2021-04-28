using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpriteDicing.Test
{
    [ExcludeFromCoverage]
    public static class Helpers
    {
        public static class Paths
        {
            public static readonly string BGRT = BuildTexturePath("2x2/BGRT");
            public static readonly string BTGR = BuildTexturePath("2x2/BTGR");
            public static readonly string TTTT = BuildTexturePath("2x2/TTTT");
            public static readonly string RGBA1x4 = BuildTexturePath("RGBA1x4");
            public static readonly string RGBA4x1 = BuildTexturePath("RGBA4x1");
            public static readonly string RGBA8x8 = BuildTexturePath("RGBA8x8");
            public static readonly IReadOnlyList<string> TopLevel = new[] { RGBA1x4, RGBA4x1, RGBA8x8 };
            public static readonly IReadOnlyList<string> TwoByTwo = new[] { BGRT, BTGR, TTTT };
            public static readonly IReadOnlyList<string> All = TopLevel.Concat(TwoByTwo).ToArray();
        }

        public static class Textures
        {
            public static Texture2D BGRT => LoadTexture("2x2/BGRT");
            public static Texture2D BTGR => LoadTexture("2x2/BTGR");
            public static Texture2D TTTT => LoadTexture("2x2/TTTT");
            public static Texture2D RGBA1x4 => LoadTexture("RGBA1x4");
            public static Texture2D RGBA4x1 => LoadTexture("RGBA4x1");
            public static Texture2D RGBA8x8 => LoadTexture("RGBA8x8");
        }

        public const string TextureFolderPath = "Assets/Tests/Textures";

        public static string BuildTexturePath (string textureName)
        {
            return $"{TextureFolderPath}/{textureName}.png";
        }

        public static Texture2D LoadTexture (string textureName)
        {
            var texturePath = BuildTexturePath(textureName);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }
    }
}
