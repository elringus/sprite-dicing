using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static NUnit.Framework.Assert;

namespace SpriteDicing.Test
{
    public class TextureSerializerTest
    {
        private const string tempFolder = "Temp";
        private const string tempRoot = Helpers.TextureFolderPath + "/" + tempFolder;

        private string basePath;
        private TextureSerializer serializer;

        [SetUp]
        public void SetUp ()
        {
            AssetDatabase.CreateFolder(Helpers.TextureFolderPath, tempFolder);
            basePath = $"{tempRoot}/{Guid.NewGuid():N}";
            serializer = new TextureSerializer(basePath);
        }

        [TearDown]
        public void TearDown ()
        {
            AssetDatabase.DeleteAsset(tempRoot);
        }

        [Test]
        public void SerializedTextureIsSavedAsPng ()
        {
            var png = Serialize();
            IsTrue(AssetDatabase.IsMainAsset(png));
            IsTrue(AssetDatabase.GetAssetPath(png).EndsWith(".png"));
        }

        [Test]
        public void SerializedEqualsOriginalTexture ()
        {
            var path = AssetDatabase.GetAssetPath(Serialize(Color.blue, true));
            var png = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            AreEqual(Color.blue, png.GetPixel(0, 0));
        }

        [Test]
        public void ImportSettingsAreApplied ()
        {
            var importer = GetImporter(Serialize());
            AreEqual(TextureImporterType.Default, importer.textureType);
            IsTrue(importer.alphaIsTransparency);
            IsFalse(importer.mipmapEnabled);
            AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            AreEqual(1, importer.maxTextureSize);
        }

        [Test]
        public void MultipleTexturesNamedCorrectly ()
        {
            Serialize();
            Serialize();
            IsTrue(File.Exists($"{basePath} 001.png"));
            IsTrue(File.Exists($"{basePath} 002.png"));
        }

        private Texture2D Serialize (Color color = default, bool readable = false)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            var png = serializer.Serialize(texture);
            if (readable) MakeReadable(png);
            return png;
        }

        private void MakeReadable (Texture2D png)
        {
            var importer = GetImporter(png);
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private TextureImporter GetImporter (Texture2D png)
        {
            var path = AssetDatabase.GetAssetPath(png);
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }
    }
}
