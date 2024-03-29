using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Serializes raw bytes as <see cref="Texture2D"/> assets.
    /// </summary>
    public class TextureSerializer
    {
        private readonly string basePath;
        private readonly TextureSettings settings;

        public TextureSerializer (string basePath, TextureSettings settings)
        {
            this.basePath = basePath;
            this.settings = settings;
        }

        public Texture2D Serialize (byte[] bytes)
        {
            var filePath = BuildFilePath();
            WriteBytes(bytes, filePath);
            var png = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            var maxSize = Mathf.NextPowerOfTwo(Mathf.Max(png.width, png.height));
            ApplyImportSettings(filePath, maxSize);
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

        private void ApplyImportSettings (string filePath, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            settings.ApplyExistingOrDefault(importer);
            importer.maxTextureSize = maxSize;
            AssetDatabase.ImportAsset(filePath);
        }
    }
}
