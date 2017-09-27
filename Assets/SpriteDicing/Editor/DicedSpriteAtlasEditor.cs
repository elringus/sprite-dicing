using System;
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


        // ---------------------------- 
        // Remove all the full-transparent units (no need to render them).
        //DisplayProgressBar("Processing diced units...", .7f);
        //dicedUnits.RemoveAll(unit => unit.Colors.All(color => color.a == 0));
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
        // Save created atlas textures.
        foreach (var newTexture in newAtlasTextures)
        {
            var savedAtlasTexture = newTexture.SaveAsPng(AssetDatabase.GetAssetPath(target));
            //atlasTexture.objectReferenceValue = savedAtlasTexture;
        }
        // ---------------------------- 



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

    private List<Texture2D> CreateAtlasTextures (Dictionary<string, List<DicedUnit>> nameToUnitsMap)
    {
        var unitSize = diceUnitSize.intValue;
        var paddingSize = padding.intValue;
        var paddedUnitSize = unitSize + paddingSize * 2;
        var forceSquare = this.forceSquare.boolValue;
        var atlasSizeLimit = this.atlasSizeLimit.intValue;
        // Evaluate how many units can be packed to a single atlas.
        var unitsPerAtlasLimit = Mathf.FloorToInt(Mathf.Pow(atlasSizeLimit, 2) / Mathf.Pow(paddedUnitSize, 2));
        // Group name->units map to name->hash->units enumerable and order by number of distinct units.
        var nameToHashToUnitsEnumerable = nameToUnitsMap.Select(nameToUnits => new KeyValuePair<string, Dictionary<int, List<DicedUnit>>>(nameToUnits.Key, nameToUnits.Value
            .GroupBy(units => units.ColorsHashCode).ToDictionary(x => x.Key, x => x.ToList()))).OrderBy(item => item.Value.Count);
        // Pack units with distinct (inside atlas group) colors to the atlas textures.
        // Insure sprites integrity (units whith equal names should be in a single atlas) and atlas size limit (distinct units per atlas count).
        var atlasTextures = new List<Texture2D>(); // Resulting atlas textures to store distinct diced unit textures.
        atlasTextures.Add(TextureUtils.CreateTexture(atlasSizeLimit, name: target.name + " 000"));
        var hashToUV = new Dictionary<int, Rect>(); // Colors hash to UV rects map of the packed diced units in the current atlas.
        var yToLastXMap = new Dictionary<int, int>();
        var xLimit = Mathf.NextPowerOfTwo(paddedUnitSize);

        var totalUnitsPacked = 0;  // 3136 when texture is out bounds while limit is 3236
        var totalUnitsSkiped = 0; // 1000+ when out of bounds

        foreach (var nameToHashToUnits in nameToHashToUnitsEnumerable)
        {
            // Ensure current atlas texture has enough space to pack the units; create a new one if needed.
            var unitsToPackCount = nameToHashToUnits.Value.Count(hashToUnits => !hashToUV.ContainsKey(hashToUnits.Key));
            if (hashToUV.Keys.Count + unitsToPackCount > unitsPerAtlasLimit)
            {
                var newAtlasTexture = TextureUtils.CreateTexture(atlasSizeLimit, name: string.Format("{0} {1:000}", target.name, atlasTextures.Count));
                atlasTextures.Add(newAtlasTexture);
                hashToUV.Clear();
                yToLastXMap = new Dictionary<int, int>();
                xLimit = Mathf.NextPowerOfTwo(paddedUnitSize);
            }

            // Iterate diced units of a sprite grouped by color hashes.
            foreach (var hashToUnits in nameToHashToUnits.Value)
            {
                if (hashToUV.ContainsKey(hashToUnits.Key))
                {
                    // We've already packed unit with the same colors to this atlas; assign it's UVs to the others in the group.
                    hashToUnits.Value.ForEach(unit => unit.QuadUVs = hashToUV[hashToUnits.Key]);
                    totalUnitsSkiped++;
                    continue;
                }

                int posX, posY; // Position of the new unit on the atlas texture.
                // Find row positions that have enough room for more units until next power of two.
                var suitableYToLastXEnumerable = yToLastXMap.Where(yToLastX => xLimit - yToLastX.Value >= paddedUnitSize * 2);
                if (suitableYToLastXEnumerable.Count() == 0) // When no suitable rows found.
                {
                    // Handle corner case when we just started.
                    if (yToLastXMap.Count == 0)
                    {
                        yToLastXMap.Add(0, 0);
                        posX = 0;
                        posY = 0;
                    }
                    // Determine whether we need to add a new row or increase x limit.
                    else if (xLimit > yToLastXMap.Last().Key)
                    {
                        var newRowYPos = yToLastXMap.Last().Key + paddedUnitSize;
                        yToLastXMap.Add(newRowYPos, 0);
                        posX = 0;
                        posY = newRowYPos;
                    }
                    else
                    {
                        xLimit = Mathf.NextPowerOfTwo(xLimit + 1);
                        posX = yToLastXMap.First().Value + paddedUnitSize;
                        posY = 0;
                        yToLastXMap[0] = posX;
                    }
                }
                else // When suitable rows found.
                {
                    // Find one with the least number of elements and use it.
                    var suitableYToLastX = suitableYToLastXEnumerable.OrderBy(yToLastX => yToLastX.Value).First();
                    posX = suitableYToLastX.Value + paddedUnitSize;
                    posY = suitableYToLastX.Key;
                    yToLastXMap[posY] = posX;
                }

                // Write colors of the unit to the current atlas texture.
                var colorsToPack = hashToUnits.Value.First().PaddedColors;
                var currentAtlas = atlasTextures.Last();

                if (posX + paddedUnitSize > atlasSizeLimit || posY + paddedUnitSize > atlasSizeLimit)
                {
                    Debug.Log("HUI");
                }
                totalUnitsPacked++;

                currentAtlas.SetPixels(posX, posY, paddedUnitSize, paddedUnitSize, colorsToPack);
                // Evaluate and assign UVs of the unit to the other units in the group.
                var unitUVRect = new Rect(posX, posY, paddedUnitSize, paddedUnitSize).Crop(-paddingSize).Scale(1f / atlasSizeLimit);
                hashToUnits.Value.ForEach(unit => unit.QuadUVs = unitUVRect);
                hashToUV.Add(hashToUnits.Key, unitUVRect);
            }
        }

        // Apply packed pixels to the atlas textures.
        atlasTextures.ForEach(atlasTexture => { atlasTexture.Apply(); });
        // Crop atlas textures: http://answers.unity3d.com/questions/998264/crop-texture2d-getset-pixels.html
        // Correct UV rects after crop.

        return atlasTextures;
    }

    private Dictionary<string, List<DicedUnit>> DiceSourceTextures (List<FolderAsset<Texture2D>> textureAssets)
    {
        // Texture name -> units diced off the texture.
        var nameToUnitsMap = new Dictionary<string, List<DicedUnit>>();
        var unitSize = diceUnitSize.intValue;

        foreach (var textureAsset in textureAssets)
        {
            var sourceTexture = textureAsset.Object;
            var key = nameToUnitsMap.ContainsKey(textureAsset.Name) ? textureAsset.Name + Guid.NewGuid().ToString() : textureAsset.Name;
            var value = new List<DicedUnit>();
            var nameToUnits = new KeyValuePair<string, List<DicedUnit>>(key, value);

            // Make sure texture is readable and not crunched (can't get pixels otherwise).
            var textureImporter = textureAsset.Importer as TextureImporter;
            if (!textureImporter.isReadable || textureImporter.crunchedCompression)
            {
                textureImporter.isReadable = true;
                textureImporter.crunchedCompression = false;
                AssetDatabase.ImportAsset(textureAsset.Path);
            }

            var unitCountX = Mathf.CeilToInt((float)sourceTexture.width / unitSize);
            var unitCountY = Mathf.CeilToInt((float)sourceTexture.height / unitSize);

            for (int unitX = 0; unitX < unitCountX; unitX++)
            {
                var textureProgress = .6f * textureAssets.ProgressOf(textureAsset);
                var unitProgress = (.6f / textureAssets.Count) * ((float)unitX / unitCountX);
                var textureNumber = textureAssets.IndexOf(textureAsset) + 1;
                var message = string.Format("Dicing texture '{0}' ({1}/{2})...", textureAsset.Name, textureNumber, textureAssets.Count);
                DisplayProgressBar(message, .1f + textureProgress + unitProgress);

                var x = unitX * unitSize;
                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    var y = unitY * unitSize;
                    var pixelsRect = new Rect(x, y, unitSize, unitSize);
                    var paddedRect = pixelsRect.Crop(padding.intValue);
                    var colors = sourceTexture.GetPixels(pixelsRect);
                    // Skip transparent units (no need to render them).
                    if (colors.All(color => color.a == 0)) continue;
                    var paddedColors = sourceTexture.GetPixels(paddedRect);
                    var quadVerts = pixelsRect.Scale(1f / pixelsPerUnit.floatValue);
                    var dicedUnit = new DicedUnit() { Name = textureAsset.Name, QuadVerts = quadVerts, Colors = colors, PaddedColors = paddedColors };
                    nameToUnits.Value.Add(dicedUnit);
                }
            }

            nameToUnitsMap.Add(nameToUnits.Key, nameToUnits.Value);
        }

        return nameToUnitsMap;
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
