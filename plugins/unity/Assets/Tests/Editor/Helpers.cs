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
            public static readonly string B = BuildSourcePath("1x1/B");
            public static readonly string R = BuildSourcePath("1x1/R");
            public static readonly string BGRT = BuildSourcePath("2x2/BGRT");
            public static readonly string BTGR = BuildSourcePath("2x2/BTGR");
            public static readonly string BTGT = BuildSourcePath("2x2/BTGT");
            public static readonly string TTTT = BuildSourcePath("2x2/TTTT");
            public static readonly string RGB1x3 = BuildSourcePath("RGB1x3");
            public static readonly string RGB3x1 = BuildSourcePath("RGB3x1");
            public static readonly string RGB4x4 = BuildSourcePath("RGB4x4");
            public static readonly string UIC4x4 = BuildSourcePath("UIC4x4");
            public static readonly string Multiple = BuildSourcePath("Multiple");
            public static readonly IReadOnlyList<string> OneByOne = new[] { B, R };
            public static readonly IReadOnlyList<string> TwoByTwo = new[] { BGRT, BTGR, BTGT, TTTT };
            public static readonly IReadOnlyList<string> TopLevel = new[] { RGB1x3, RGB3x1, RGB4x4, UIC4x4, Multiple };
            public static readonly IReadOnlyList<string> All = TopLevel.Concat(TwoByTwo).Concat(OneByOne).ToArray();
        }

        public static class Textures
        {
            public static Sprite B => LoadSource(Paths.B);
            public static Sprite R => LoadSource(Paths.R);
            public static Sprite BGRT => LoadSource(Paths.BGRT);
            public static Sprite BTGR => LoadSource(Paths.BTGR);
            public static Sprite BTGT => LoadSource(Paths.BTGT);
            public static Sprite TTTT => LoadSource(Paths.TTTT);
            public static Sprite RGB1x3 => LoadSource(Paths.RGB1x3);
            public static Sprite RGB3x1 => LoadSource(Paths.RGB3x1);
            public static Sprite RGB4x4 => LoadSource(Paths.RGB4x4);
            public static Sprite UIC4x4 => LoadSource(Paths.UIC4x4);
            public static Sprite Multiple => LoadSource(Paths.Multiple);
        }

        public static class Colors
        {
            public static readonly Color32 Red = new(255, 0, 0, 255);
            public static readonly Color32 Green = new(0, 255, 0, 255);
            public static readonly Color32 Blue = new(0, 0, 255, 255);
            public static readonly Color32 Black = new(0, 0, 0, 255);
        }

        public const string SourceFolderPath = "Assets/Tests/Sources";

        public static string BuildSourcePath (string sourceName)
        {
            return $"{SourceFolderPath}/{sourceName}.png";
        }

        public static Sprite LoadSource (string sourcePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
        }

        public static TextureImporter GetImporter (string sourcePath)
        {
            return (TextureImporter)AssetImporter.GetAtPath(sourcePath);
        }

        public static TextureImporter GetImporter (Texture2D texture)
        {
            var texturePath = AssetDatabase.GetAssetPath(texture);
            return GetImporter(texturePath);
        }
    }
}
