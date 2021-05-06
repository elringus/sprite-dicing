using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

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
        private int unitSize => diceUnitSizeProperty.intValue;
        private int padding => paddingProperty.intValue;
        private float uvInset => uvInsetProperty.floatValue;
        private float ppu => pixelsPerUnitProperty.floatValue;
        private bool forceSquare => forceSquareProperty.boolValue;
        private int atlasSizeLimit => atlasSizeLimitProperty.intValue;
        private UnityEngine.Object inputFolder => inputFolderProperty.objectReferenceValue;
        private bool includeSubfolders => includeSubfoldersProperty.boolValue;
        private bool prependSubfolderNames => prependSubfolderNamesProperty.boolValue;
        private bool keepOriginalPivot => keepOriginalPivotProperty.boolValue;
        private Vector2 defaultPivot => defaultPivotProperty.vector2Value;
        private bool decoupleSpriteData => decoupleSpriteDataProperty.boolValue;

        private SerializedProperty texturesProperty;
        private SerializedProperty spritesProperty;
        private SerializedProperty defaultPivotProperty;
        private SerializedProperty keepOriginalPivotProperty;
        private SerializedProperty decoupleSpriteDataProperty;
        private SerializedProperty atlasSizeLimitProperty;
        private SerializedProperty forceSquareProperty;
        private SerializedProperty pixelsPerUnitProperty;
        private SerializedProperty diceUnitSizeProperty;
        private SerializedProperty paddingProperty;
        private SerializedProperty uvInsetProperty;
        private SerializedProperty inputFolderProperty;
        private SerializedProperty includeSubfoldersProperty;
        private SerializedProperty prependSubfolderNamesProperty;
        private SerializedProperty generatedSpritesFolderGuidProperty;
        private SerializedProperty lastRatioValue;
        private GUIContent ratioValueContent;

        private void OnEnable ()
        {
            texturesProperty = serializedObject.FindProperty("textures");
            spritesProperty = serializedObject.FindProperty("sprites");
            defaultPivotProperty = serializedObject.FindProperty("defaultPivot");
            keepOriginalPivotProperty = serializedObject.FindProperty("keepOriginalPivot");
            decoupleSpriteDataProperty = serializedObject.FindProperty("decoupleSpriteData");
            atlasSizeLimitProperty = serializedObject.FindProperty("atlasSizeLimit");
            forceSquareProperty = serializedObject.FindProperty("forceSquare");
            pixelsPerUnitProperty = serializedObject.FindProperty("pixelsPerUnit");
            diceUnitSizeProperty = serializedObject.FindProperty("diceUnitSize");
            paddingProperty = serializedObject.FindProperty("padding");
            uvInsetProperty = serializedObject.FindProperty("uvInset");
            inputFolderProperty = serializedObject.FindProperty("inputFolder");
            includeSubfoldersProperty = serializedObject.FindProperty("includeSubfolders");
            prependSubfolderNamesProperty = serializedObject.FindProperty("prependSubfolderNames");
            generatedSpritesFolderGuidProperty = serializedObject.FindProperty("generatedSpritesFolderGuid");
            lastRatioValue = serializedObject.FindProperty("lastRatioValue");
            ratioValueContent = new GUIContent(lastRatioValue.stringValue);
        }

        #region GUI
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            InitializeRichStyle();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(texturesProperty, true);
            EditorGUILayout.PropertyField(spritesProperty, true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField(ratioContent, ratioValueContent, richLabelStyle);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(decoupleSpriteDataProperty, decoupleSpriteDataContent);
            DrawPivotGUI();
            DrawSizeGUI();
            pixelsPerUnitProperty.floatValue = Mathf.Max(.001f, EditorGUILayout.FloatField(pixelsPerUnitContent, pixelsPerUnitProperty.floatValue));
            EditorGUILayout.IntPopup(diceUnitSizeProperty, diceUnitSizeLabels, diceUnitSizeValues, diceUnitSizeContent);
            DrawPaddingSlider();
            EditorGUILayout.Slider(uvInsetProperty, 0f, .5f, uvInsetContent);
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
            var maxValue = diceUnitSizeProperty.intValue / 2;
            var value = EditorGUILayout.IntSlider(paddingContent, paddingProperty.intValue, 0, maxValue);
            var nearestEven = value % 2 == 0 ? value : Mathf.Min(value + 1, maxValue);
            paddingProperty.intValue = nearestEven;
        }

        private GUIContent GetBuildButtonContent ()
        {
            var name = targetAtlas.Sprites.Count > 0 ? "Rebuild Atlas" : "Build Atlas";
            var tooltip = inputFolderProperty.objectReferenceValue ? "" : "Select input directory to build atlas.";
            return new GUIContent(name, tooltip);
        }

        private void DrawPivotGUI ()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, -1, defaultPivotContent);
                rect.width = Mathf.Max(50, (rect.width - 4) / 2);
                using (new EditorGUI.DisabledScope(keepOriginalPivotProperty.boolValue))
                    defaultPivotProperty.vector2Value = EditorGUI.Vector2Field(rect, string.Empty, defaultPivotProperty.vector2Value);
                rect.x += rect.width + 5;
                Utilities.ToggleLeftGUI(rect, keepOriginalPivotProperty, keepOriginalPivotContent);
            }
        }

        private void DrawSizeGUI ()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, -1, atlasSizeLimitContent);
                rect.width = Mathf.Max(50, (rect.width - 4) / 2);
                var popupValues = new[] { 1024, 2048, 4096, 8192 };
                var popupLabels = popupValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
                EditorGUI.IntPopup(rect, atlasSizeLimitProperty, popupLabels, popupValues, GUIContent.none);
                rect.x += rect.width + 5;
                Utilities.ToggleLeftGUI(rect, forceSquareProperty, forceSquareContent);
            }
        }

        private void DrawInputFolderGUI ()
        {
            var folderObject = EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), inputFolderContent, inputFolderProperty.objectReferenceValue, typeof(DefaultAsset), false);
            if (folderObject == null || AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folderObject)))
                inputFolderProperty.objectReferenceValue = folderObject;

            EditorGUI.BeginDisabledGroup(!inputFolderProperty.objectReferenceValue);

            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, new GUIContent(" "));
            rect.width = Mathf.Max(50, (rect.width - 4) / 2);
            EditorGUIUtility.labelWidth = 50;
            Utilities.ToggleLeftGUI(rect, includeSubfoldersProperty, includeSubfoldersContent);
            rect.x += rect.width + 5;
            using (new EditorGUI.DisabledScope(!includeSubfoldersProperty.boolValue))
                Utilities.ToggleLeftGUI(rect, prependSubfolderNamesProperty, prependSubfolderNamesContent);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button(GetBuildButtonContent(), EditorStyles.miniButton))
                BuildAtlas();
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
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
            var inputFolderPath = AssetDatabase.GetAssetPath(inputFolder);
            var texturePaths = TextureFinder.FindAt(inputFolderPath, includeSubfolders);
            var loader = new TextureLoader(prependSubfolderNames ? inputFolderPath : null);
            return texturePaths.Select(loader.Load).ToArray();
        }

        private List<DicedTexture> DiceTextures (IReadOnlyList<SourceTexture> sourceTextures)
        {
            var dicer = new TextureDicer(unitSize, padding, ppu);
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
            var texturePacker = new TexturePacker(textureSerializer, uvInset, forceSquare, atlasSizeLimit, unitSize, padding);
            var atlasTextures = texturePacker.Pack(dicedTextures);
            SaveAtlasTextures(atlasTextures);
            return atlasTextures;
        }

        private void DeleteAtlasTextures ()
        {
            for (int i = texturesProperty.arraySize - 1; i >= 0; i--)
            {
                var texture = texturesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!texture) continue;
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(texture));
                DestroyImmediate(texture, true);
            }
            texturesProperty.arraySize = 0;
        }

        private void SaveAtlasTextures (IReadOnlyList<AtlasTexture> textures)
        {
            texturesProperty.arraySize = textures.Count;
            for (int i = 0; i < textures.Count; i++)
                texturesProperty.GetArrayElementAtIndex(i).objectReferenceValue = textures[i].Texture;
        }

        private void BuildDicedSprites (IReadOnlyCollection<AtlasTexture> atlasTextures)
        {
            var sprites = new List<Sprite>();
            var builder = new SpriteBuilder(ppu, defaultPivot, keepOriginalPivot);
            float total = atlasTextures.Sum(a => a.DicedTextures.Count), built = 0;
            foreach (var atlasTexture in atlasTextures)
            foreach (var dicedTexture in atlasTexture.DicedTextures)
            {
                DisplayProgressBar("Building diced sprites...", .5f + ++built / total * .5f);
                sprites.Add(builder.Build(atlasTexture, dicedTexture));
            }
            SaveDicedSprites(sprites);
        }

        private void SaveDicedSprites (IReadOnlyList<Sprite> sprites)
        {
            DisplayProgressBar("Saving diced sprites...", 1f);

            if (!decoupleSpriteData)
            {
                // Delete generated sprites folder (in case it was previously created).
                if (!string.IsNullOrWhiteSpace(generatedSpritesFolderGuidProperty.stringValue))
                {
                    var folderPath = AssetDatabase.GUIDToAssetPath(generatedSpritesFolderGuidProperty.stringValue);
                    if (AssetDatabase.IsValidFolder(folderPath))
                        AssetDatabase.DeleteAsset(folderPath);
                }

                // Update rebuilt sprites to preserve references and delete stale ones.
                var spritesToAdd = new List<Sprite>(sprites);
                for (int i = spritesProperty.arraySize - 1; i >= 0; i--)
                {
                    var oldSprite = spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    if (!oldSprite) continue;
                    var newSprite = spritesToAdd.Find(sprite => sprite.name == oldSprite.name);
                    if (newSprite)
                    {
                        EditorUtility.CopySerialized(newSprite, oldSprite);
                        spritesToAdd.Remove(newSprite);
                    }
                    else DestroyImmediate(oldSprite, true);
                }
                AssetDatabase.SaveAssets(); // Required to delete old sprites before adding new ones.
                foreach (var spriteToAdd in spritesToAdd)
                    AssetDatabase.AddObjectToAsset(spriteToAdd, target);
                spritesProperty.SetListValues(spritesToAdd, false);
            }
            else
            {
                // Delete sprites stored in atlas asset (in case they were previously added).
                foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(atlasPath))
                    DestroyImmediate(asset, true);
                AssetDatabase.SaveAssets(); // Required to remove sub-assets.

                var folderPath = AssetDatabase.GetAssetPath(target).GetBeforeLast("/") + "/" + target.name;
                var dicedSpritesFolder = new FolderAssetHelper(folderPath);
                var savedDicedSprites = dicedSpritesFolder.SetContainedAssets(sprites);
                generatedSpritesFolderGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(folderPath);
                spritesProperty.SetListValues(savedDicedSprites);
            }

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private void UpdateRatio (IEnumerable<SourceTexture> sourceTextures, IEnumerable<AtlasTexture> atlasTextures)
        {
            var sourceSize = sourceTextures.Sum(t => GetAssetSize(t.Texture));
            var atlasSize = atlasTextures.Sum(t => GetAssetSize(t.Texture));
            var dataSize = GetDataSize();
            var ratio = sourceSize / (float)(atlasSize + dataSize);
            var color = ratio > 2 ? EditorGUIUtility.isProSkin ? "lime" : "green" : ratio > 1 ? "yellow" : "red";
            ratioValueContent = new GUIContent($"{sourceSize} KB / ({atlasSize} KB + {dataSize} KB) = <color={color}>{ratio:F2}</color>");
            lastRatioValue.stringValue = ratioValueContent.text;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            long GetDataSize ()
            {
                var size = GetAssetSize(target);
                if (decoupleSpriteData)
                    for (int i = spritesProperty.arraySize - 1; i >= 0; i--)
                        size += GetAssetSize(spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue);
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
