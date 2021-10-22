using System.Linq;
using UnityEditor;
using UnityEngine;
using static SpriteDicing.Editors.EditorProperties;

namespace SpriteDicing.Editors
{
    [CustomEditor(typeof(DicedSpriteAtlas))]
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
        private static readonly int[] atlasLimitValues = { 1024, 2048, 4096, 8192 };
        private static readonly GUIContent[] diceUnitSizeLabels = diceUnitSizeValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
        private static GUIStyle richLabelStyle;

        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            InitializeRichStyle();
            DrawDataGUI();
            DrawPivotGUI();
            DrawSizeGUI();
            PPUProperty.floatValue = Mathf.Max(.001f, EditorGUILayout.FloatField(pixelsPerUnitContent, PPU));
            EditorGUILayout.IntPopup(UnitSizeProperty, diceUnitSizeLabels, diceUnitSizeValues, diceUnitSizeContent);
            DrawPaddingSlider();
            EditorGUILayout.Slider(UVInsetProperty, 0f, .5f, uvInsetContent);
            DrawInputFolderGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable () => InitializeProperties(serializedObject);

        private void InitializeRichStyle ()
        {
            if (richLabelStyle != null) return;
            richLabelStyle = new GUIStyle(GUI.skin.label);
            richLabelStyle.richText = true;
        }

        private void DrawDataGUI ()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(TexturesProperty, true);
            EditorGUILayout.PropertyField(SpritesProperty, true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField(ratioContent, new GUIContent(LastRatioValue), richLabelStyle);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(DecoupleSpriteDataProperty, decoupleSpriteDataContent);
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
            var targetAtlas = target as DicedSpriteAtlas;
            if (!targetAtlas) return GUIContent.none;
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
            EditorGUI.BeginDisabledGroup(KeepOriginalPivot);
            DefaultPivotProperty.vector2Value = EditorGUI.Vector2Field(rect, string.Empty, DefaultPivot);
            EditorGUI.EndDisabledGroup();
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
            var popupLabels = atlasLimitValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
            EditorGUI.IntPopup(rect, AtlasSizeLimitProperty, popupLabels, atlasLimitValues, GUIContent.none);
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
            EditorGUI.BeginDisabledGroup(!IncludeSubfolders);
            ToggleLeftGUI(rect, PrependSubfolderNamesProperty, prependSubfolderNamesContent);
            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button(GetBuildButtonContent(), EditorStyles.miniButton))
                new AtlasBuilder(serializedObject).Build();
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
    }
}
