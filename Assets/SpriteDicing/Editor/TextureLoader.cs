using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for loading texture asset to dice.
    /// </summary>
    public class TextureLoader
    {
        /// <param name="nameRoot">When provided, build name relative to the root replacing slashes with dots.</param>
        public SourceTexture Load (string texturePath, string nameRoot = null)
        {
            if (string.IsNullOrEmpty(texturePath))
                throw new ArgumentNullException(nameof(texturePath));

            var name = BuildName(texturePath, nameRoot);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture) throw new Exception($"Failed to load `{texturePath}` texture.");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
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
