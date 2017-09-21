using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DicedSpriteRenderer))]
public class DicedSpriteRendererEditor : Editor
{
    protected DicedSpriteRenderer TargetRenderer { get { return target as DicedSpriteRenderer; } }

    private SerializedProperty dicedSprite;
    private SerializedProperty color;

    private GUIContent materialContent = new GUIContent("Material", "Material used by the renderer.");

    private void OnEnable ()
    {
        dicedSprite = serializedObject.FindProperty("_dicedSprite");
        color = serializedObject.FindProperty("_color");
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(dicedSprite);
        EditorGUILayout.PropertyField(color);
        TargetRenderer.Material = EditorGUILayout.ObjectField(materialContent, TargetRenderer.Material, typeof(Material), false) as Material;
        serializedObject.ApplyModifiedProperties();
    }
}
