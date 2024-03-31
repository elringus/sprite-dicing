using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace SpriteDicing
{
    /// <summary>
    /// Finds paths of the source sprite textures to dice.
    /// </summary>
    public static class SourceFinder
    {
        public static IReadOnlyList<string> FindAt (string folderPath, bool includeSubfolders)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("Folder path is empty.");
            if (!Directory.Exists(folderPath))
                throw new ArgumentException($"Directory `{folderPath}` doesn't exist.");

            var textures = new List<string>();
            foreach (var path in FindAllTexturesAt(folderPath))
                if (includeSubfolders || !IsInSubfolder(folderPath, path))
                    textures.Add(path);
            return textures;
        }

        private static bool IsInSubfolder (string folderPath, string assetPath)
        {
            return assetPath[(folderPath.Length + 1)..].Contains("/");
        }

        private static IEnumerable<string> FindAllTexturesAt (string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            return guids.Select(AssetDatabase.GUIDToAssetPath);
        }
    }
}
