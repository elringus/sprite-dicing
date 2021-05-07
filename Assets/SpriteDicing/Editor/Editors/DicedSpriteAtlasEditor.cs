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
    [CustomEditor(typeof(DicedSpriteAtlas)), CanEditMultipleObjects]
    public class DicedSpriteAtlasEditor : Editor
    {
        private static readonly GUIContent ratioContent = new GUIContent("Compression Ratio", "Total size of the source textures divided by size of the generated diced atlas textures plus associated sprite data (higher is better).");
        private static readonly GUIContent defaultPivotContent = new GUIContent("Default Pivot", "Relative pivot point position in 0 to 1 range, counting from the bottom-left corner. Can be changed after build for each sprite individually.");
        private static readonly GUIContent keepOriginalPivotContent = new GUIContent("Keep Original", "Whether to preserve original sprites pivot (usable for animations).");
        private static readonly GUIContent decoupleSpriteDataContent = new GUIContent("Decouple Sprite Data", "Whether to save sprite assets in a separate folder instead of adding them as children of the atlas asset.\nWARNING: When rebuilding after changing this option the asset references to previously generated sprites will be lost.");
        private static readonly GUIContent atlasSizeLimitContent = new GUIContent("Atlas Size Limit", "Maximum size of a single generated atlas texture; will generate multiple textures when the limit is reached.");
        private static readonly GUIContent forceSquareContent = new GUIContent("Force Square", "The generated atlas textures will always be square. Less efficient, but required for PVRTC compression.");
        private static readonly GUIContent pixelsPerUnitContent = new GUIContent("Pixels Per Unit", "How many pixels in the sprite correspond to the unit in the world.");
        private static readonly GUIContent diceUnitSizeContent = new GUIContent("Dice Unit Size", "The size of a single diced unit.");
        private static readonly GUIContent paddingContent = new GUIContent("Padding", "The size of a pixel border to add between adjacent diced units inside atlas. Increase to prevent texture bleeding artifacts (usually appear as thin gaps between diced units). Larger values will consume more texture space, but yield better anti-bleeding results. Minimum value of 2 is recommended in most cases. When 2 is not enough to prevent bleeding, consider adding a bit of `UV Inset` before increasing the padding.");
        private static readonly GUIContent uvInsetContent = new GUIContent("UV Inset", "Relative inset of the diced units UV coordinates. Can be used in addition to (or instead of) `Padding` to prevent texture bleeding artifacts. Won't consume any texture space, but higher values could visually distort the final result.");
        private static readonly GUIContent inputFolderContent = new GUIContent("Input Folder", "Asset folder with source sprite textures.");
        private static readonly GUIContent includeSubfoldersContent = new GUIContent("Include Subfolders", "Whether to recursively search for textures inside the input folder.");
        private static readonly GUIContent prependSubfolderNamesContent = new GUIContent("Prepend Names", "Whether to prepend sprite names with the subfolder name. Eg: SubfolderName.SpriteName");
        private static readonly int[] diceUnitSizeValues = { 8, 16, 32, 64, 128, 256 };
        private static readonly GUIContent[] diceUnitSizeLabels = diceUnitSizeValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
        private static GUIStyle richLabelStyle;

        private DicedSpriteAtlas targetAtlas => target as DicedSpriteAtlas;
        private string atlasPath => AssetDatabase.GetAssetPath(target);
        private GUIContent ratioValueContent;

        private void OnEnable ()
        {
            InitializeProperties(serializedObject);
            ratioValueContent = new GUIContent(LastRatioValue);
        }

        #region GUI
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            InitializeRichStyle();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(TexturesProperty, true);
            EditorGUILayout.PropertyField(SpritesProperty, true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField(ratioContent, ratioValueContent, richLabelStyle);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(DecoupleSpriteDataProperty, decoupleSpriteDataContent);
            DrawPivotGUI();
            DrawSizeGUI();
            PPUProperty.floatValue = Mathf.Max(.001f, EditorGUILayout.FloatField(pixelsPerUnitContent, PPU));
            EditorGUILayout.IntPopup(UnitSizeProperty, diceUnitSizeLabels, diceUnitSizeValues, diceUnitSizeContent);
            DrawPaddingSlider();
            EditorGUILayout.Slider(UVInsetProperty, 0f, .5f, uvInsetContent);
            DrawInputFolderGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private void InitializeRichStyle ()
        {
            if (richLabelStyle != null) return;
            richLabelStyle = new GUIStyle(GUI.skin.label);
            richLabelStyle.richText = true;
        }

        private void DrawPaddingSlider ()
        {
            var maxValue = UnitSize / 2;
            var value = EditorGUILayout.IntSlider(paddingContent, Padding, 0, maxValue);
            var nearestEven = value % 2 == 0 ? value : Mathf.Min(value + 1, maxValue);
            PaddingProperty.intValue = nearestEven;
        }

        private GUIContent GetBuildButtonContent ()
        {
            var name = targetAtlas.Sprites.Count > 0 ? "Rebuild Atlas" : "Build Atlas";
            var tooltip = InputFolder ? "" : "Select input directory to build atlas.";
            return new GUIContent(name, tooltip);
        }

        private void DrawPivotGUI ()
        {
            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, defaultPivotContent);
            rect.width = Mathf.Max(50, (rect.width - 4) / 2);
            using (new EditorGUI.DisabledScope(KeepOriginalPivot))
                DefaultPivotProperty.vector2Value = EditorGUI.Vector2Field(rect, string.Empty, DefaultPivot);
            rect.x += rect.width + 5;
            ToggleLeftGUI(rect, KeepOriginalPivotProperty, keepOriginalPivotContent);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSizeGUI ()
        {
            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, atlasSizeLimitContent);
            rect.width = Mathf.Max(50, (rect.width - 4) / 2);
            var popupValues = new[] { 1024, 2048, 4096, 8192 };
            var popupLabels = popupValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
            EditorGUI.IntPopup(rect, AtlasSizeLimitProperty, popupLabels, popupValues, GUIContent.none);
            rect.x += rect.width + 5;
            ToggleLeftGUI(rect, ForceSquareProperty, forceSquareContent);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInputFolderGUI ()
        {
            var folderObject = EditorGUI.ObjectField(EditorGUILayout.GetControlRect(),
                inputFolderContent, InputFolder, typeof(DefaultAsset), false);
            if (folderObject == null || AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folderObject)))
                InputFolderProperty.objectReferenceValue = folderObject;

            EditorGUI.BeginDisabledGroup(!InputFolder);

            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, new GUIContent(" "));
            rect.width = Mathf.Max(50, (rect.width - 4) / 2);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeftGUI(rect, IncludeSubfoldersProperty, includeSubfoldersContent);
            rect.x += rect.width + 5;
            using (new EditorGUI.DisabledScope(!IncludeSubfolders))
                ToggleLeftGUI(rect, PrependSubfolderNamesProperty, prependSubfolderNamesContent);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button(GetBuildButtonContent(), EditorStyles.miniButton))
                BuildAtlas();
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private static void ToggleLeftGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            var toggleValue = property.boolValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            toggleValue = EditorGUI.ToggleLeft(position, label, toggleValue);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
                property.boolValue = property.hasMultipleDifferentValues || !property.boolValue;
            EditorGUI.showMixedValue = false;
        }
        #endregion

        private void BuildAtlas ()
        {
            try
            {
                var sourceTextures = CollectSourceTextures();
                var dicedTextures = DiceTextures(sourceTextures);
                var atlasTextures = PackTextures(dicedTextures);
                BuildDicedSprites(atlasTextures);
                UpdateRatio(sourceTextures, atlasTextures);
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
                DestroyImmediate(texture, true);
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
                else DestroyImmediate(oldSprite, true);
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
                DestroyImmediate(asset, true);
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

        private void UpdateRatio (IEnumerable<SourceTexture> sourceTextures, IEnumerable<AtlasTexture> atlasTextures)
        {
            var sourceSize = sourceTextures.Sum(t => GetAssetSize(t.Texture));
            var atlasSize = atlasTextures.Sum(t => GetAssetSize(t.Texture));
            var dataSize = GetDataSize();
            var ratio = sourceSize / (float)(atlasSize + dataSize);
            var color = ratio > 2 ? EditorGUIUtility.isProSkin ? "lime" : "green" : ratio > 1 ? "yellow" : "red";
            ratioValueContent = new GUIContent($"{sourceSize} KB / ({atlasSize} KB + {dataSize} KB) = <color={color}>{ratio:F2}</color>");
            LastRatioValueProperty.stringValue = ratioValueContent.text;
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

        private static void DisplayProgressBar (string activity, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Building Diced Sprite Atlas", activity, progress))
                throw new OperationCanceledException("Diced sprite atlas building was canceled by the user.");
        }
    }
}
