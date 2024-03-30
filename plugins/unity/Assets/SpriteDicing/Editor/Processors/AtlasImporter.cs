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

        public string Write (byte[] bytes)
        {
            var filePath = BuildFilePath();
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

        public Texture2D Import (string filePath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            settings.ApplyExistingOrDefault(importer);
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        }

        private string BuildFilePath ()
        {
            var index = 0;
            var path = string.Empty;
            do { path = $"{basePath} {++index:000}.png"; } while (File.Exists(path));
            return path;
        }
    }
}
