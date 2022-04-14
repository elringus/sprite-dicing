using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    public class TextureSettings
    {
        private const string defaultPlatformId = "default";
        private static readonly string[] platformIds = GetPlatformIds();
        private readonly TextureImporterSettings @base = new TextureImporterSettings();
        private readonly Dictionary<string, TextureImporterPlatformSettings> platforms =
            new Dictionary<string, TextureImporterPlatformSettings>();

        public void TryImportExisting (Texture texture)
        {
            if (!TryGetTextureImporter(texture, out var importer)) return;
            importer.ReadTextureSettings(@base);
            foreach (var platformId in platformIds)
                platforms[platformId] = importer.GetPlatformTextureSettings(platformId);
            platforms[defaultPlatformId] = importer.GetDefaultPlatformTextureSettings();
        }

        public void ApplyExistingOrDefault (TextureImporter importer)
        {
            if (platforms.Count == 0) ApplyDefault(importer);
            else ApplyExisting(importer);
        }

        private bool TryGetTextureImporter (Texture texture, out TextureImporter importer)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            return importer != null;
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
            foreach (var kv in platforms)
                if (kv.Key == defaultPlatformId || kv.Value.overridden)
                    importer.SetPlatformTextureSettings(kv.Value);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static string[] GetPlatformIds ()
        {
            // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.4/Editor/Mono/BuildPipeline/BuildPlatform.cs

            Type platformsType = default, platformType = default;
            foreach (var type in Assembly.GetAssembly(typeof(Editor)).GetTypes())
                if (type.Name == "BuildPlatforms") platformsType = type;
                else if (type.Name == "BuildPlatform") platformType = type;
                else if (platformsType != null && platformType != null) break;
            var platformsInstance = platformsType.GetProperty("instance").GetValue(null);
            var validPlatforms = (IEnumerable<object>)platformsType
                .GetMethod("GetValidPlatforms", Array.Empty<Type>())
                .Invoke(platformsInstance, null);
            return validPlatforms.Select(p => (string)platformType.GetField("name").GetValue(p)).ToArray();
        }
    }
}
