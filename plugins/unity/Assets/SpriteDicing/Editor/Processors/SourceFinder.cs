using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace SpriteDicing
{
    /// <summary>
    /// Finds paths of the source sprites to dice.
    /// </summary>
    public static class SourceFinder
    {
        public static IReadOnlyList<string> FindAt (string folderPath, bool includeSubfolders)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("Folder path is empty.");
            if (!Directory.Exists(folderPath))
                throw new ArgumentException($"Directory '{folderPath}' doesn't exist.");

            var paths = new List<string>();
            foreach (var path in FindAllSourcesAt(folderPath))
                if (includeSubfolders || !IsInSubfolder(folderPath, path))
                    paths.Add(path);
            return paths;
        }

        private static bool IsInSubfolder (string folderPath, string assetPath)
        {
            return assetPath[(folderPath.Length + 1)..].Contains('/');
        }

        private static IEnumerable<string> FindAllSourcesAt (string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            return guids.Select(AssetDatabase.GUIDToAssetPath);
        }
    }
}
