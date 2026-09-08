#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OpenDefaultSceneOnProjectLaunch
{
    private const string DefaultScenePath = "Assets/Scenes/heping_Main.unity";

    static OpenDefaultSceneOnProjectLaunch()
    {
        EditorApplication.delayCall += OpenDefaultSceneWhenNeeded;
    }

    private static void OpenDefaultSceneWhenNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(DefaultScenePath))
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            return;
        }

        EditorSceneManager.OpenScene(DefaultScenePath, OpenSceneMode.Single);
    }
}
#endif
