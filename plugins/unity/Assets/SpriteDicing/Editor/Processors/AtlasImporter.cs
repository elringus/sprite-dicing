using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Imports raw bytes of the atlases as <see cref="Texture2D"/> assets.
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

        public Texture2D Import (byte[] bytes)
        {
            var filePath = BuildFilePath();
            File.WriteAllBytes(filePath, bytes);
            ApplyImportSettings(filePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        }

        private string BuildFilePath ()
        {
            var index = 0;
            var path = string.Empty;
            do { path = $"{basePath} {++index:000}.png"; } while (File.Exists(path));
            return path;
        }

        private void ApplyImportSettings (string filePath)
        {
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            settings.ApplyExistingOrDefault(importer);
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }
    }
}
