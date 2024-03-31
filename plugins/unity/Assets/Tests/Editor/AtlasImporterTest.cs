using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static NUnit.Framework.Assert;
using static SpriteDicing.Test.Helpers.Paths;

namespace SpriteDicing.Test
{
    public class AtlasImporterTest
    {
        private const string tempFolder = "Temp";
        private const string tempRoot = Helpers.TextureFolderPath + "/" + tempFolder;

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
            var png = Import(B);
            IsTrue(AssetDatabase.IsMainAsset(png));
            IsTrue(AssetDatabase.GetAssetPath(png).EndsWith(".png"));
        }

        [Test]
        public void SerializedEqualsOriginalTexture ()
        {
            var path = AssetDatabase.GetAssetPath(Import(B));
            var png = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            CollectionAssert.AreEqual(Helpers.Textures.B.GetRawTextureData(), png.GetRawTextureData());
        }

        [Test]
        public void DefaultImportSettingsAreApplied ()
        {
            var importer = GetImporter(Import(B));
            AreEqual(TextureImporterType.Default, importer.textureType);
            IsTrue(importer.alphaIsTransparency);
            IsFalse(importer.mipmapEnabled);
            AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            AreEqual(1, importer.maxTextureSize);
        }

        [Test]
        public void ExistingImportSettingsArePreserved ()
        {
            var otherTexture = Import(B);
            var otherImporter = GetImporter(otherTexture);
            otherImporter.alphaIsTransparency = false;
            otherImporter.textureCompression = TextureImporterCompression.Compressed;
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Import(B));
            IsFalse(importer.alphaIsTransparency);
            AreEqual(TextureImporterCompression.Compressed, importer.textureCompression);
        }

        [Test]
        public void ExistingPlatformSpecificSettingsArePreserved ()
        {
            var otherPlatform = "Standalone";
            var otherTexture = Import(B);
            var otherImporter = GetImporter(otherTexture);
            var otherSettings = new TextureImporterPlatformSettings {
                name = otherPlatform,
                overridden = true,
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear
            };
            otherImporter.SetPlatformTextureSettings(otherSettings);
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Import(B));
            var settings = importer.GetPlatformTextureSettings(otherPlatform);
            AreEqual(TextureResizeAlgorithm.Bilinear, settings.resizeAlgorithm);
        }

        [Test]
        public void NonOverriddenPlatformSpecificSettingsAreIgnored ()
        {
            var otherPlatform = "Standalone";
            var otherTexture = Import(B);
            var otherImporter = GetImporter(otherTexture);
            var otherSettings = new TextureImporterPlatformSettings {
                name = otherPlatform,
                overridden = false,
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear
            };
            otherImporter.SetPlatformTextureSettings(otherSettings);
            otherImporter.SaveAndReimport();

            textureSettings.TryImportExisting(otherTexture);
            var importer = GetImporter(Import(B));
            var settings = importer.GetPlatformTextureSettings(otherPlatform);
            AreEqual(TextureResizeAlgorithm.Mitchell, settings.resizeAlgorithm);
        }

        [Test]
        public void SettingsFromInvalidObjectAreIgnored ()
        {
            var obj = new UnityEngine.Object();
            textureSettings.TryImportExisting(obj as Texture);
            DoesNotThrow(() => Import(B));
        }

        [Test]
        public void MultipleTexturesNamedCorrectly ()
        {
            Import(B);
            Import(B);
            IsTrue(File.Exists($"{basePath} 001.png"));
            IsTrue(File.Exists($"{basePath} 002.png"));
        }

        private Texture2D Import (string texturePath)
        {
            var bytes = File.ReadAllBytes(texturePath);
            var path = importer.Write(bytes);
            AssetDatabase.ImportAsset(path);
            return importer.Import(path);
        }

        private TextureImporter GetImporter (Texture2D png)
        {
            var path = AssetDatabase.GetAssetPath(png);
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }
    }
}
