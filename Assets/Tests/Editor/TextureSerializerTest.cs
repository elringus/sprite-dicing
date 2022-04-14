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
        private TextureSettings textureSettings;
        private TextureSerializer serializer;

        [SetUp]
        public void SetUp ()
        {
            AssetDatabase.CreateFolder(Helpers.TextureFolderPath, tempFolder);
            basePath = $"{tempRoot}/{Guid.NewGuid():N}";
            textureSettings = new TextureSettings();
            serializer = new TextureSerializer(basePath, textureSettings);
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
        public void DefaultImportSettingsAreApplied ()
        {
            var importer = GetImporter(Serialize());
            AreEqual(TextureImporterType.Default, importer.textureType);
            IsTrue(importer.alphaIsTransparency);
            IsFalse(importer.mipmapEnabled);
            AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            AreEqual(1, importer.maxTextureSize);
        }

        [Test]
        public void ExistingImportSettingsArePreserved ()
        {
            var otherTexture = Serialize();
            var otherImporter = GetImporter(otherTexture);
            otherImporter.alphaIsTransparency = false;
            otherImporter.textureCompression = TextureImporterCompression.Compressed;
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Serialize());
            IsFalse(importer.alphaIsTransparency);
            AreEqual(TextureImporterCompression.Compressed, importer.textureCompression);
        }

        [Test]
        public void ExistingPlatformSpecificSettingsArePreserved ()
        {
            var otherPlatform = "Standalone";
            var otherTexture = Serialize();
            var otherImporter = GetImporter(otherTexture);
            var otherSettings = new TextureImporterPlatformSettings {
                name = otherPlatform,
                overridden = true,
                textureCompression = TextureImporterCompression.Compressed
            };
            otherImporter.SetPlatformTextureSettings(otherSettings);
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Serialize());
            var settings = importer.GetPlatformTextureSettings(otherPlatform);
            AreEqual(TextureImporterCompression.Compressed, settings.textureCompression);
        }

        [Test]
        public void NonOverriddenPlatformSpecificSettingsAreIgnored ()
        {
            var otherPlatform = "Standalone";
            var otherTexture = Serialize();
            var otherImporter = GetImporter(otherTexture);
            var otherSettings = new TextureImporterPlatformSettings {
                name = otherPlatform,
                overridden = false,
                textureCompression = TextureImporterCompression.Compressed
            };
            otherImporter.SetPlatformTextureSettings(otherSettings);
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Serialize());
            var settings = importer.GetPlatformTextureSettings(otherPlatform);
            AreEqual(TextureImporterCompression.Uncompressed, settings.textureCompression);
        }

        [Test]
        public void SettingsFromInvalidObjectAreIgnored ()
        {
            var obj = new UnityEngine.Object();
            textureSettings.TryImportExisting(obj as Texture);
            DoesNotThrow(() => Serialize());
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
