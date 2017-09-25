using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityCommon;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DicedSpriteAtlas)), CanEditMultipleObjects]
public class DicedSpriteAtlasEditor : Editor
{
    protected DicedSpriteAtlas TargetAtlas { get { return target as DicedSpriteAtlas; } }

    private SerializedProperty atlasTexture;
    private SerializedProperty dicedSprites;
    private SerializedProperty defaultPivot;
    private SerializedProperty keepOriginalPivot;
    private SerializedProperty decoupleSpriteData;
    private SerializedProperty atlasSizeLimit;
    private SerializedProperty forceSquare;
    private SerializedProperty pixelsPerUnit;
    private SerializedProperty diceUnitSize;
    private SerializedProperty padding;
    private SerializedProperty inputFolder;
    private SerializedProperty includeSubfolders;
    private SerializedProperty prependSubfolderNames;

    private GUIContent dataSizeValueContent;
    private GUIContent dataSizeContent = new GUIContent("Generated Data Size", "Total amount of the generated sprites data (vertices, UVs and triangles). Reduce by increasing Dice Unit Size.");
    private GUIContent defaultPivotContent = new GUIContent("Default Pivot", "Relative pivot point position in 0 to 1 range, counting from the bottom-left corner. Can be changed after build for each sprite individually.");
    private GUIContent keepOriginalPivotContent = new GUIContent("Keep Original", "Whether to preserve original sprites pivot (usable for animations).");
    private GUIContent decoupleSpriteDataContent = new GUIContent("Decouple Sprite Data", "Whether to save sprite assets in a separate folder instead of adding them as childs of the atlas asset.\nWARNING: When rebuilding after changing this option the asset references to previously generated sprites will be lost.");
    private GUIContent atlasSizeLimitContent = new GUIContent("Atlas Size Limit", "Maximum size of the generated atlas texture.");
    private GUIContent forceSquareContent = new GUIContent("Force Square", "The generated atlas textures will always be square. Less efficient, but required for PVRTC compression.");
    private GUIContent pixelsPerUnitContent = new GUIContent("Pixels Per Unit", "How many pixels in the sprite correspond to the unit in the world.");
    private GUIContent diceUnitSizeContent = new GUIContent("Dice Unit Size", "The size of a single diced unit.");
    private GUIContent paddingContent = new GUIContent("Padding", "The pixel gap between adjacent diced units inside atlas. Increase to prevent texture bleeding artefacts when scaling or using mipmaps.");
    private GUIContent inputFolderContent = new GUIContent("Input Folder", "Asset folder with source sprite textures.");
    private GUIContent includeSubfoldersContent = new GUIContent("Include Subfolders", "Whether to recursively search for textures inside the input folder.");
    private GUIContent prependSubfolderNamesContent = new GUIContent("Prepend Names", "Whether to prepend sprite names with the subfolder name. Eg: SubfolderName.SpriteName");

    private void OnEnable ()
    {
        atlasTexture = serializedObject.FindProperty("atlasTexture");
        dicedSprites = serializedObject.FindProperty("dicedSprites");
        defaultPivot = serializedObject.FindProperty("defaultPivot");
        keepOriginalPivot = serializedObject.FindProperty("keepOriginalPivot");
        decoupleSpriteData = serializedObject.FindProperty("decoupleSpriteData");
        atlasSizeLimit = serializedObject.FindProperty("atlasSizeLimit");
        forceSquare = serializedObject.FindProperty("forceSquare");
        pixelsPerUnit = serializedObject.FindProperty("pixelsPerUnit");
        diceUnitSize = serializedObject.FindProperty("diceUnitSize");
        padding = serializedObject.FindProperty("padding");
        inputFolder = serializedObject.FindProperty("inputFolder");
        includeSubfolders = serializedObject.FindProperty("includeSubfolders");
        prependSubfolderNames = serializedObject.FindProperty("prependSubfolderNames");

        dataSizeValueContent = GetDataSizeValueContent();
    }

