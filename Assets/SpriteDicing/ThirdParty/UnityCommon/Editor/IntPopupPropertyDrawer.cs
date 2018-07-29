using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityCommon
{
    [CustomPropertyDrawer(typeof(IntPopupAttribute))]
    public class IntPopupPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            var popupAttribute = attribute as IntPopupAttribute;
            var popupValues = popupAttribute.Values;
            if (popupValues == null || popupValues.Length <= 0)
                EditorGUI.PropertyField(position, property, label);
            else
            {
                var popupLabels = popupValues.Select(pair => new GUIContent(pair.ToString())).ToArray();
                EditorGUI.IntPopup(position, property, popupLabels, popupValues, label);
            }
        }
    }
}
