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
        private readonly string nameRoot;

        /// <param name="nameRoot">When provided, will include subfolders in the texture names.</param>
        public TextureLoader (string nameRoot = null)
        {
            this.nameRoot = nameRoot;
        }

        public SourceTexture Load (string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
                throw new ArgumentNullException(nameof(texturePath));

            var name = BuildName(texturePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture) throw new Exception($"Failed to load `{texturePath}` texture.");
            EnsureReadable(texturePath);
            var pivot = GetSpritePivotOrNull(texturePath);
            return new SourceTexture(name, texture, pivot);
        }

        private string BuildName (string path)
        {
            if (nameRoot is null) return Path.GetFileNameWithoutExtension(path);
            if (!path.Contains(nameRoot)) throw new Exception($"Name root `{nameRoot}` is not valid for `{path}` path.");
            return Path.GetFileNameWithoutExtension(path.Substring(nameRoot.Length + 1).Replace("/", "."));
        }

        private Vector2? GetSpritePivotOrNull (string texturePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            return sprite ? sprite.pivot : (Vector2?)null;
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
