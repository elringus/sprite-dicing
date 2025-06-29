using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

        public IEnumerable<SourceSprite> Load (string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
                throw new ArgumentNullException(nameof(texturePath));
            if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
                yield break;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                yield return new SourceSprite {
                    Native = new() {
                        Id = BuildID(texturePath),
                        Texture = BuildTexture(texturePath),
                        Pivot = GetPivot(texturePath)
                    },
                    Texture = texture
                };
                yield break;
            }
            var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>();
            foreach (var sprite in sprites)
            {
                yield return new SourceSprite {
                    Native = new() {
                        Id = $"{BuildID(texturePath)}{separator}{sprite.name}",       //For consistency because since it is a sub-sprite of type Multiple.
                        Texture = BuildTexture(texturePath, sprite.rect),
                        Pivot = GetPivot(sprite)
                    },
                    Texture = texture
                };
            }
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
            return GetPivot(AssetDatabase.LoadAssetAtPath<Sprite>(texturePath));
        }

        private Native.Pivot? GetPivot(Sprite sprite)
        {
            if (!keepPivot || !sprite) return null;
            var pivot = sprite.pivot / sprite.rect.size;
            return new Native.Pivot { X = pivot.x, Y = pivot.y };
        }

        private Native.Texture BuildTexture (string texturePath, Rect? region = null)
        {
            EnsureReadable(texturePath);
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Rect targetRegion;
            if (!region.HasValue) targetRegion = Rect.MinMaxRect(0, 0, asset.width, asset.height);
            else targetRegion = region.Value;
            return new Native.Texture {
                Width = (uint)targetRegion.width,
                Height = (uint)targetRegion.height,
                Pixels = BuildPixels(asset, targetRegion)
            };
        }

        private Native.Pixel[] BuildPixels (Texture2D asset, Rect region)
        {
            int fullWidth = asset.width;
            var colors = asset.GetPixels32(); // GetPixelData is actually slower in editor.
            var pixels = new Native.Pixel[(int)(region.width * region.height)];
            int writeIndex = 0;
            for (int y = (int)region.yMin; y < region.yMax; y++)
            {
                for (int x = (int)region.xMin; x < region.xMax; x++)
                {
                    int index = y * fullWidth + x;
                    var c = colors[index];
                    pixels[writeIndex++] = new Native.Pixel {
                        R = c.r,
                        G = c.g,
                        B = c.b,
                        A = c.a
                    };
                }
            }
            return pixels;
        }

        private void EnsureReadable (string texturePath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.isReadable = true;
            importer.crunchedCompression = false;
            importer.SaveAndReimport();
        }
    }
}