    #region GUI
    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(atlasTexture);
        EditorGUILayout.PropertyField(dicedSprites, true);
        EditorGUILayout.LabelField(dataSizeContent, dataSizeValueContent);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(decoupleSpriteData, decoupleSpriteDataContent);
        PivotGUI();
        SizeGUI();
        pixelsPerUnit.floatValue = Mathf.Max(.001f, EditorGUILayout.FloatField(pixelsPerUnitContent, pixelsPerUnit.floatValue));
        EditorGUILayout.PropertyField(diceUnitSize, diceUnitSizeContent);
        padding.intValue = EditorGUILayout.IntSlider(paddingContent, padding.intValue, 2, diceUnitSize.intValue / 2).ToNearestEven();
        InputFolderGUI();
        serializedObject.ApplyModifiedProperties();
    }

    private GUIContent GetBuildButtonContent ()
    {
        var name = (target as DicedSpriteAtlas).IsBuilt ? "Rebuild Atlas" : "Build Atlas";
        var tooltip = inputFolder.objectReferenceValue ? "" : "Select input directory to build atlas.";
        return new GUIContent(name, tooltip);
    }

    private GUIContent GetDataSizeValueContent ()
    {
        long size = 0;

        if (decoupleSpriteData.boolValue)
        {
            for (int i = dicedSprites.arraySize - 1; i >= 0; i--)
            {
                var spriteData = dicedSprites.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!spriteData) continue;
                var spritePath = AssetDatabase.GetAssetPath(spriteData);
                var spriteFullPath = Path.GetFullPath(spritePath);
                size += new FileInfo(spriteFullPath).Length / 1024;
            }
        }

        var atlasPath = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(atlasPath)) return GUIContent.none;
        var atlasFullPath = Path.GetFullPath(atlasPath);
        size += new FileInfo(atlasFullPath).Length / 1024;

        var isBinary = EditorSettings.serializationMode != SerializationMode.ForceText;
        var label = string.Format("{0} KB {1}", size, isBinary ? string.Empty : "(uncompressed)");
        return new GUIContent(label);
    }

    private void PivotGUI ()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, defaultPivotContent);
            rect.width = Mathf.Max(50, (rect.width - 4) / 2);
            using (new EditorGUI.DisabledScope(keepOriginalPivot.boolValue))
                defaultPivot.vector2Value = EditorGUI.Vector2Field(rect, string.Empty, defaultPivot.vector2Value);
            rect.x += rect.width + 5;
            ToggleLeftGUI(rect, keepOriginalPivot, keepOriginalPivotContent);
        }
    }

    private void SizeGUI ()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, atlasSizeLimitContent);
            rect.width = Mathf.Max(50, (rect.width - 4) / 2);
            var popupValues = new int[] { 1024, 2048, 4096, 8192 };
            var popupLabels = popupValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
            EditorGUI.IntPopup(rect, atlasSizeLimit, popupLabels, popupValues, GUIContent.none);
            rect.x += rect.width + 5;
            ToggleLeftGUI(rect, forceSquare, forceSquareContent);
        }
    }

    private void InputFolderGUI ()
    {
        EditorGUILayout.PropertyField(inputFolder, inputFolderContent);
        using (new EditorGUI.DisabledScope(!inputFolder.objectReferenceValue))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, -1, new GUIContent(" "));
                rect.width = Mathf.Max(50, (rect.width - 4) / 2);
                EditorGUIUtility.labelWidth = 50;
                ToggleLeftGUI(rect, includeSubfolders, includeSubfoldersContent);
                rect.x += rect.width + 5;
                using (new EditorGUI.DisabledScope(!includeSubfolders.boolValue))
                    ToggleLeftGUI(rect, prependSubfolderNames, prependSubfolderNamesContent);
                EditorGUIUtility.labelWidth = 0;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button(GetBuildButtonContent(), EditorStyles.miniButton))
                    BuildAtlas();
            }
        }
    }

    private void ToggleLeftGUI (Rect position, SerializedProperty property, GUIContent label)
    {
        var toggleValue = property.boolValue;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        var oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        toggleValue = EditorGUI.ToggleLeft(position, label, toggleValue);
        EditorGUI.indentLevel = oldIndent;
        if (EditorGUI.EndChangeCheck())
            property.boolValue = property.hasMultipleDifferentValues ? true : !property.boolValue;
        EditorGUI.showMixedValue = false;
    }
    #endregion

    #region Atlas generation
    private void BuildAtlas ()
    {
        var inputFolderHelper = new FolderAssetHelper(inputFolder.objectReferenceValue);
        // Create new atlas texture with the max allowed size; it will be auto-trimmed (if necessary) when packing textures.
        var newAtlasTexture = TextureUtils.CreateTexture(atlasSizeLimit.intValue, name: target.name + "Texture");
        newAtlasTexture.alphaIsTransparency = true;
        // Load source texture assets from the input directory.
        DisplayProgressBar("Loading source textures...", .1f);
        var sourceTextureAssets = inputFolderHelper.LoadContainedAssets<Texture2D>(includeSubfolders.boolValue, prependSubfolderNames.boolValue);
        // Dice source textures, evaluate quad rects and colors.
        var dicedUnits = DiceSourceTextures(sourceTextureAssets);
        // Remove all the full-transparent units (no need to render them).
        DisplayProgressBar("Processing diced units...", .7f);
        dicedUnits.RemoveAll(unit => unit.Colors.All(color => color.a == 0));


        // ---------------------------- 
        //// Select diced units with distinct colors (we'll reuse them to render repeating patterns).
        //var distinctUnits = dicedUnits.DistinctBy(unit => unit.Colors, new ArrayEqualityComparer<Color>()).ToList();
        //// Create textures from the distinct units using their padded colors (to prevent texture bleeding).
        //var distinctTextures = distinctUnits.Select(unit => TextureUtils.CreateTexture(diceUnitSize.intValue + padding.intValue * 2, unit.PaddedColors)).ToArray();
        //// Pack distinct unit textures to the atlas and retrieve uv rects.
        //DisplayProgressBar("Packing diced textures...", .8f);
        //var distinctUVRects = newAtlasTexture.PackTextures(distinctTextures, 0, atlasSizeLimit.intValue).ToList();
        //// Map distinct uv rects to the diced units using color hashes as equality keys.
        //MapDicedUnitUVs(dicedUnits, distinctUVRects, distinctUnits.Select(unit => unit.Colors).ToList(), newAtlasTexture);
        // ---------------------------- 


        // ---------------------------- 
        var newAtlasTextures = CreateAtlasTextures(dicedUnits);
        foreach (var newTexture in newAtlasTextures)
        {
            // ... save texture and generate sprites
        }
        // ---------------------------- 


        //// Save generated atlas texture.
        //DisplayProgressBar("Saving generated assets...", .9f);
        //var savedAtlasTexture = newAtlasTexture.SaveAsPng(AssetDatabase.GetAssetPath(target));
        //atlasTexture.objectReferenceValue = savedAtlasTexture;
        //// Generate diced sprites from the diced units.
        //var newDicedSprites = dicedUnits
        //    .GroupBy(unit => unit.Name)
        //    .Select(units => DicedSprite.CreateInstance(units.First().Name, savedAtlasTexture, units.ToList(), defaultPivot.vector2Value, keepOriginalPivot.boolValue))
        //    .ToList();
        //// Save generated sprites.
        //SaveDicedSprites(newDicedSprites);
        //// Update data size content.
        //dataSizeValueContent = GetDataSizeValueContent();
        EditorUtility.ClearProgressBar();
    }

    private List<Texture2D> CreateAtlasTextures (List<DicedUnit> dicedUnits)
    {
        // Group diced units by colors hash and convert to dictionary.
        var hashToUnits = dicedUnits.GroupBy(unit => unit.ColorsHashCode).ToDictionary(unitsGroup => unitsGroup.Key, unitsGroup => unitsGroup.ToList());
        // Evaluate how many units can be packed to a single atlas.
        var unitLimit = Mathf.FloorToInt(Mathf.Pow(atlasSizeLimit.intValue, 2) / Mathf.Pow(diceUnitSize.intValue + padding.intValue * 2f, 2));
        // Evaluate required number of atlas textures to create.
        var atlasCount = Mathf.CeilToInt(hashToUnits.Keys.Count / (float)unitLimit);
        // Spread diced units among the required number of atlas textures and insure any given sprite reference only a single atlas texture.
        var unitsPerAtlas = hashToUnits.Aggregate(new List<List<DicedUnit>> { new List<DicedUnit>() }, (seed, item) => {



            return seed;
        });

        return new List<Texture2D>();
    }

    private List<DicedUnit> DiceSourceTextures (List<FolderAsset<Texture2D>> textureAssets)
    {
        var dicedUnits = new List<DicedUnit>();

        foreach (var textureAsset in textureAssets)
        {
            var sourceTexture = textureAsset.Object;

            // Make sure texture is readable and not crunched (can't get pixels otherwise).
            var textureImporter = textureAsset.Importer as TextureImporter;
            if (!textureImporter.isReadable || textureImporter.crunchedCompression)
            {
                textureImporter.isReadable = true;
                textureImporter.crunchedCompression = false;
                AssetDatabase.ImportAsset(textureAsset.Path);
            }

            var unitCountX = Mathf.CeilToInt((float)sourceTexture.width / diceUnitSize.intValue);
            var unitCountY = Mathf.CeilToInt((float)sourceTexture.height / diceUnitSize.intValue);

            for (int unitX = 0; unitX < unitCountX; unitX++)
            {
                var textureProgress = .6f * textureAssets.ProgressOf(textureAsset);
                var unitProgress = (.6f / textureAssets.Count) * ((float)unitX / unitCountX);
                var textureNumber = textureAssets.IndexOf(textureAsset) + 1;
                var message = string.Format("Dicing texture '{0}' ({1}/{2})...", textureAsset.Name, textureNumber, textureAssets.Count);
                DisplayProgressBar(message, .1f + textureProgress + unitProgress);

                var x = unitX * diceUnitSize.intValue;
                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    var y = unitY * diceUnitSize.intValue;
                    var pixelsRect = new Rect(x, y, diceUnitSize.intValue, diceUnitSize.intValue);
                    var paddedRect = pixelsRect.Crop(padding.intValue);
                    var colors = sourceTexture.GetPixels(pixelsRect);
                    var paddedColors = sourceTexture.GetPixels(paddedRect);
                    var quadVerts = pixelsRect.Scale(1f / pixelsPerUnit.floatValue);
                    var dicedUnit = new DicedUnit() { Name = textureAsset.Name, QuadVerts = quadVerts, Colors = colors, PaddedColors = paddedColors };
                    dicedUnits.Add(dicedUnit);
                }
            }
        }

        return dicedUnits;
    }

    private void MapDicedUnitUVs (List<DicedUnit> dicedUnits, List<Rect> distinctUVRects, List<Color[]> distinctColors, Texture2D atlasTexture)
    {
        foreach (var dicedUnit in dicedUnits)
        {
            var distinctIndex = distinctColors.FindIndex(colors => ArrayEqualityComparer<Color>.Equals(colors, dicedUnit.Colors));
            var paddedUVs = distinctUVRects[distinctIndex];
            // We've used padded rects when building atlas to prevent texture bleeding. 
            // Now we need to correct the uvs so they'll map to the original pixel rects.
            var correctedUVs = paddedUVs
                .Scale(atlasTexture.width, atlasTexture.height)
                .Crop(-padding.intValue)
                .Scale(1f / atlasTexture.width, 1f / atlasTexture.height);
            dicedUnit.QuadUVs = correctedUVs;
        }
    }

    private void SaveDicedSprites (List<DicedSprite> newDicedSprites)
    {
        var folderPath = AssetDatabase.GetAssetPath(target).GetBefore("/", false) + "/" + target.name + "Sprites";
        if (!decoupleSpriteData.boolValue)
        {
            // Delete generated sprites folder (in case it was previously created).
            if (AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.DeleteAsset(folderPath);

            // Update rebuilded sprites to preserve references and delete stale ones.
            var spritesToAdd = new List<DicedSprite>(newDicedSprites);
            for (int i = dicedSprites.arraySize - 1; i >= 0; i--)
            {
                var oldSprite = dicedSprites.GetArrayElementAtIndex(i).objectReferenceValue as DicedSprite;
                if (!oldSprite) continue;
                var newSprite = spritesToAdd.Find(sprite => sprite.Name == oldSprite.Name);
                if (newSprite)
                {
                    EditorUtility.CopySerialized(newSprite, oldSprite);
                    spritesToAdd.Remove(newSprite);
                }
                else DestroyImmediate(oldSprite, true);
            }

            foreach (var spriteToAdd in spritesToAdd)
                AssetDatabase.AddObjectToAsset(spriteToAdd, target);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            dicedSprites.SetListValues(spritesToAdd, false);
        }
        else
        {
            // Delete sprites stored in atlas asset (in case they were previously added).
            for (int i = dicedSprites.arraySize - 1; i >= 0; i--)
                DestroyImmediate(dicedSprites.GetArrayElementAtIndex(i).objectReferenceValue, true);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            var dicedSpritesFolder = new FolderAssetHelper(folderPath);
            var savedDicedSprites = dicedSpritesFolder.SetContainedAssets(newDicedSprites);

            dicedSprites.SetListValues(savedDicedSprites);
        }
    }

    private static void DisplayProgressBar (string activity, float progress)
    {
        EditorUtility.DisplayProgressBar("Building Diced Sprite Atlas", activity, progress);
    }
    #endregion
}
