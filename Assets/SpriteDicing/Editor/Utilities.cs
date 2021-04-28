using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpriteDicing
{
    public static class Utilities
    {
        public static void ToggleLeftGUI (Rect position, SerializedProperty property, GUIContent label)
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

        public static T CreateOrReplaceAsset<T> (this UnityEngine.Object asset, string path) where T : UnityEngine.Object
        {
            var existingAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset as T;
            }
            else
            {
                EditorUtility.CopySerialized(asset, existingAsset);
                return existingAsset;
            }
        }

        public static void SetListValues<T> (this SerializedProperty serializedProperty, List<T> listValues, bool clearSourceList = true) where T : UnityEngine.Object
        {
            Debug.Assert(serializedProperty != null && serializedProperty.isArray);

            var targetObject = serializedProperty.serializedObject.targetObject;
            var objectType = targetObject.GetType();
            var fieldInfo = objectType.GetField(serializedProperty.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo is null) throw new Exception();
            var list = (List<T>)fieldInfo.GetValue(targetObject);
            if (clearSourceList) list.Clear();
            list.AddRange(listValues);
            list.RemoveAll(item => !item || item == null);

            serializedProperty.serializedObject.CopyFromSerializedProperty(new SerializedObject(targetObject).FindProperty(serializedProperty.name));
        }

        public static Texture2D SaveAsPng (this Texture2D texture, string path, TextureImporterType textureType = TextureImporterType.Default,
            TextureImporterCompression compression = TextureImporterCompression.Uncompressed, bool generateMipmaps = false, bool destroyInitialTextureObject = true)
        {
            var wrapMode = texture.wrapMode;
            var alphaIsTransparency = texture.alphaIsTransparency;
            var maxSize = Mathf.Max(texture.width, texture.height);

            path = $"{path.GetBeforeLast("/")}/{texture.name}.png";
            Debug.Assert(AssetDatabase.IsValidFolder(path.GetBefore("/")));
            var bytes = texture.EncodeToPNG();
            using (var fileStream = File.Create(path))
                fileStream.Write(bytes, 0, bytes.Length);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter is null) throw new Exception();
            textureImporter.textureType = textureType;
            textureImporter.alphaIsTransparency = alphaIsTransparency;
            textureImporter.wrapMode = wrapMode;
            textureImporter.mipmapEnabled = generateMipmaps;
            textureImporter.textureCompression = compression;
            textureImporter.maxTextureSize = maxSize;
            AssetDatabase.ImportAsset(path);

            if (destroyInitialTextureObject)
                UnityEngine.Object.DestroyImmediate(texture);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Attempts to extract content before the specified match (on first occurence).
        /// </summary>
        public static string GetBefore (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var endIndex = content.IndexOf(matchString, comp);
                return content.Substring(0, endIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content before the specified match (on last occurence).
        /// </summary>
        public static string GetBeforeLast (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var endIndex = content.LastIndexOf(matchString, comp);
                return content.Substring(0, endIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content after the specified match (on last occurence).
        /// </summary>
        public static string GetAfter (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var startIndex = content.LastIndexOf(matchString, comp) + matchString.Length;
                if (content.Length <= startIndex) return string.Empty;
                return content.Substring(startIndex);
            }
            else return null;
        }

        public static float ProgressOf<T> (this List<T> list, T currentItem)
        {
            return list.IndexOf(currentItem) / (float)list.Count;
        }

        public static bool Contains (this Rect rect, float x, float y)
        {
            return rect.Contains(new Vector2(x, y));
        }

        public static Rect Scale (this Rect rect, float scale)
        {
            return new Rect(rect.position * scale, rect.size * scale);
        }

        public static Rect Scale (this Rect rect, Vector2 scale)
        {
            return new Rect(new Vector2(rect.position.x * scale.x, rect.position.y * scale.y),
                new Vector2(rect.size.x * scale.x, rect.size.y * scale.y));
        }

        public static Rect Crop (this Rect rect, float cropAmount)
        {
            return new Rect(rect.position - Vector2.one * cropAmount, rect.size + Vector2.one * (cropAmount * 2f));
        }

        /// <summary>
        /// Creates a texture.
        /// </summary>
        public static Texture2D CreateTexture (int width, int height, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            var texture = new Texture2D(width, height, textureFormat, mipmap, linear);
            texture.wrapMode = wrapMode;
            if (!string.IsNullOrEmpty(name))
                texture.name = name;
            return texture;
        }

        /// <summary>
        /// Creates a square texture.
        /// </summary>
        public static Texture2D CreateTexture (int size, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            return CreateTexture(size, size, wrapMode, textureFormat, mipmap, linear, name);
        }

        /// <summary>
        /// Creates a texture and fills it with provided colors.
        /// </summary>
        public static Texture2D CreateTexture (int width, int height, Color[] pixelColors, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            var texture = CreateTexture(width, height, wrapMode, textureFormat, mipmap, linear, name);
            texture.SetPixels(pixelColors);
            texture.Apply();
            return texture;
        }

        public static int ToNearestEven (this int value, int upperLimit = int.MaxValue)
        {
            return (value % 2 == 0) ? value : Mathf.Min(value + 1, upperLimit);
        }

        /// <summary>
        /// Creates a square texture and fills it with provided colors.
        /// </summary>
        public static Texture2D CreateTexture (int size, Color[] pixelColors, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
            TextureFormat textureFormat = TextureFormat.RGBA32, bool mipmap = false, bool linear = false, string name = "")
        {
            return CreateTexture(size, size, pixelColors, wrapMode, textureFormat, mipmap, linear, name);
        }

        /// <summary>
        /// Read pixels from the texture, filling overbound regions with the provided color.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="pixelsRect">Rect on the texture to read pixels from.</param>
        /// <param name="overboundColor">If rect is outside of texture bounds, overbound regions will be filled with this color.</param>
        /// <returns>Flattened 2D array, where pixels are laid out left to right, bottom to top.</returns>
        public static Color[] GetPixels (this Texture2D texture, Rect pixelsRect, Color overboundColor = default)
        {
            var startX = Mathf.FloorToInt(pixelsRect.x);
            var startY = Mathf.FloorToInt(pixelsRect.y);
            var rectWidth = Mathf.FloorToInt(pixelsRect.width);
            var rectHeight = Mathf.FloorToInt(pixelsRect.height);
            var endX = startX + rectWidth;
            var endY = startY + rectHeight;
            var colors = new Color[rectWidth * rectHeight];
            var colorsIndex = 0;
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (x > texture.width || y > texture.height || !pixelsRect.Contains(x, y))
                        colors[colorsIndex] = overboundColor;
                    else colors[colorsIndex] = texture.GetPixel(x, y);
                    colorsIndex++;
                }
            }
            return colors;
        }
    }
}
