using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for preparing texture asset for dicing.
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
            EnsureReadable(texturePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            return new SourceTexture(name, texture, sprite);
        }

        private static string BuildName (string path, string root = null)
        {
            if (string.IsNullOrEmpty(root)) return Path.GetFileNameWithoutExtension(path);
            if (!path.Contains(root)) throw new Exception($"Name root `{root}` is not valid for `{path}` path.");
            return Path.GetFileNameWithoutExtension(path.Substring(root.Length + 1).Replace("/", "."));
        }

        private static void EnsureReadable (string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Debug.Assert(importer);
            if (importer.isReadable && !importer.crunchedCompression) return;
            importer.isReadable = true;
            importer.crunchedCompression = false;
            AssetDatabase.ImportAsset(path);
        }
    }
}
