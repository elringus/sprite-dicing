// Copyright 2012-2017 Elringus (Artyom Sovetnikov). All Rights Reserved.

namespace UnityCommon
{
    using UnityEngine;
    using UnityEditor;
    
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    
        public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
    
}
