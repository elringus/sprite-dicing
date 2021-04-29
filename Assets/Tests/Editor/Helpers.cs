using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing.Test
{
    public static class Helpers
    {
        public static class Paths
        {
            public static readonly string BGRT = BuildTexturePath("2x2/BGRT");
            public static readonly string BTGR = BuildTexturePath("2x2/BTGR");
            public static readonly string TTTT = BuildTexturePath("2x2/TTTT");
            public static readonly string RGB1x3 = BuildTexturePath("RGB1x3");
            public static readonly string RGB3x1 = BuildTexturePath("RGB3x1");
            public static readonly string RGB8x8 = BuildTexturePath("RGB8x8");
            public static readonly IReadOnlyList<string> TopLevel = new[] { RGB1x3, RGB3x1, RGB8x8 };
            public static readonly IReadOnlyList<string> TwoByTwo = new[] { BGRT, BTGR, TTTT };
            public static readonly IReadOnlyList<string> All = TopLevel.Concat(TwoByTwo).ToArray();
        }

        public static class Textures
        {
            public static Texture2D BGRT => LoadTexture("2x2/BGRT");
            public static Texture2D BTGR => LoadTexture("2x2/BTGR");
            public static Texture2D TTTT => LoadTexture("2x2/TTTT");
            public static Texture2D RGB1x3 => LoadTexture("RGB1x3");
            public static Texture2D RGB3x1 => LoadTexture("RGB3x1");
            public static Texture2D RGB8x8 => LoadTexture("RGB8x8");
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
