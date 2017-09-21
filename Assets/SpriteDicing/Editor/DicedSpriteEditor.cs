using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DicedSprite))]
public class DicedSpriteEditor : Editor
{
    protected DicedSprite TargetSprite { get { return target as DicedSprite; } }

    private SerializedProperty atlasTexture;

    private GUIContent subAssetNameContent = new GUIContent("Name", "Name of the diced sprite object. Used to find it among others in the parent atlas.");
    private GUIContent mainAssetNameContent = new GUIContent("Name", "Name of the diced sprite object. Used to find it among others in the parent atlas. Raname asset object in the editor to change.");
    private GUIContent pivotContent = new GUIContent("Pivot", "Relative pivot point position in 0 to 1 range, counting from the bottom-left corner.");
    private GUIContent atlasTextureContent = new GUIContent("Atlas Texture", "Reference to the atlas texture where the dices of the original sprite texture are stored.");

    private Vector2 pivotValue;

    private void OnEnable ()
    {
        atlasTexture = serializedObject.FindProperty("atlasTexture");

        pivotValue = TargetSprite.Pivot;
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        SpriteNameGUI();
        PivotGUI();
        EditorGUILayout.PropertyField(atlasTexture, atlasTextureContent);
        serializedObject.ApplyModifiedProperties();
    }

    private void SpriteNameGUI ()
    {
        if (AssetDatabase.IsMainAsset(target))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, -1, mainAssetNameContent);
                EditorGUI.SelectableLabel(rect, target.name);
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            target.name = EditorGUILayout.TextField(subAssetNameContent, target.name);
            if (EditorGUI.EndChangeCheck())
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
        }
    }

    private void PivotGUI ()
    {
        EditorGUI.BeginChangeCheck();
        pivotValue = EditorGUILayout.Vector2Field(pivotContent, pivotValue);
        if (EditorGUI.EndChangeCheck())
            TargetSprite.SetPivot(pivotValue);
    }
}
