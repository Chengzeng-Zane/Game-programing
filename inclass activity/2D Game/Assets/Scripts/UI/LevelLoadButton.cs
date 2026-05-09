using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// This class is meant to be used on buttons as a quick easy way to load levels (scenes)
/// </summary>
public class LevelLoadButton : MonoBehaviour
{
    /// <summary>
    /// Description:
    /// Loads a level according to the name provided
    /// Input:
    /// string levelToLoadName
    /// Returns:
    /// void (no return)
    /// </summary>
    /// <param name="levelToLoadName">The name of the level to load</param>
    public void LoadLevelByName(string levelToLoadName)
    {
        if (!SceneExistsInBuild(levelToLoadName))
        {
            Debug.LogWarning("Cannot load scene '" + levelToLoadName + "' because it is not in the build settings.");
            return;
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(levelToLoadName);
    }

    /// <summary>
    /// Description:
    /// Checks whether a scene name is available in the build settings before trying to load it.
    /// Inputs:
    /// string sceneName
    /// Returns:
    /// bool
    /// </summary>
    private bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneFileName == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
