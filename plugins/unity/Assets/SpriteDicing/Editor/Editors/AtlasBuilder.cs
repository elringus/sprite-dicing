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
                var sources = CollectSourceSprites();
                using var diced = Native.Dice(sources.Select(s => s.Native), BuildPrefs());
                var atlases = ImportAtlases(diced.Atlases);
                BuildDicedSprites(diced.Sprites, atlases);
                UpdateCompressionRatio(sources.Select(s => s.Texture), atlases);
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

        private SourceSprite[] CollectSourceSprites ()
        {
            DisplayProgressBar("Collecting source sprites...", .0f);
            var inputFolderPath = AssetDatabase.GetAssetPath(InputFolder);
            var paths = SourceFinder.FindAt(inputFolderPath, IncludeSubfolders);
            var loader = new SourceLoader(inputFolderPath, Separator, KeepOriginalPivot);
            var sources = new SourceSprite[paths.Count];
            for (int i = 0; i < paths.Count; i++)
            {
                var progress = .25f * ((i + 1f) / paths.Count);
                DisplayProgressBar($"Loading source sprites... ({i + 1} of {paths.Count})", progress);
                sources[i] = loader.Load(paths[i]);
            }
            return sources;
        }

        private Native.Prefs BuildPrefs () => new() {
            UnitSize = (uint)UnitSize,
            Padding = (uint)Padding,
            UVInset = UVInset,
            TrimTransparent = TrimTransparent,
            AtlasSizeLimit = (uint)AtlasSizeLimit,
            AtlasSquare = ForceSquare,
            AtlasPOT = ForcePot,
            PPU = PPU,
            Pivot = new Native.Pivot { X = DefaultPivot.x, Y = DefaultPivot.y },
            OnProgress = p => DisplayProgressBar(p.Activity, .25f + p.Ratio / 4)
        };

        private Texture2D[] ImportAtlases (IReadOnlyList<Native.Texture> atlases)
        {
            DisplayProgressBar("Importing atlases...", .5f);
            var textureSettings = GetExistingAtlasTextureSettings();
            DeleteExistingAtlasTextures();
            var basePath = atlasPath[..atlasPath.LastIndexOf(".", StringComparison.Ordinal)];
            var importer = new AtlasImporter(basePath, textureSettings, AtlasSizeLimit);
            var paths = atlases.Select(importer.Save).ToArray();
            AssetDatabase.Refresh();
            var imported = new Texture2D[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                var progress = .5f + .25f * ((i + 1f) / paths.Length);
                DisplayProgressBar($"Importing atlases... ({i + 1} of {paths.Length})", progress);
                imported[i] = importer.Import(paths[i]);
            }
            SaveAtlasTextures(imported);
            return imported;

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

            void SaveAtlasTextures (IReadOnlyList<Texture2D> textures)
            {
                TexturesProperty.arraySize = textures.Count;
                for (int i = 0; i < textures.Count; i++)
                    TexturesProperty.GetArrayElementAtIndex(i).objectReferenceValue = textures[i];
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void BuildDicedSprites (IReadOnlyList<Native.DicedSprite> diced, IReadOnlyList<Texture2D> atlases)
        {
            var sprites = new List<Sprite>();
            var builder = new SpriteBuilder(PPU, atlases);
            for (int i = 0; i < diced.Count; i++)
            {
                var progress = .75f + .25f * ((i + 1f) / diced.Count);
                DisplayProgressBar($"Building diced sprites... ({i + 1} of {diced.Count})", progress);
                sprites.Add(builder.Build(diced[i]));
            }
            new DicedSpriteSerializer(serializedObject).Serialize(sprites);
        }

        private void UpdateCompressionRatio (IEnumerable<Texture2D> sources, IEnumerable<Texture2D> atlases)
        {
            AssetDatabase.SaveAssets();
            var sourceSize = sources.Sum(GetAssetSize);
            var atlasSize = atlases.Sum(GetAssetSize);
            var dataSize = GetDataSize();
            var ratio = sourceSize / (float)(atlasSize + dataSize);
            var color = ratio > 2 ? EditorGUIUtility.isProSkin ? "lime" : "green" : ratio > 1 ? "yellow" : "red";
            LastRatioValueProperty.stringValue = $"{sourceSize} KB / ({atlasSize} KB + {dataSize} KB) = <color={color}>{ratio:F2}</color>";
            LastRatioValueProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(target);

            long GetAssetSize (UnityEngine.Object asset)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (!File.Exists(assetPath)) return 0;
                return new FileInfo(assetPath).Length / 1024;
            }

            long GetDataSize ()
            {
                var size = GetAssetSize(target);
                if (DecoupleSpriteData)
                    for (int i = SpritesProperty.arraySize - 1; i >= 0; i--)
                        size += GetAssetSize(SpritesProperty.GetArrayElementAtIndex(i).objectReferenceValue);
                return size / (EditorSettings.serializationMode == SerializationMode.ForceText ? 2 : 1);
            }
        }

        private void DisplayProgressBar (string activity, float progress)
        {
            buildStartTime ??= EditorApplication.timeSinceStartup;
            var elapsed = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - buildStartTime.Value);
            var title = $"Building Diced Atlas ({elapsed:mm\\:ss})";
            if (EditorUtility.DisplayCancelableProgressBar(title, activity, progress))
                throw new OperationCanceledException("Diced sprite atlas building was canceled by the user.");
        }
    }
}
