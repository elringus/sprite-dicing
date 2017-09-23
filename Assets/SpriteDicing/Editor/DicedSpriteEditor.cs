using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DicedSprite)), CanEditMultipleObjects]
public class DicedSpriteEditor : Editor
{
    protected DicedSprite TargetSprite { get { return target as DicedSprite; } }

    private SerializedProperty pivot;
    private SerializedProperty atlasTexture;

    private GUIContent subAssetNameContent = new GUIContent("Name", "Name of the diced sprite object. Used to find it among others in the parent atlas. Multi-editing is not supported.");
    private GUIContent mainAssetNameContent = new GUIContent("Name", "Name of the diced sprite object. Used to find it among others in the parent atlas. Raname asset object in the editor to change.");
    private GUIContent pivotContent = new GUIContent("Pivot", "Relative pivot point position in 0 to 1 range, counting from the bottom-left corner.");
    private GUIContent atlasTextureContent = new GUIContent("Atlas Texture", "Reference to the atlas texture where the dices of the original sprite texture are stored.");

    private void OnEnable ()
    {
        pivot = serializedObject.FindProperty("_pivot");
        atlasTexture = serializedObject.FindProperty("atlasTexture");
    }

    public override void OnInspectorGUI ()
    {
        serializedObject.Update();
        SpriteNameGUI();
        EditorGUILayout.PropertyField(pivot, pivotContent);
        EditorGUILayout.PropertyField(atlasTexture, atlasTextureContent);
        serializedObject.ApplyModifiedProperties();
    }

    public override bool HasPreviewGUI () { return true; }

    public override void OnPreviewGUI (Rect previewRect, GUIStyle background)
    {
        if (TargetSprite && TargetSprite.Vertices.Count >= 8)
        {
            var spriteRect = TargetSprite.EvaluateSpriteRect();
            var unitSize = (TargetSprite.Vertices[2].y - TargetSprite.Vertices[0].y);
            var pivorCorrection = new Vector2(TargetSprite.Pivot.x * spriteRect.size.x, TargetSprite.Pivot.y * spriteRect.size.y);
            var sizeCorrection = UnityCommon.MathUtils.MaxScaleKeepAspect(previewRect.size, spriteRect.size);
            var centerXCorrection = previewRect.center.x / sizeCorrection - (TargetSprite.Vertices.Average(v => v.x) + pivorCorrection.x);

            // Iterate target sprite's quads (4 verts each).
            for (int i = 0; i < (TargetSprite.Vertices.Count - 3); i += 4)
            {
                // Evaluate UV rect of the current quad.
                var uvRect = new Rect(TargetSprite.UVs[i], TargetSprite.UVs[i + 2] - TargetSprite.UVs[i]);

                // Evaluate draw rect of the current quad.
                var drawRect = new Rect(previewRect);
                drawRect.size = Vector2.one * unitSize * sizeCorrection;
                var drawPosX = TargetSprite.Vertices[i].x + pivorCorrection.x + centerXCorrection;
                var drawPosY = spriteRect.max.y - TargetSprite.Vertices[i].y - unitSize;
                drawRect.position += new Vector2(drawPosX, drawPosY) * sizeCorrection;

                // Draw texture of the current quad.
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                GUI.DrawTextureWithTexCoords(drawRect, TargetSprite.AtlasTexture, uvRect, true);
                GL.sRGBWrite = false;
            }
        }
    }

    public override string GetInfoString ()
    {
        if (!TargetSprite) return string.Empty;

        var spriteRect = TargetSprite.EvaluateSpriteRect();

        var x = Mathf.RoundToInt(spriteRect.size.x);
        var y = Mathf.RoundToInt(spriteRect.size.y);

        return string.Format("Sprite Size: {0}x{1} | Dices Allocated: {2}", x, y, TargetSprite.Vertices.Count / 4f);
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

    [MenuItem("GameObject/2D Object/Diced Sprite")]
    private static void CreateDicedSprite ()
    {
        new GameObject("New Diced Sprite", typeof(DicedSpriteRenderer));
    }
}
