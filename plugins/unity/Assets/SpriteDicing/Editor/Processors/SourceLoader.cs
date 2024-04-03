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

        public SourceSprite Load (string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
                throw new ArgumentNullException(nameof(texturePath));
            return new SourceSprite {
                Native = new() {
                    Id = BuildID(texturePath),
                    Texture = BuildTexture(texturePath),
                    Pivot = GetPivot(texturePath)
                },
                Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)
            };
        }

        private string BuildID (string path)
        {
            if (!Path.HasExtension(path))
                throw new ArgumentException($"Invalid source texture path: {path}", nameof(path));
            if (!path.Contains(root))
                throw new ArgumentException($"Name root `{root}` is not valid for `{path}` path.", nameof(path));
            var local = path[(root.Length + 1)..];
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

        private Native.Texture BuildTexture (string texturePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            return new Native.Texture {
                Width = (uint)asset.width,
                Height = (uint)asset.height,
                Pixels = BuildPixels(asset.GetPixels32())
            };
        }

        private Native.Pixel[] BuildPixels (Color32[] colors)
        {
            var pixels = new Native.Pixel[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                pixels[i] = new Native.Pixel {
                    R = c.r,
                    G = c.g,
                    B = c.b,
                    A = c.a,
                };
            }
            return pixels;
        }
    }
}
