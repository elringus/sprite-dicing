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
            if (AssetImporter.GetAtPath(sourcePath) is not TextureImporter importer)
                throw new ArgumentException($"Invalid source path: '{sourcePath}' is not a sprite.");
            EnsureReadable(importer);
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
            return IsSingleMode(sprite) ? id : $"{id}{separator}{sprite.name}";
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
            if (IsSingleMode(sprite)) return BuildPixelsSingle(colors); // This is much faster for large sprites.
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

        private static Native.Pixel[] BuildPixelsSingle (Color32[] colors)
        {
            var pixels = new Native.Pixel[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                pixels[i] = new() { R = c.r, G = c.g, B = c.b, A = c.a };
            }
            return pixels;
        }

        private static void EnsureReadable (TextureImporter importer)
        {
            importer.isReadable = true;
            importer.crunchedCompression = false;
            importer.SaveAndReimport();
        }

        private static bool IsSingleMode (Sprite sprite)
        {
            return sprite.texture.name == sprite.name;
        }
    }
}
