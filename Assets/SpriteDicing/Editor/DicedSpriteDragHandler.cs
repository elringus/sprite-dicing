using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class DicedSpriteDragHandler : Editor
{
    private const float SPAWN_POSITION_OFFSET = 10F;

    static DicedSpriteDragHandler ()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    private static void OnSceneGUI (SceneView sceneView)
    {
        if (!IsDicedSpriteDragged()) return;

        switch (Event.current.type)
        {
            case EventType.DragUpdated:
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                Event.current.Use();
                break;

            case EventType.DragPerform:
                SpawnDraggedSprite();
                DragAndDrop.AcceptDrag();
                Event.current.Use();
                break;

            default:
                break;
        }
    }

    private static bool IsDicedSpriteDragged ()
    {
        return DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is DicedSprite;
    }

    private static void SpawnDraggedSprite ()
    {
        var dicedSprite = DragAndDrop.objectReferences[0] as DicedSprite;
        var spriteGameObject = new GameObject(dicedSprite.Name);
        var spawnRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var spawnPosition = spawnRay.origin + spawnRay.direction * SPAWN_POSITION_OFFSET;
        spriteGameObject.transform.position = spawnPosition;
        spriteGameObject.transform.rotation = Quaternion.LookRotation(spawnRay.direction);
        var dicedSpriteRenderer = spriteGameObject.AddComponent<DicedSpriteRenderer>();
        dicedSpriteRenderer.SetDicedSprite(dicedSprite);
    }
}
