using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

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

        public string Save (Native.Texture atlas, string path = null)
        {
            if (string.IsNullOrEmpty(path)) path = BuildFilePath();
            var png = ImageConversion.EncodeArrayToPNG(atlas.Pixels,
                GraphicsFormat.R8G8B8A8_UNorm, atlas.Width, atlas.Height);
            File.WriteAllBytes(path, png);
            return path;
        }

        public Texture2D Import (string path)
        {
            if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
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
            do { path = $"{basePath} {++index:000}.png"; }
            while (File.Exists(path));
            return path;
        }
    }
}
