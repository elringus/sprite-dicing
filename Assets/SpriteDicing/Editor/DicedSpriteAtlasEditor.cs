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
        private static readonly GUIContent dataSizeContent = new GUIContent("Generated Data Size", "Total amount of the generated sprites data (vertices, UVs and triangles). Reduce by increasing Dice Unit Size.");
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

        private DicedSpriteAtlas targetAtlas => target as DicedSpriteAtlas;
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
        private GUIContent dataSizeValueContent;

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

            dataSizeValueContent = GetDataSizeValueContent();
        }

        #region GUI
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(texturesProperty, true);
            EditorGUILayout.PropertyField(spritesProperty, true);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.LabelField(dataSizeContent, dataSizeValueContent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(decoupleSpriteDataProperty, decoupleSpriteDataContent);
            DrawPivotGUI();
            DrawSizeGUI();
            pixelsPerUnitProperty.floatValue = Mathf.Max(.001f, EditorGUILayout.FloatField(pixelsPerUnitContent, pixelsPerUnitProperty.floatValue));
            EditorGUILayout.IntPopup(diceUnitSizeProperty, diceUnitSizeLabels, diceUnitSizeValues, diceUnitSizeContent);
            DrawPaddingSlider();
            EditorGUILayout.PropertyField(uvInsetProperty, uvInsetContent);
            DrawInputFolderGUI();
            serializedObject.ApplyModifiedProperties();
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
            var name = targetAtlas.SpritesCount > 0 ? "Rebuild Atlas" : "Build Atlas";
            var tooltip = inputFolderProperty.objectReferenceValue ? "" : "Select input directory to build atlas.";
            return new GUIContent(name, tooltip);
        }

        private GUIContent GetDataSizeValueContent ()
        {
            long size = 0;

            if (decoupleSpriteDataProperty.boolValue)
            {
                for (int i = spritesProperty.arraySize - 1; i >= 0; i--)
                {
                    var spriteData = spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
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
            var label = $"{size} KB {(isBinary ? string.Empty : "(uncompressed)")}";
            return new GUIContent(label);
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

            using (new EditorGUI.DisabledScope(!inputFolderProperty.objectReferenceValue))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var rect = EditorGUILayout.GetControlRect();
                    rect = EditorGUI.PrefixLabel(rect, -1, new GUIContent(" "));
                    rect.width = Mathf.Max(50, (rect.width - 4) / 2);
                    EditorGUIUtility.labelWidth = 50;
                    Utilities.ToggleLeftGUI(rect, includeSubfoldersProperty, includeSubfoldersContent);
                    rect.x += rect.width + 5;
                    using (new EditorGUI.DisabledScope(!includeSubfoldersProperty.boolValue))
                        Utilities.ToggleLeftGUI(rect, prependSubfolderNamesProperty, prependSubfolderNamesContent);
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
            try
            {
                DeleteAtlasTextures();
                var sourceTextures = CollectSourceTextures();
                var dicedTextures = DiceTextures(sourceTextures);
                CreateAtlasTextures(dicedTextures, unitSize, padding, uvInset, forceSquare, atlasSizeLimit, texturesProperty, AssetDatabase.GetAssetPath(target));
                DisplayProgressBar("Generating sprite assets...", 1f);
                var sprites = dicedTextures.Select(t => CreateSprite(t.Source.Name, t.Units.First().AtlasTexture, t, ppu, keepOriginalPivot, defaultPivot)).ToList();
                SaveGeneratedSprites(sprites, decoupleSpriteData, generatedSpritesFolderGuidProperty, spritesProperty, target);

                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                dataSizeValueContent = GetDataSizeValueContent();
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void DeleteAtlasTextures ()
        {
            for (int i = texturesProperty.arraySize - 1; i >= 0; i--)
            {
                var texture = texturesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                var texturePath = AssetDatabase.GetAssetPath(texture);
                AssetDatabase.DeleteAsset(texturePath);
                DestroyImmediate(texture, true);
            }
            texturesProperty.arraySize = 0;
        }

        private IReadOnlyList<SourceTexture> CollectSourceTextures ()
        {
            DisplayProgressBar("Collecting source textures...", .0f);
            var inputFolderPath = AssetDatabase.GetAssetPath(inputFolder);
            var texturePaths = TextureFinder.FindAt(inputFolderPath, includeSubfolders);
            var loader = new TextureLoader(prependSubfolderNames ? inputFolderPath : null);
            return texturePaths.Select(loader.Load).ToArray();
        }

        private IReadOnlyList<DicedTexture> DiceTextures (IReadOnlyList<SourceTexture> sourceTextures)
        {
            var dicer = new TextureDicer(unitSize, padding, ppu);
            var dicedTextures = new List<DicedTexture>();
            for (int i = 0; i < sourceTextures.Count; i++)
            {
                var sourceTexture = sourceTextures[i];
                DisplayDicingProgress(sourceTexture.Name, i, sourceTextures.Count);
                dicedTextures.Add(dicer.Dice(sourceTexture));
            }
            return dicedTextures;
        }

        private static void DisplayDicingProgress (string name, int index, int total)
        {
            var progress = .5f * index / total;
            var message = $"Dicing '{name}' ({index + 1}/{total})...";
            DisplayProgressBar(message, progress);
        }

        private static void CreateAtlasTextures (IReadOnlyList<DicedTexture> dicedTextures, int unitSize, int padding, float uvInset,
            bool forceSquare, int atlasSizeLimit, SerializedProperty texturesProperty, string atlasAssetPath)
        {
            DisplayProgressBar("Processing diced textures...", .5f);

            var atlasName = Path.GetFileNameWithoutExtension(atlasAssetPath);
            var atlasCount = 0;
            var paddedUnitSize = unitSize + padding * 2;
            var unitsPerAtlasLimit = Mathf.FloorToInt(Mathf.Pow(Mathf.FloorToInt(atlasSizeLimit / (float)paddedUnitSize), 2));

            // Group name->units to name->hash->units map.
            var unitsToPackMap = dicedTextures.Select(nameToUnits => new KeyValuePair<string, Dictionary<Hash128, List<DicedUnit>>>(nameToUnits.Source.Name, nameToUnits.Units
                    .GroupBy(units => units.ContentHash).ToDictionary(hashToUnitsGroup => hashToUnitsGroup.Key, hashToUnitsGroup => hashToUnitsGroup.ToList())))
                .ToDictionary(nameToHashToUnits => nameToHashToUnits.Key, nameToHashToUnits => nameToHashToUnits.Value);

            // Pack units with distinct (inside atlas group) colors to the atlas textures.
            // Ensure sprites integrity (units belonging to one sprite should be in a common atlas) and atlas size limit (distinct units per atlas count).
            while (unitsToPackMap.Count > 0)
            {
                atlasCount++;

                var atlasTexture = Utilities.CreateTexture(atlasSizeLimit, atlasSizeLimit, name: $"{atlasName} {atlasCount:000}");
                var hashToUV = new Dictionary<Hash128, Rect>(); // Colors hash to UV rects map of the packed diced units in the current atlas.
                var yToLastXMap = new Dictionary<int, int>(); // Y position of a units row in the current atlas to the x position of the last unit in this row.
                var xLimit = Mathf.NextPowerOfTwo(paddedUnitSize); // Maximum allowed width of the current atlas. Increases by the power of two in the process.
                var packedUnits = new List<DicedUnit>(); // List of the units packed to the current atlas.

                // Find units that can be packed to the current atlas (respecting atlas size limit and remaining free space).
                KeyValuePair<string, Dictionary<Hash128, List<DicedUnit>>> findSuitableUnitsToPack ()
                {
                    return unitsToPackMap.FirstOrDefault(nameToHashToUnits => {
                        var unitsToPackCount = nameToHashToUnits.Value.Count(hashToUnits => !hashToUV.ContainsKey(hashToUnits.Key));
                        return hashToUV.Keys.Count + unitsToPackCount <= unitsPerAtlasLimit;
                    });
                }

                var suitableUnits = findSuitableUnitsToPack();
                if (suitableUnits.Key == null) // None of the source textures fit atlas limit.
                    throw new Exception("SpriteDicing: Unable to fit input textures to the atlas. Consider increasing atlas size limit.");

                while (suitableUnits.Key != null)
                {
                    var packingProgress = 1f - (unitsToPackMap.Count / (float)dicedTextures.Count);
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
                        var suitableYToLastXEnumerable = yToLastXMap.Where(yToLastX => xLimit - yToLastX.Value >= paddedUnitSize * 2).ToArray();
                        if (suitableYToLastXEnumerable.Length == 0) // When no suitable rows found.
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
                        var colorsToPack = hashToUnits.Value.First().PaddedPixels;
                        atlasTexture.SetPixels(posX, posY, paddedUnitSize, paddedUnitSize, colorsToPack);
                        // Evaluate and assign UVs of the unit to the other units in the group.
                        var unitUVRect = new Rect(posX, posY, paddedUnitSize, paddedUnitSize).Crop(-padding).Scale(1f / atlasSizeLimit);
                        if (uvInset > 0) unitUVRect = unitUVRect.Crop(-uvInset * (unitUVRect.width / 2f));
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
                    atlasTexture = Utilities.CreateTexture(croppedWidth, croppedHeight, name: atlasTexture.name);
                    atlasTexture.SetPixels(croppedPixels);

                    // Correct UV rects after crop.
                    packedUnits.ForEach(unit => unit.QuadUVs = unit.QuadUVs
                        .Scale(new Vector2(atlasSizeLimit / (float)croppedWidth, atlasSizeLimit / (float)croppedHeight)));
                }

                // Save atlas texture.
                atlasTexture.alphaIsTransparency = true;
                atlasTexture.Apply();
                var savedTexture = atlasTexture.SaveAsPng(atlasAssetPath);
                texturesProperty.arraySize = Mathf.Max(texturesProperty.arraySize, atlasCount);
                texturesProperty.GetArrayElementAtIndex(atlasCount - 1).objectReferenceValue = savedTexture;
                packedUnits.ForEach(unit => unit.AtlasTexture = savedTexture);
            }
        }

        private static Sprite CreateSprite (string name, Texture2D atlasTexture, DicedTexture dicedTexture, float ppu, bool keepOriginalPivot, Vector2 defaultPivot)
        {
            var vertices = new List<Vector2>();
            var uvs = new List<Vector2>();
            var triangles = new List<ushort>();

            foreach (var dicedUnit in dicedTexture.Units)
                AddDicedUnit(dicedUnit);

            var originalPivot = TrimVertices();
            var pivot = keepOriginalPivot ? originalPivot : defaultPivot;
            ApplyPivotChange(pivot);
            var renderRect = EvaluateSpriteRect().Scale(ppu);

            // Public sprite ctor won't allow using a rect that is larger than the texture:
            // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/2D/Common/ScriptBindings/Sprites.bindings.cs#L271
            var sprite = typeof(Sprite).GetMethod("CreateSprite", BindingFlags.NonPublic | BindingFlags.Static)
                // (texture, rect, pivot, pixelsPerUnit, extrude, meshType, border, generateFallbackPhysicsShape)
                ?.Invoke(null, new object[] { atlasTexture, renderRect, pivot, ppu, (uint)0, SpriteMeshType.Tight, Vector4.zero, false }) as Sprite;
            if (sprite is null) throw new Exception($"Failed to create `{name}` sprite.");
            sprite.name = name;
            sprite.SetVertexCount(vertices.Count);
            sprite.SetIndices(new NativeArray<ushort>(triangles.ToArray(), Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.Position, new NativeArray<Vector3>(vertices.Select(v => new Vector3(v.x, v.y)).ToArray(), Allocator.Temp));
            sprite.SetVertexAttribute(VertexAttribute.TexCoord0, new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp));

            return sprite;

            #region Local functions
            void AddDicedUnit (DicedUnit dicedUnit) => AddQuad(dicedUnit.QuadVerts.min, dicedUnit.QuadVerts.max, dicedUnit.QuadUVs.min, dicedUnit.QuadUVs.max);

            void AddQuad (Vector2 posMin, Vector2 posMax, Vector2 uvMin, Vector2 uvMax)
            {
                var startIndex = vertices.Count;

                AddVertice(new Vector2(posMin.x, posMin.y), new Vector2(uvMin.x, uvMin.y));
                AddVertice(new Vector2(posMin.x, posMax.y), new Vector2(uvMin.x, uvMax.y));
                AddVertice(new Vector2(posMax.x, posMax.y), new Vector2(uvMax.x, uvMax.y));
                AddVertice(new Vector2(posMax.x, posMin.y), new Vector2(uvMax.x, uvMin.y));

                AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                AddTriangle(startIndex + 2, startIndex + 3, startIndex);
            }

            void AddVertice (Vector2 position, Vector2 uv)
            {
                vertices.Add(position);
                uvs.Add(uv);
            }

            void AddTriangle (int idx0, int idx1, int idx2)
            {
                triangles.Add((ushort)idx0);
                triangles.Add((ushort)idx1);
                triangles.Add((ushort)idx2);
            }

            // Reposition the vertices so that they start at the local origin (0, 0).
            Vector2 TrimVertices ()
            {
                var rect = EvaluateSpriteRect();
                if (rect.min.x > 0 || rect.min.y > 0)
                    for (int i = 0; i < vertices.Count; i++)
                        vertices[i] -= rect.min;
                if (!dicedTexture.Source.Sprite) return -rect.min / rect.size;
                return (dicedTexture.Source.Sprite.pivot / ppu - rect.min) / rect.size;
            }

            // Evaluate sprite rectangle using vertex data.
            Rect EvaluateSpriteRect ()
            {
                var minVertPos = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
                var maxVertPos = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
                var spriteSizeX = Mathf.Abs(maxVertPos.x - minVertPos.x);
                var spriteSizeY = Mathf.Abs(maxVertPos.y - minVertPos.y);
                var spriteSize = new Vector2(spriteSizeX, spriteSizeY);
                return new Rect(minVertPos, spriteSize);
            }

            // Corrects geometry data to to match current pivot value.
            void ApplyPivotChange (Vector2 newPivot)
            {
                var spriteRect = EvaluateSpriteRect();
                var curPivot = new Vector2(-spriteRect.min.x / spriteRect.size.x, -spriteRect.min.y / spriteRect.size.y);
                var curDeltaX = spriteRect.size.x * curPivot.x;
                var curDeltaY = spriteRect.size.y * curPivot.y;
                var newDeltaX = spriteRect.size.x * newPivot.x;
                var newDeltaY = spriteRect.size.y * newPivot.y;
                var deltaPos = new Vector2(newDeltaX - curDeltaX, newDeltaY - curDeltaY);

                for (int i = 0; i < vertices.Count; i++)
                    vertices[i] -= deltaPos;
            }
            #endregion
        }

        private static void SaveGeneratedSprites (List<Sprite> sprites, bool decoupleSpriteData,
            SerializedProperty generatedSpritesFolderGuidProperty, SerializedProperty spritesProperty, UnityEngine.Object target)
        {
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

                foreach (var spriteToAdd in spritesToAdd)
                    AssetDatabase.AddObjectToAsset(spriteToAdd, target);

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                spritesProperty.SetListValues(spritesToAdd, false);
            }
            else
            {
                // Delete sprites stored in atlas asset (in case they were previously added).
                for (int i = spritesProperty.arraySize - 1; i >= 0; i--)
                    DestroyImmediate(spritesProperty.GetArrayElementAtIndex(i).objectReferenceValue, true);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                var folderPath = AssetDatabase.GetAssetPath(target).GetBeforeLast("/") + "/" + target.name;
                var dicedSpritesFolder = new FolderAssetHelper(folderPath);
                var savedDicedSprites = dicedSpritesFolder.SetContainedAssets(sprites);

                generatedSpritesFolderGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(folderPath);
                spritesProperty.SetListValues(savedDicedSprites);
            }
        }

        private static void DisplayProgressBar (string activity, float progress)
        {
            EditorUtility.DisplayProgressBar("Building Diced Sprite Atlas", activity, progress);
        }
        #endregion
    }
}
