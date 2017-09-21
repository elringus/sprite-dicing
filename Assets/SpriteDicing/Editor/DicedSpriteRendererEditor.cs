using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DicedSpriteRenderer))]
public class DicedSpriteRendererEditor : Editor
{
    protected DicedSpriteRenderer TargetRenderer { get { return target as DicedSpriteRenderer; } }

    private SerializedProperty dicedSprite;
    private SerializedProperty color;
    private SerializedProperty shareMaterial;

    private GUIContent materialContent = new GUIContent("Material", "Material used by the renderer.");

    private void OnEnable ()
    {
        dicedSprite = serializedObject.FindProperty("_dicedSprite");
        color = serializedObject.FindProperty("_color");
        shareMaterial = serializedObject.FindProperty("_shareMaterial");
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(dicedSprite);
        EditorGUILayout.PropertyField(color);
        TargetRenderer.Material = EditorGUILayout.ObjectField(materialContent, TargetRenderer.Material, typeof(Material), false) as Material;
        EditorGUILayout.PropertyField(shareMaterial);
        serializedObject.ApplyModifiedProperties();
    }
}
