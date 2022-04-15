using UnityEditor;
using UnityEngine;

namespace SpriteDicing.Editors
{
    public static class EditorProperties
    {
        public static string GeneratedSpritesFolderGuid => GeneratedSpritesFolderGuidProperty.stringValue;
        public static int UnitSize => UnitSizeProperty.intValue;
        public static int Padding => PaddingProperty.intValue;
        public static float UVInset => UVInsetProperty.floatValue;
        public static float PPU => PPUProperty.floatValue;
        public static bool ForceSquare => ForceSquareProperty.boolValue;
        public static int AtlasSizeLimit => AtlasSizeLimitProperty.intValue;
        public static Object InputFolder => InputFolderProperty.objectReferenceValue;
        public static bool IncludeSubfolders => IncludeSubfoldersProperty.boolValue;
        public static bool PrependSubfolderNames => PrependSubfolderNamesProperty.boolValue;
        public static bool KeepOriginalPivot => KeepOriginalPivotProperty.boolValue;
        public static Vector2 DefaultPivot => DefaultPivotProperty.vector2Value;
        public static bool DecoupleSpriteData => DecoupleSpriteDataProperty.boolValue;
        public static string LastRatioValue => LastRatioValueProperty.stringValue;

        public static SerializedProperty TexturesProperty { get; private set; }
        public static SerializedProperty SpritesProperty { get; private set; }
        public static SerializedProperty DefaultPivotProperty { get; private set; }
        public static SerializedProperty KeepOriginalPivotProperty { get; private set; }
        public static SerializedProperty DecoupleSpriteDataProperty { get; private set; }
        public static SerializedProperty AtlasSizeLimitProperty { get; private set; }
        public static SerializedProperty ForceSquareProperty { get; private set; }
        public static SerializedProperty PPUProperty { get; private set; }
        public static SerializedProperty UnitSizeProperty { get; private set; }
        public static SerializedProperty PaddingProperty { get; private set; }
        public static SerializedProperty UVInsetProperty { get; private set; }
        public static SerializedProperty InputFolderProperty { get; private set; }
        public static SerializedProperty IncludeSubfoldersProperty { get; private set; }
        public static SerializedProperty PrependSubfolderNamesProperty { get; private set; }
        public static SerializedProperty GeneratedSpritesFolderGuidProperty { get; private set; }
        public static SerializedProperty LastRatioValueProperty { get; private set; }

        public static void InitializeProperties (SerializedObject serializedObject)
        {
            TexturesProperty = serializedObject.FindProperty("textures");
            SpritesProperty = serializedObject.FindProperty("sprites");
            DefaultPivotProperty = serializedObject.FindProperty("defaultPivot");
            KeepOriginalPivotProperty = serializedObject.FindProperty("keepOriginalPivot");
            DecoupleSpriteDataProperty = serializedObject.FindProperty("decoupleSpriteData");
            AtlasSizeLimitProperty = serializedObject.FindProperty("atlasSizeLimit");
            ForceSquareProperty = serializedObject.FindProperty("forceSquare");
            PPUProperty = serializedObject.FindProperty("pixelsPerUnit");
            UnitSizeProperty = serializedObject.FindProperty("diceUnitSize");
            PaddingProperty = serializedObject.FindProperty("padding");
            UVInsetProperty = serializedObject.FindProperty("uvInset");
            InputFolderProperty = serializedObject.FindProperty("inputFolder");
            IncludeSubfoldersProperty = serializedObject.FindProperty("includeSubfolders");
            PrependSubfolderNamesProperty = serializedObject.FindProperty("prependSubfolderNames");
            GeneratedSpritesFolderGuidProperty = serializedObject.FindProperty("generatedSpritesFolderGuid");
            LastRatioValueProperty = serializedObject.FindProperty("lastRatioValue");
        }
    }
}
