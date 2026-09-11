using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        public void Load (string path, ICollection<SourceSprite> sources)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                throw new ArgumentException($"Invalid source path: '{path}' is not a sprite.");
            EnsureReadable(importer);
            var colors = AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetPixels32();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
            foreach (var sprite in sprites.OrderBy(s => s.name, StringComparer.Ordinal))
                sources.Add(BuildSource(path, sprite, colors));
        }

        private SourceSprite BuildSource (string path, Sprite sprite, Color32[] colors) => new() {
            Native = new Native.SourceSprite {
                Id = BuildID(path, sprite),
                Texture = BuildTexture(sprite, colors),
                Pivot = GetPivot(sprite)
            },
            Managed = sprite
        };

        private string BuildID (string path, Sprite sprite)
        {
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
            return new Native.Pivot(pivot.x, pivot.y);
        }

        private static Native.Texture BuildTexture (Sprite sprite, Color32[] colors) => new() {
            Width = (uint)sprite.rect.width,
            Height = (uint)sprite.rect.height,
            Pixels = BuildPixels(sprite, colors)
        };

        private static Native.Pixel[] BuildPixels (Sprite sprite, Color32[] colors)
        {
            // Both structs are packed RGBA bytes; bulk copying avoids a managed call per pixel.
            var span = MemoryMarshal.Cast<Color32, Native.Pixel>(colors.AsSpan());
            var w = (int)sprite.rect.width;
            var h = (int)sprite.rect.height;
            if (w == sprite.texture.width && w * h == colors.Length) return span.ToArray();
            var pixels = new Native.Pixel[w * h];
            var start = (int)sprite.rect.y * sprite.texture.width + (int)sprite.rect.x;
            for (int y = 0; y < h; y++)
                span.Slice(start + y * sprite.texture.width, w).CopyTo(pixels.AsSpan(y * w, w));
            return pixels;
        }

        private static void EnsureReadable (TextureImporter importer)
        {
            if (importer.isReadable && !importer.crunchedCompression && !EditorUtility.IsDirty(importer)) return;
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
