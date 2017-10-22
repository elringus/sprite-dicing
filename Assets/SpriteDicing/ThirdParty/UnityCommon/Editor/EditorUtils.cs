// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    
    public static class EditorUtils 
    {
        public static T CreateOrReplaceAsset<T> (this Object asset, string path) where T : Object
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
    
        public static void SetListValues<T> (this SerializedProperty serializedProperty, List<T> listValues, bool clearSourceList = true) where T : Object
        {
            Debug.Assert(serializedProperty != null && serializedProperty.isArray);
    
            var targetObject = serializedProperty.serializedObject.targetObject;
            var objectType = targetObject.GetType();
            var fieldInfo = objectType.GetField(serializedProperty.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
    
            path = string.Format("{0}/{1}.png", path.GetBeforeLast("/"), texture.name);
            Debug.Assert(AssetDatabase.IsValidFolder(path.GetBefore("/")));
            var bytes = texture.EncodeToPNG();
            using (var fileStream = System.IO.File.Create(path)) 
                fileStream.Write(bytes, 0, bytes.Length);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
    
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            textureImporter.textureType = textureType;
            textureImporter.alphaIsTransparency = alphaIsTransparency;
            textureImporter.wrapMode = wrapMode;
            textureImporter.mipmapEnabled = generateMipmaps;
            textureImporter.textureCompression = compression;
            textureImporter.maxTextureSize = maxSize;
            AssetDatabase.ImportAsset(path);
    
            if (destroyInitialTextureObject)
                Object.DestroyImmediate(texture);
    
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    
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
                property.boolValue = property.hasMultipleDifferentValues ? true : !property.boolValue;
            EditorGUI.showMixedValue = false;
        }
    }
    
}
