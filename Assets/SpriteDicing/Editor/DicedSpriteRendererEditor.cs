using UnityCommon;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DicedSpriteRenderer)), CanEditMultipleObjects]
public class DicedSpriteRendererEditor : Editor
{
    protected DicedSpriteRenderer TargetRenderer => target as DicedSpriteRenderer;

    private SerializedProperty dicedSprite;
    private SerializedProperty color;
    private SerializedProperty flipX;
    private SerializedProperty flipY;
    private SerializedProperty shareMaterial;
    private SerializedProperty customMaterial;

    private void OnEnable ()
    {
        dicedSprite = serializedObject.FindProperty("dicedSprite");
        color = serializedObject.FindProperty("color");
        flipX = serializedObject.FindProperty("flipX");
        flipY = serializedObject.FindProperty("flipY");
        shareMaterial = serializedObject.FindProperty("shareMaterial");
        customMaterial = serializedObject.FindProperty("customMaterial");
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(dicedSprite);
        EditorGUILayout.PropertyField(color);
        using (new EditorGUILayout.HorizontalScope())
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, new GUIContent("Flip"));
            rect.width = 25;
            EditorGUIUtility.labelWidth = 50;
            EditorUtils.ToggleLeftGUI(rect, flipX, new GUIContent("X"));
            rect.x += rect.width + 5;
            EditorUtils.ToggleLeftGUI(rect, flipY, new GUIContent("Y"));
            EditorGUIUtility.labelWidth = 0;
        }
        EditorGUILayout.PropertyField(customMaterial);
        EditorGUILayout.PropertyField(shareMaterial);
        serializedObject.ApplyModifiedProperties();
    }
}
