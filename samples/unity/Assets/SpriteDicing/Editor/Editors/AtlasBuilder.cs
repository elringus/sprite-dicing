using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static SpriteDicing.Editors.EditorProperties;

namespace SpriteDicing.Editors
{
    public class AtlasBuilder
    {
        private readonly SerializedObject serializedObject;
        private readonly DicedSpriteAtlas target;
        private readonly string atlasPath;

        private double? buildStartTime;

        public AtlasBuilder (SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            target = serializedObject.targetObject as DicedSpriteAtlas;
            atlasPath = AssetDatabase.GetAssetPath(target);
        }

        public void Build ()
        {
            try
            {
                var sourceTextures = CollectSourceTextures();
                var dicedTextures = DiceTextures(sourceTextures);
                var atlasTextures = PackTextures(dicedTextures);
                BuildDicedSprites(atlasTextures);
                UpdateCompressionRatio(sourceTextures, atlasTextures);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to build diced sprite atlas. {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                GUIUtility.ExitGUI();
            }
        }

        private SourceTexture[] CollectSourceTextures ()
        {
            DisplayProgressBar("Collecting source textures...", .0f);
            var inputFolderPath = AssetDatabase.GetAssetPath(InputFolder);
            var texturePaths = TextureFinder.FindAt(inputFolderPath, IncludeSubfolders);
            var loader = new TextureLoader(PrependSubfolderNames ? inputFolderPath : null);
            return texturePaths.Select(loader.Load).ToArray();
        }

        private List<DicedTexture> DiceTextures (IReadOnlyList<SourceTexture> sourceTextures)
        {
            var dicer = new TextureDicer(UnitSize, Padding, TrimTransparent);
            var dicedTextures = new List<DicedTexture>();
            for (int i = 0; i < sourceTextures.Count; i++)
            {
                DisplayProgressBar("Dicing textures...", .5f * i / sourceTextures.Count);
                dicedTextures.Add(dicer.Dice(sourceTextures[i]));
            }
            return dicedTextures;
        }

        private List<AtlasTexture> PackTextures (IReadOnlyList<DicedTexture> dicedTextures)
        {
            DisplayProgressBar("Packing dices...", .5f);
            var textureSettings = GetExistingAtlasTextureSettings();
            DeleteExistingAtlasTextures();
            var basePath = atlasPath.Substring(0, atlasPath.LastIndexOf(".", StringComparison.Ordinal));
            var serializer = new TextureSerializer(basePath, textureSettings);
            var packer = new TexturePacker(serializer, UVInset, ForceSquare, ForcePot, AtlasSizeLimit, UnitSize, Padding);
            var atlasTextures = packer.Pack(dicedTextures);
            SaveAtlasTextures(atlasTextures);
            return atlasTextures;

            TextureSettings GetExistingAtlasTextureSettings ()
            {
                var settings = new TextureSettings();
                if (TexturesProperty.arraySize <= 0) return settings;
                var texture = TexturesProperty.GetArrayElementAtIndex(0).objectReferenceValue as Texture;
                if (texture) settings.TryImportExisting(texture);
                return settings;
            }

            void DeleteExistingAtlasTextures ()
            {
                for (int i = TexturesProperty.arraySize - 1; i >= 0; i--)
                {
                    var texture = TexturesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (!texture) continue;
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(texture));
                    UnityEngine.Object.DestroyImmediate(texture, true);
                }
                TexturesProperty.arraySize = 0;
            }

            void SaveAtlasTextures (IReadOnlyList<AtlasTexture> textures)
            {
                TexturesProperty.arraySize = textures.Count;
                for (int i = 0; i < textures.Count; i++)
                    TexturesProperty.GetArrayElementAtIndex(i).objectReferenceValue = textures[i].Texture;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void BuildDicedSprites (IReadOnlyCollection<AtlasTexture> atlasTextures)
        {
            var sprites = new List<Sprite>();
            var builder = new SpriteBuilder(PPU, DefaultPivot, KeepOriginalPivot);
            float total = atlasTextures.Sum(a => a.DicedTextures.Count), built = 0;
            foreach (var atlasTexture in atlasTextures)
            foreach (var dicedTexture in atlasTexture.DicedTextures)
            {
                DisplayProgressBar("Building diced sprites...", .5f + .5f * ++built / total);
                sprites.Add(builder.Build(atlasTexture, dicedTexture));
            }
            new DicedSpriteSerializer(serializedObject).Serialize(sprites);
        }

        private void UpdateCompressionRatio (IEnumerable<SourceTexture> sourceTextures, IEnumerable<AtlasTexture> atlasTextures)
        {
            var sourceSize = sourceTextures.Sum(t => GetAssetSize(t.Texture));
            var atlasSize = atlasTextures.Sum(t => GetAssetSize(t.Texture));
            var dataSize = GetDataSize();
            var ratio = sourceSize / (float)(atlasSize + dataSize);
            var color = ratio > 2 ? EditorGUIUtility.isProSkin ? "lime" : "green" : ratio > 1 ? "yellow" : "red";
            LastRatioValueProperty.stringValue = $"{sourceSize} KB / ({atlasSize} KB + {dataSize} KB) = <color={color}>{ratio:F2}</color>";
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            long GetDataSize ()
            {
                var size = GetAssetSize(target);
                if (DecoupleSpriteData)
                    for (int i = SpritesProperty.arraySize - 1; i >= 0; i--)
                        size += GetAssetSize(SpritesProperty.GetArrayElementAtIndex(i).objectReferenceValue);
                return size / (EditorSettings.serializationMode == SerializationMode.ForceText ? 2 : 1);
            }

            long GetAssetSize (UnityEngine.Object asset)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (!File.Exists(assetPath)) return 0;
                return new FileInfo(assetPath).Length / 1024;
            }
        }

        private void DisplayProgressBar (string activity, float progress)
        {
            if (!buildStartTime.HasValue) buildStartTime = EditorApplication.timeSinceStartup;
            var elapsed = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - buildStartTime.Value);
            var title = $"Building Diced Atlas ({elapsed:mm\\:ss})";
            if (EditorUtility.DisplayCancelableProgressBar(title, activity, progress))
                throw new OperationCanceledException("Diced sprite atlas building was canceled by the user.");
        }
    }
}
