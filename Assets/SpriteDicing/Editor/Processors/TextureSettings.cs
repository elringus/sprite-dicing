using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    public class TextureSettings
    {
        private readonly TextureImporterSettings @base = new TextureImporterSettings();
        private TextureImporterPlatformSettings platform;

        public void TryImportExisting (Texture texture)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer) return;
            importer.ReadTextureSettings(@base);
            platform = importer.GetDefaultPlatformTextureSettings();
        }

        public void ApplyExistingOrDefault (TextureImporter importer)
        {
            if (platform == null) ApplyDefault(importer);
            else ApplyExisting(importer);
        }

        private void ApplyDefault (TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private void ApplyExisting (TextureImporter importer)
        {
            importer.SetTextureSettings(@base);
            importer.SetPlatformTextureSettings(platform);
        }
    }
}
