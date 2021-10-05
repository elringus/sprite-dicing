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

        public TextureSerializer (string basePath)
        {
            this.basePath = basePath;
        }

        public Texture2D Serialize (Texture2D texture)
        {
            var filePath = BuildFilePath();
            SaveAsPNG(texture, filePath);
            var png = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            var maxSize = Mathf.Max(texture.width, texture.height);
            ApplyImportSettings(filePath, Mathf.NextPowerOfTwo(maxSize));
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

        private static void SaveAsPNG (Texture2D texture, string filePath)
        {
            var wrapMode = texture.wrapMode;
            var alphaIsTransparency = texture.alphaIsTransparency;
            var bytes = texture.EncodeToPNG();
            using (var fileStream = File.Create(filePath))
                fileStream.Write(bytes, 0, bytes.Length);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ApplyImportSettings (string filePath, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            AssetDatabase.ImportAsset(filePath);
        }
    }
}
