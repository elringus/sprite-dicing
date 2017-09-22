using UnityEditor;

[CustomEditor(typeof(DicedSpriteRenderer)), CanEditMultipleObjects]
public class DicedSpriteRendererEditor : Editor
{
    protected DicedSpriteRenderer TargetRenderer { get { return target as DicedSpriteRenderer; } }

    private SerializedProperty dicedSprite;
    private SerializedProperty color;
    private SerializedProperty shareMaterial;
    private SerializedProperty customMaterial;

    private void OnEnable ()
    {
        dicedSprite = serializedObject.FindProperty("_dicedSprite");
        color = serializedObject.FindProperty("_color");
        shareMaterial = serializedObject.FindProperty("_shareMaterial");
        customMaterial = serializedObject.FindProperty("customMaterial");
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(dicedSprite);
        EditorGUILayout.PropertyField(color);
        EditorGUILayout.PropertyField(customMaterial);
        EditorGUILayout.PropertyField(shareMaterial);
        serializedObject.ApplyModifiedProperties();
    }
}
