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
    protected DicedSpriteAtlas TargetAtlas => target as DicedSpriteAtlas;

    private SerializedProperty atlasTextures;
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
        atlasTextures = serializedObject.FindProperty("atlasTextures");
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
        EditorGUILayout.PropertyField(atlasTextures, true);
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
            EditorUtils.ToggleLeftGUI(rect, keepOriginalPivot, keepOriginalPivotContent);
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
            EditorUtils.ToggleLeftGUI(rect, forceSquare, forceSquareContent);
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
                EditorUtils.ToggleLeftGUI(rect, includeSubfolders, includeSubfoldersContent);
                rect.x += rect.width + 5;
                using (new EditorGUI.DisabledScope(!includeSubfolders.boolValue))
                    EditorUtils.ToggleLeftGUI(rect, prependSubfolderNames, prependSubfolderNamesContent);
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
    #endregion

    #region Atlas generation
    private void BuildAtlas ()
    {
        DisplayProgressBar("Loading source textures...", .0f);
        var inputFolderHelper = new FolderAssetHelper(inputFolder.objectReferenceValue);
        var textureAssets = inputFolderHelper.LoadContainedAssets<Texture2D>(includeSubfolders.boolValue, prependSubfolderNames.boolValue);
        var dicedUnits = DiceSourceTextures(textureAssets);
        if (!CreateAtlasTextures(dicedUnits)) { EditorUtility.ClearProgressBar(); return; }
        CreateDicedSprites(dicedUnits);
        dataSizeValueContent = GetDataSizeValueContent();
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
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
                var textureProgress = .5f * textureAssets.ProgressOf(textureAsset);
                var unitProgress = (.5f / textureAssets.Count) * ((float)unitX / unitCountX);
                var textureNumber = textureAssets.IndexOf(textureAsset) + 1;
                var message = $"Dicing texture '{textureAsset.Name}' ({textureNumber}/{textureAssets.Count})...";
                DisplayProgressBar(message, textureProgress + unitProgress);

                var x = unitX * unitSize;
                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    var y = unitY * unitSize;
                    var pixelsRect = new Rect(x, y, unitSize, unitSize);
                    var paddedRect = pixelsRect.Crop(padding.intValue);
                    var colors = sourceTexture.GetPixels(pixelsRect); // TODO: Get only padded pixels and evaluate original pixels from them.
                    // Skip transparent units (no need to render them).
                    if (colors.All(color => color.a == 0)) continue;
                    var paddedColors = sourceTexture.GetPixels(paddedRect);
                    var quadVerts = pixelsRect.Scale(1f / pixelsPerUnit.floatValue);
                    var dicedUnit = new DicedUnit() { QuadVerts = quadVerts, Colors = colors, PaddedColors = paddedColors };
                    nameToUnits.Value.Add(dicedUnit);
                }
            }

            nameToUnitsMap.Add(nameToUnits.Key, nameToUnits.Value);
        }

        return nameToUnitsMap;
    }

    // TODO: Use custom structs for all the maps, move out atlas generation to a separate .cs (?).
    private bool CreateAtlasTextures (Dictionary<string, List<DicedUnit>> dicedUnits)
    {
        DisplayProgressBar("Processing diced textures...", .5f);

        // Delete any previously generated atlas textures.
        for (int i = atlasTextures.arraySize - 1; i >= 0; i--)
        {
            var unusedTexture = atlasTextures.GetArrayElementAtIndex(i).objectReferenceValue;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(unusedTexture));
            DestroyImmediate(unusedTexture, true);
        }
        atlasTextures.arraySize = 0;

        var atlasCount = 0;
        var unitSize = diceUnitSize.intValue;
        var paddingSize = padding.intValue;
        var paddedUnitSize = unitSize + paddingSize * 2;
        var forceSquare = this.forceSquare.boolValue;
        var atlasSizeLimit = this.atlasSizeLimit.intValue;
        var unitsPerAtlasLimit = Mathf.Pow(atlasSizeLimit / paddedUnitSize, 2);

        // Group name->units to name->hash->units map.
        var unitsToPackMap = dicedUnits.Select(nameToUnits => new KeyValuePair<string, Dictionary<int, List<DicedUnit>>>(nameToUnits.Key, nameToUnits.Value
            .GroupBy(units => units.ColorsHashCode).ToDictionary(hashToUnitsGroup => hashToUnitsGroup.Key, hashToUnitsGroup => hashToUnitsGroup.ToList())))
            .ToDictionary(nameToHashToUnits => nameToHashToUnits.Key, nameToHashToUnits => nameToHashToUnits.Value);

        // Pack units with distinct (inside atlas group) colors to the atlas textures.
        // Insure sprites integrity (units belonging to one sprite should be in a common atlas) and atlas size limit (distinct units per atlas count).
        while (unitsToPackMap.Count > 0)
        {
            atlasCount++;

            var atlasTexture = TextureUtils.CreateTexture(atlasSizeLimit, name: $"{target.name} {atlasCount:000}");
            var hashToUV = new Dictionary<int, Rect>(); // Colors hash to UV rects map of the packed diced units in the current atlas.
            var yToLastXMap = new Dictionary<int, int>(); // Y position of a units row in the current atlas to the x position of the last unit in this row.
            var xLimit = Mathf.NextPowerOfTwo(paddedUnitSize); // Maximum allowed width of the current atlas. Increases by the power of two in the process.
            var packedUnits = new List<DicedUnit>(); // List of the units packed to the current atlas.

            // Find units that can be packed to the current atlas (respecting atlas size limit and remaining free space).
            Func<KeyValuePair<string, Dictionary<int, List<DicedUnit>>>> findSuitableUnitsToPack = () => {
                return unitsToPackMap.FirstOrDefault(nameToHashToUnits => {
                    var unitsToPackCount = nameToHashToUnits.Value.Count(hashToUnits => !hashToUV.ContainsKey(hashToUnits.Key));
                    return hashToUV.Keys.Count + unitsToPackCount <= unitsPerAtlasLimit;
                });
            };

            var suitableUnits = findSuitableUnitsToPack();
            if (suitableUnits.Key == null) // None of the source textures fit atlas limit. 
            {
                Debug.LogError("SpriteDicing: Unable to fit input textures to the atlas. Consider increasing atlas size limit.");
                return false; 
            }

            while (suitableUnits.Key != null)
            {
                var packingProgress = 1f - (unitsToPackMap.Count / (float)dicedUnits.Count);
                DisplayProgressBar("Packing diced textures...", .5f + .5f * packingProgress);

                // Iterate suitable for packing units grouped by their color hashes.
                foreach (var hashToUnits in suitableUnits.Value)
                {
                    if (hashToUV.ContainsKey(hashToUnits.Key))
                    {
                        // We've already packed unit with the same colors to this atlas; assign it's UVs to the others in the group.
                        hashToUnits.Value.ForEach(unit => unit.QuadUVs = hashToUV[hashToUnits.Key]);
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
                    atlasTexture.SetPixels(posX, posY, paddedUnitSize, paddedUnitSize, colorsToPack);
                    // Evaluate and assign UVs of the unit to the other units in the group.
                    var unitUVRect = new Rect(posX, posY, paddedUnitSize, paddedUnitSize).Crop(-paddingSize).Scale(1f / atlasSizeLimit);
                    hashToUnits.Value.ForEach(unit => unit.QuadUVs = unitUVRect);
                    hashToUV.Add(hashToUnits.Key, unitUVRect);
                }

                packedUnits.AddRange(suitableUnits.Value.SelectMany(u => u.Value));
                unitsToPackMap.Remove(suitableUnits.Key);
                suitableUnits = findSuitableUnitsToPack();
            }

            // Crop unused atlas texture space.
            var needToCrop = xLimit < atlasSizeLimit || (!forceSquare && yToLastXMap.Last().Key + paddedUnitSize < atlasSizeLimit);
            if (needToCrop)
            {
                var croppedWidth = xLimit;
                var croppedHeight = forceSquare ? croppedWidth : yToLastXMap.Last().Key + paddedUnitSize;
                var croppedPixels = atlasTexture.GetPixels(0, 0, croppedWidth, croppedHeight);
                atlasTexture = TextureUtils.CreateTexture(croppedWidth, croppedHeight, name: atlasTexture.name);
                atlasTexture.SetPixels(croppedPixels);

                // Correct UV rects after crop.
                packedUnits.ForEach(unit => unit.QuadUVs = unit.QuadUVs
                    .Scale(new Vector2(atlasSizeLimit / (float)croppedWidth, atlasSizeLimit / (float)croppedHeight)));
            }

            // Save atlas texture.
            atlasTexture.alphaIsTransparency = true;
            atlasTexture.Apply();
            var savedTexture = atlasTexture.SaveAsPng(AssetDatabase.GetAssetPath(target));
            atlasTextures.arraySize = Mathf.Max(atlasTextures.arraySize, atlasCount);
            atlasTextures.GetArrayElementAtIndex(atlasCount - 1).objectReferenceValue = savedTexture;
            packedUnits.ForEach(unit => unit.AtlasTexture = savedTexture);
        }

        return true;
    }

    private void CreateDicedSprites (Dictionary<string, List<DicedUnit>> dicedUnits)
    {
        DisplayProgressBar("Generating diced sprites data...", 1f);

        // Generate diced sprites using diced units.
        var newDicedSprites = dicedUnits.Select(nameToUnits => DicedSprite.CreateInstance(nameToUnits.Key, nameToUnits.Value.First().AtlasTexture,
            nameToUnits.Value, defaultPivot.vector2Value, keepOriginalPivot.boolValue)).ToList();

        // Save generated sprites.
        var folderPath = AssetDatabase.GetAssetPath(target).GetBeforeLast("/") + "/" + target.name;
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
