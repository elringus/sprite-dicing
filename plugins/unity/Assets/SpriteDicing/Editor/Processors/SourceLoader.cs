using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Loads source sprite textures to dice.
    /// </summary>
    public class SourceLoader
    {
        private readonly string root;
        private readonly string separator;
        private readonly bool keepPivot;

        public SourceLoader (string root, string separator, bool keepPivot)
        {
            this.root = root;
            this.separator = separator;
            this.keepPivot = keepPivot;
        }

        public Native.SourceSprite Load (string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
                throw new ArgumentNullException(nameof(texturePath));
            return new Native.SourceSprite {
                Id = BuildID(texturePath),
                Bytes = File.ReadAllBytes(texturePath),
                Format = Path.GetExtension(texturePath).Substring(1),
                Pivot = GetPivot(texturePath)
            };
        }

        private string BuildID (string path)
        {
            if (!path.Contains(root))
                throw new Exception($"Name root `{root}` is not valid for `{path}` path.");
            var local = path.Substring(root.Length + 1);
            return Path.GetFileNameWithoutExtension(local.Replace("/", separator));
        }

        private Native.Pivot? GetPivot (string texturePath)
        {
            if (!keepPivot) return null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (!sprite) return null;
            var pivot = sprite.pivot / sprite.rect.size;
            return new Native.Pivot { X = pivot.x, Y = pivot.y };
        }
    }
}
