using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Serializes and imports raw bytes of the atlases as <see cref="Texture2D"/> assets.
    /// </summary>
    public class AtlasImporter
    {
        private readonly string basePath;
        private readonly TextureSettings settings;
        private readonly int maxSize;

        public AtlasImporter (string basePath, TextureSettings settings, int maxSize)
        {
            this.basePath = basePath;
            this.settings = settings;
            this.maxSize = maxSize;
        }

        public string Save (Native.Texture atlas)
        {
            var asset = new Texture2D((int)atlas.Width, (int)atlas.Height);
            asset.SetPixels32(BuildColors(atlas.Pixels));
            asset.Apply(false, true);
            var path = BuildFilePath();
            AssetDatabase.CreateAsset(asset, path);
            return path;
        }

        public Texture2D Import (string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            settings.ApplyExistingOrDefault(importer);
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private string BuildFilePath ()
        {
            var index = 0;
            var path = string.Empty;
            do { path = $"{basePath} {++index:000}.png"; } while (File.Exists(path));
            return path;
        }

        private Color32[] BuildColors (IReadOnlyList<Native.Pixel> pixels)
        {
            var colors = new Color32[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
            {
                var p = pixels[i];
                colors[i] = new Color32(p.R, p.G, p.B, p.A);
            }
            return colors;
        }
    }
}
