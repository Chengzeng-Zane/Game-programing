using EchoEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the MainMenu scene from a Unity editor menu command.
/// </summary>
/// <remarks>
/// This is an Editor-only utility script.
/// It creates a MainMenuController and a basic camera, then saves MainMenu.unity and updates Build Settings.
/// </remarks>
public static class MainMenuSceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    /// <summary>
    /// Creates or replaces MainMenu.unity with the runtime menu controller setup.
    /// </summary>
    /// <remarks>
    /// Called from the Unity menu item Echo Escape/Build Main Menu Scene.
    /// </remarks>
    [MenuItem("Echo Escape/Build Main Menu Scene")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject controllerObject = new GameObject("Main Menu Controller");
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();
        controller.gameSceneName = "Level 1 - The First Echo";

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color(0.045f, 0.055f, 0.08f, 1f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Command-line entry point for rebuilding the main menu scene.
    /// </summary>
    public static void BuildFromCommandLine()
    {
        BuildScene();
    }
}
