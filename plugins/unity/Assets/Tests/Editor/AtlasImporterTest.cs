using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static NUnit.Framework.Assert;

namespace SpriteDicing.Test
{
    public class AtlasImporterTest
    {
        private const string tempFolder = "Temp";
        private const string tempRoot = Helpers.TextureFolderPath + "/" + tempFolder;
        private static readonly Native.Texture mockTexture = new() {
            Width = 1,
            Height = 1,
            Pixels = new[] { new Native.Pixel { R = 255, G = 255, B = 255, A = 255 } }
        };

        private string basePath;
        private TextureSettings textureSettings;
        private AtlasImporter importer;

        [SetUp]
        public void SetUp ()
        {
            AssetDatabase.CreateFolder(Helpers.TextureFolderPath, tempFolder);
            basePath = $"{tempRoot}/{Guid.NewGuid():N}";
            textureSettings = new TextureSettings();
            importer = new AtlasImporter(basePath, textureSettings, 1);
        }

        [TearDown]
        public void TearDown ()
        {
            AssetDatabase.DeleteAsset(tempRoot);
        }

        [Test]
        public void SerializedTextureIsSavedAsPng ()
        {
            var png = Import(mockTexture);
            IsTrue(AssetDatabase.IsMainAsset(png));
            IsTrue(AssetDatabase.GetAssetPath(png).EndsWith(".png"));
        }

        [Test]
        public void ContentOfImportedTextureEqualsOriginalContent ()
        {
            var imported = Import(mockTexture);
            var importer = GetImporter(imported);
            importer.isReadable = true;
            importer.SaveAndReimport();
            var importedPixels = imported.GetPixels32();
            CollectionAssert.AreEqual(new Color32[] { new(255, 255, 255, 255) }, importedPixels);
        }

        [Test]
        public void DefaultImportSettingsAreApplied ()
        {
            var importer = GetImporter(Import(mockTexture));
            AreEqual(TextureImporterType.Default, importer.textureType);
            IsTrue(importer.alphaIsTransparency);
            IsFalse(importer.mipmapEnabled);
            AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            AreEqual(1, importer.maxTextureSize);
        }

        [Test]
        public void ExistingImportSettingsArePreserved ()
        {
            var otherTexture = Import(mockTexture);
            var otherImporter = GetImporter(otherTexture);
            otherImporter.alphaIsTransparency = false;
            otherImporter.textureCompression = TextureImporterCompression.Compressed;
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Import(mockTexture));
            IsFalse(importer.alphaIsTransparency);
            AreEqual(TextureImporterCompression.Compressed, importer.textureCompression);
        }

        [Test]
        public void ExistingPlatformSpecificSettingsArePreserved ()
        {
            var otherPlatform = "Standalone";
            var otherTexture = Import(mockTexture);
            var otherImporter = GetImporter(otherTexture);
            var otherSettings = new TextureImporterPlatformSettings {
                name = otherPlatform,
                overridden = true,
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear
            };
            otherImporter.SetPlatformTextureSettings(otherSettings);
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Import(mockTexture));
            var settings = importer.GetPlatformTextureSettings(otherPlatform);
            AreEqual(TextureResizeAlgorithm.Bilinear, settings.resizeAlgorithm);
        }

        [Test]
        public void NonOverriddenPlatformSpecificSettingsAreIgnored ()
        {
            var otherPlatform = "Standalone";
            var otherTexture = Import(mockTexture);
            var otherImporter = GetImporter(otherTexture);
            var otherSettings = new TextureImporterPlatformSettings {
                name = otherPlatform,
                overridden = false,
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear
            };
            otherImporter.SetPlatformTextureSettings(otherSettings);
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Import(mockTexture));
            var settings = importer.GetPlatformTextureSettings(otherPlatform);
            AreEqual(TextureResizeAlgorithm.Mitchell, settings.resizeAlgorithm);
        }

        [Test]
        public void SettingsFromInvalidObjectAreIgnored ()
        {
            var obj = new UnityEngine.Object();
            textureSettings.TryImportExisting(obj as Texture);
            DoesNotThrow(() => Import(mockTexture));
        }

        [Test]
        public void MultipleTexturesNamedCorrectly ()
        {
            Import(mockTexture);
            Import(mockTexture);
            IsTrue(File.Exists($"{basePath} 001.png"));
            IsTrue(File.Exists($"{basePath} 002.png"));
        }

        private Texture2D Import (Native.Texture texture)
        {
            var path = importer.Save(texture);
            AssetDatabase.Refresh();
            return importer.Import(path);
        }

        private TextureImporter GetImporter (Texture2D png)
        {
            var path = AssetDatabase.GetAssetPath(png);
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }
    }
}
