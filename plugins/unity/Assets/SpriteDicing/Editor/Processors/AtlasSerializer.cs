using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Persists raw bytes of the atlases as <see cref="Texture2D"/> assets.
    /// </summary>
    public class AtlasSerializer
    {
        private readonly string basePath;
        private readonly TextureSettings settings;
        private readonly int maxSize;

        public AtlasSerializer (string basePath, TextureSettings settings, int maxSize)
        {
            this.basePath = basePath;
            this.settings = settings;
            this.maxSize = maxSize;
        }

        public Texture2D Serialize (byte[] bytes)
        {
            var filePath = BuildFilePath();
            WriteBytes(bytes, filePath);
            var png = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            ApplyImportSettings(filePath);
            return png;
        }

        private string BuildFilePath ()
        {
            var index = 0;
            var path = string.Empty;
            do { path = $"{basePath} {++index:000}.png"; } while (File.Exists(path));
            return path;
        }

        private void WriteBytes (byte[] bytes, string filePath)
        {
            using (var fileStream = File.Create(filePath))
                fileStream.Write(bytes, 0, bytes.Length);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ApplyImportSettings (string filePath)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            settings.ApplyExistingOrDefault(importer);
            importer.maxTextureSize = maxSize;
            AssetDatabase.ImportAsset(filePath);
        }
    }
}
