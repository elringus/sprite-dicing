using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Loads source sprite textures to dice.
    /// </summary>
    public class SourceLoader
    {
        private static readonly char[] invalidNameChars = Path.GetInvalidFileNameChars();
        private readonly string root;
        private readonly string separator;
        private readonly bool keepPivot;

        public SourceLoader (string root, string separator, bool keepPivot)
        {
            this.root = root;
            this.separator = separator;
            this.keepPivot = keepPivot;
        }

        public void Load (string sourcePath, ICollection<SourceSprite> sources)
        {
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentNullException(nameof(sourcePath));
            EnsureReadable(sourcePath);
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Sprite>())
                sources.Add(BuildSource(sourcePath, sprite));
        }

        private SourceSprite BuildSource (string path, Sprite sprite) => new() {
            Native = new Native.SourceSprite {
                Id = BuildID(path, sprite),
                Texture = BuildTexture(sprite),
                Pivot = GetPivot(sprite)
            },
            Texture = sprite.texture
        };

        private string BuildID (string path, Sprite sprite)
        {
            if (!Path.HasExtension(path))
                throw new ArgumentException($"Invalid source path: {path}", nameof(path));
            if (!path.Contains(root))
                throw new ArgumentException($"Name root '{root}' is not valid for '{path}' path.", nameof(path));
            var local = path[(root.Length + 1)..];
            var id = Path.GetFileNameWithoutExtension(local.Replace("/", separator));
            if (sprite.name == sprite.texture.name) return id; // single sprite mode
            return $"{id}{separator}{SanitizeName(sprite.name)}";
        }

        private Native.Pivot? GetPivot (Sprite sprite)
        {
            if (!keepPivot) return null;
            var pivot = sprite.pivot / sprite.rect.size;
            return new Native.Pivot { X = pivot.x, Y = pivot.y };
        }

        private static Native.Texture BuildTexture (Sprite sprite) => new() {
            Width = (uint)sprite.rect.width,
            Height = (uint)sprite.rect.height,
            Pixels = BuildPixels(sprite)
        };

        private static Native.Pixel[] BuildPixels (Sprite sprite)
        {
            var colors = sprite.texture.GetPixels32(); // GetPixelData is actually slower in the editor.
            var pixels = new Native.Pixel[(int)(sprite.rect.width * sprite.rect.height)];
            int idx = 0;
            for (int y = (int)sprite.rect.yMin; y < sprite.rect.yMax; y++)
            for (int x = (int)sprite.rect.xMin; x < sprite.rect.xMax; x++)
            {
                var c = colors[y * sprite.texture.width + x];
                pixels[idx++] = new() { R = c.r, G = c.g, B = c.b, A = c.a };
            }
            return pixels;
        }

        private static void EnsureReadable (string texturePath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.isReadable = true;
            importer.crunchedCompression = false;
            importer.SaveAndReimport();
        }

        private static string SanitizeName (string name)
        {
            if (!name.Any(c => invalidNameChars.Contains(c))) return name;
            return string.Join('_', name.Split(invalidNameChars));
        }
    }
}
