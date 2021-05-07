using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static SpriteDicing.EditorProperties;

namespace SpriteDicing
{
    public class AtlasBuilder
    {
        private readonly SerializedObject serializedObject;
        private readonly DicedSpriteAtlas target;
        private readonly string atlasPath;

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
            var dicer = new TextureDicer(UnitSize, Padding);
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
            DeleteAtlasTextures();
            var basePath = atlasPath.Substring(0, atlasPath.LastIndexOf(".asset", StringComparison.Ordinal));
            var textureSerializer = new TextureSerializer(basePath);
            var texturePacker = new TexturePacker(textureSerializer, UVInset, ForceSquare, AtlasSizeLimit, UnitSize, Padding);
            var atlasTextures = texturePacker.Pack(dicedTextures);
            SaveAtlasTextures(atlasTextures);
            return atlasTextures;
        }

        private void DeleteAtlasTextures ()
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

        private void SaveAtlasTextures (IReadOnlyList<AtlasTexture> textures)
        {
            TexturesProperty.arraySize = textures.Count;
            for (int i = 0; i < textures.Count; i++)
                TexturesProperty.GetArrayElementAtIndex(i).objectReferenceValue = textures[i].Texture;
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
            SaveDicedSprites(sprites);
        }

        private void SaveDicedSprites (IEnumerable<Sprite> sprites)
        {
            if (DecoupleSpriteData) SaveDecoupled();
            else SaveEmbedded();
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            void SaveEmbedded ()
            {
                DeleteDecoupledSprites();
                var newSprites = new List<Sprite>(sprites);
                UpdatedEmbeddedSprites(newSprites);
                foreach (var sprite in newSprites)
                    AssetDatabase.AddObjectToAsset(sprite, target);
                SetSpriteValues(newSprites, false);
            }

            void SaveDecoupled ()
            {
                DeleteEmbeddedSprites();
                var folderPath = GetOrCreateGeneratedSpritesFolder();
                var newSprites = new List<Sprite>(sprites);
                UpdatedDecoupledSprites(folderPath, newSprites);
                foreach (var newSprite in newSprites)
                    AssetDatabase.CreateAsset(newSprite, Path.Combine(folderPath, $"{newSprite.name}.asset"));
                GeneratedSpritesFolderGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(folderPath);
                SetSpriteValues(newSprites, true);
            }
        }

        private string GetOrCreateGeneratedSpritesFolder ()
        {
            var existingPath = AssetDatabase.GUIDToAssetPath(GeneratedSpritesFolderGuid);
            if (AssetDatabase.IsValidFolder(existingPath)) return existingPath;
            var parentPath = Path.GetDirectoryName(atlasPath);
            var folderName = Path.GetFileNameWithoutExtension(atlasPath);
            var newPath = Path.Combine(parentPath, folderName);
            Directory.CreateDirectory(newPath);
            return newPath;
        }

        private void UpdatedDecoupledSprites (string folderPath, List<Sprite> newSprites)
        {
            foreach (var path in Directory.GetFiles(folderPath, "*.asset", SearchOption.TopDirectoryOnly))
            {
                if (newSprites.Find(s => s.name == Path.GetFileNameWithoutExtension(path)) is Sprite newSprite)
                {
                    EditorUtility.CopySerialized(newSprite, AssetDatabase.LoadAssetAtPath<Sprite>(path));
                    newSprites.Remove(newSprite);
                }
                else AssetDatabase.DeleteAsset(path);
            }
        }

        private void UpdatedEmbeddedSprites (List<Sprite> newSprites)
        {
            for (int i = SpritesProperty.arraySize - 1; i >= 0; i--)
            {
                var oldSprite = SpritesProperty.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                if (!oldSprite) continue;
                if (newSprites.Find(s => s.name == oldSprite.name) is Sprite newSprite)
                {
                    EditorUtility.CopySerialized(newSprite, oldSprite);
                    newSprites.Remove(newSprite);
                }
                else UnityEngine.Object.DestroyImmediate(oldSprite, true);
            }
            AssetDatabase.SaveAssets();
        }

        private void DeleteDecoupledSprites ()
        {
            var folderPath = AssetDatabase.GUIDToAssetPath(GeneratedSpritesFolderGuid);
            if (AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.DeleteAsset(folderPath);
        }

        private void DeleteEmbeddedSprites ()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(atlasPath))
                UnityEngine.Object.DestroyImmediate(asset, true);
            AssetDatabase.SaveAssets();
        }

        private void SetSpriteValues (IEnumerable<Sprite> values, bool clear)
        {
            var objectType = typeof(DicedSpriteAtlas);
            var fieldInfo = objectType.GetField(SpritesProperty.name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo is null) throw new Exception();
            var list = (List<Sprite>)fieldInfo.GetValue(target);
            if (clear) list.Clear();
            list.AddRange(values);
            list.RemoveAll(item => !item || item == null);
            var copiedProperty = new SerializedObject(target).FindProperty(SpritesProperty.name);
            SpritesProperty.serializedObject.CopyFromSerializedProperty(copiedProperty);
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
            if (EditorUtility.DisplayCancelableProgressBar("Building Diced Sprite Atlas", activity, progress))
                throw new OperationCanceledException("Diced sprite atlas building was canceled by the user.");
        }
    }
}
