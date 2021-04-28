using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for loading texture assets to slice.
    /// </summary>
    public class TextureLoader
    {
        /// <param name="nameRoot">When provided, build names relative to the root replacing slashes with dots.</param>
        public IReadOnlyList<SourceTexture> Load (IEnumerable<string> texturePaths, string nameRoot = null)
        {
            if (texturePaths is null) throw new ArgumentNullException(nameof(texturePaths));

            var textures = new List<SourceTexture>();
            foreach (var path in texturePaths)
                textures.Add(LoadAt(path, nameRoot));
            return textures;
        }

        private static SourceTexture LoadAt (string path, string nameRoot = null)
        {
            var name = BuildName(path, nameRoot);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) throw new Exception($"Failed to load `{path}` texture.");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return new SourceTexture(name, texture, sprite);
        }

        private static string BuildName (string path, string root = null)
        {
            if (string.IsNullOrEmpty(root)) return Path.GetFileNameWithoutExtension(path);
            if (!path.Contains(root)) throw new Exception($"Name root `{root}` is not valid for `{path}` path.");
            return Path.GetFileNameWithoutExtension(path.Substring(root.Length + 1).Replace("/", "."));
        }
    }
}
