using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    private const int buttonHeight = 50;
    private const int buttonWidth = 150;

    private void OnGUI ()
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            if (GUI.Button(GetRectForSceneAt(i), GetNameForSceneAt(i)))
                SceneManager.LoadScene(i);
    }

    private static Rect GetRectForSceneAt (int index)
    {
        var yPos = Screen.height - (buttonHeight + buttonHeight * index);
        return new Rect(0, yPos, buttonWidth, buttonHeight);
    }

    private static string GetNameForSceneAt (int index)
    {
        var scenePath = SceneUtility.GetScenePathByBuildIndex(index);
        return Path.GetFileNameWithoutExtension(scenePath);
    }
}
