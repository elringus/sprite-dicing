using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    /// <summary>
    /// Responsible for serializing textures as PNG assets.
    /// </summary>
    public class TextureSerializer : ITextureSerializer
    {
        private readonly string basePath;
        private readonly TextureSettings settings;

        public TextureSerializer (string basePath, TextureSettings settings)
        {
            this.basePath = basePath;
            this.settings = settings;
        }

        public Texture2D Serialize (Texture2D texture)
        {
            var filePath = BuildFilePath();
            SaveAsPNG(texture, filePath);
            var png = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            var maxSize = Mathf.NextPowerOfTwo(Mathf.Max(texture.width, texture.height));
            ApplyImportSettings(filePath, maxSize);
            Object.DestroyImmediate(texture);
            return png;
        }

        private string BuildFilePath ()
        {
            var index = 0;
            var path = string.Empty;
            do { path = $"{basePath} {++index:000}.png"; } while (File.Exists(path));
            return path;
        }

        private void SaveAsPNG (Texture2D texture, string filePath)
        {
            var bytes = texture.EncodeToPNG();
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
