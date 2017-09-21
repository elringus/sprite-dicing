// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using UnityEngine;
    using UnityEditor;
    
    [CustomPropertyDrawer(typeof(FolderAssetHelper))]
    public class FolderAssetPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            var folderObject = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(DefaultAsset), false);
            if (folderObject == null || AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folderObject)))
                property.objectReferenceValue = folderObject;
        }
    }
    
}
