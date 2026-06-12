using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoEscape
{
    /// <summary>
    /// Detects when the player reaches the level exit and optionally loads the next scene.
    /// </summary>
    /// <remarks>
    /// Attach this script to the exit trigger object.
    /// It only responds to Player objects, not Echo replay objects.
    /// When a next scene is configured, it loads that scene; otherwise it falls back to completing the current run.
    /// </remarks>
    public class GoalZone : MonoBehaviour
    {
        private const string Level3SceneName = "Level3_RiskReward";
        private const string MainMenuSceneName = "MainMenu";
        private const float Level3EndingDelayAfterLoot = 2.75f;

        [SerializeField]
        private string nextSceneName;

        [SerializeField]
        private bool useNextBuildIndex;

        [SerializeField]
        private bool debugLogs = true;

        private bool hasTriggered;

        /// <summary>
        /// Sets the explicit scene name loaded when the player reaches this goal.
        /// </summary>
        /// <param name="sceneName">Scene name to load, without the .unity extension.</param>
        /// <remarks>
        /// Used by editor scene builders so regenerated scenes keep the correct next-level link.
        /// </remarks>
        public void ConfigureNextScene(string sceneName)
        {
            nextSceneName = sceneName;
            useNextBuildIndex = false;
        }

        /// <summary>
        /// Unity physics event called when another 2D collider enters the exit trigger.
        /// </summary>
        /// <param name="other">The collider that entered the exit area.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered || !IsPlayer(other))
            {
                return;
            }

            hasTriggered = true;

            if (debugLogs)
            {
                Debug.Log("Goal reached by Player.");
            }

            if (TryLoadNextScene())
            {
                return;
            }

            CompleteCurrentLevelOnly();
        }

        /// <summary>
        /// Checks whether a collider belongs to the real player.
        /// </summary>
        /// <param name="other">The collider that entered the goal trigger.</param>
        /// <returns>True if the collider or parent object is the Player; otherwise false.</returns>
        private bool IsPlayer(Collider2D other)
        {
            return HasTag(other, "Player") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null;
        }

        /// <summary>
        /// Loads the configured next scene if it exists in Build Settings.
        /// </summary>
        /// <returns>True if a scene load was started; otherwise false.</returns>
        private bool TryLoadNextScene()
        {
            string explicitSceneName = string.IsNullOrWhiteSpace(nextSceneName) ? string.Empty : nextSceneName.Trim();
            if (!string.IsNullOrEmpty(explicitSceneName))
            {
                return LoadSceneIfAvailable(explicitSceneName);
            }

            if (!useNextBuildIndex)
            {
                return false;
            }

            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;
            if (currentIndex < 0 || nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("Next scene is not available in Build Settings.");
                return false;
            }

            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (debugLogs)
            {
                Debug.Log("Loading next scene: " + sceneName);
            }

            SecurePendingLootBeforeSceneLoad();
            SceneManager.LoadScene(nextIndex);
            return true;
        }

        /// <summary>
        /// Loads an explicitly named scene if Unity reports it as available.
        /// </summary>
        /// <param name="sceneName">Scene name to load.</param>
        /// <returns>True if the scene load was started; otherwise false.</returns>
        private bool LoadSceneIfAvailable(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("Next scene is not available in Build Settings.");
                return false;
            }

            if (debugLogs)
            {
                Debug.Log("Loading next scene: " + sceneName);
            }

            if (TryShowLevel3EndingBeforeSceneLoad(sceneName))
            {
                return true;
            }

            SecurePendingLootBeforeSceneLoad();
            SceneManager.LoadScene(sceneName);
            return true;
        }

        /// <summary>
        /// Shows the Level3 final Echo Wizard dialogue before returning to MainMenu.
        /// </summary>
        /// <param name="sceneName">Scene name requested by the exit.</param>
        /// <returns>True if the ending sequence took over scene loading.</returns>
        private bool TryShowLevel3EndingBeforeSceneLoad(string sceneName)
        {
            if (SceneManager.GetActiveScene().name != Level3SceneName || sceneName != MainMenuSceneName)
            {
                return false;
            }

            LevelIntroSequence introSequence = FindObjectOfType<LevelIntroSequence>();
            if (introSequence == null)
            {
                Debug.LogWarning("Level3 ending dialogue could not be shown because LevelIntroSequence is missing.");
                return false;
            }

            StartCoroutine(ShowLevel3EndingAfterLootFeedback(sceneName, introSequence));
            return true;
        }

        /// <summary>
        /// Secures pending loot before the current scene unloads.
        /// </summary>
        /// <returns>Number of loot items secured.</returns>
        private int SecurePendingLootBeforeSceneLoad()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
                return EchoEscapeGameManager.Instance.SecurePendingLoot();
            }

            return 0;
        }

        /// <summary>
        /// Shows Level3 secured-loot feedback before the final Echo Wizard dialogue.
        /// </summary>
        /// <param name="targetSceneName">Scene loaded after the ending dialogue completes.</param>
        /// <param name="introSequence">Intro sequence used for the ending dialogue.</param>
        /// <returns>Coroutine steps for delayed ending playback.</returns>
        private IEnumerator ShowLevel3EndingAfterLootFeedback(string targetSceneName, LevelIntroSequence introSequence)
        {
            int securedLootCount = SecurePendingLootBeforeSceneLoad();
            if (securedLootCount > 0)
            {
                yield return new WaitForSecondsRealtime(Level3EndingDelayAfterLoot);
            }

            if (introSequence != null && introSequence.ShowEndingSequence(targetSceneName))
            {
                yield break;
            }

            SceneManager.LoadScene(targetSceneName);
        }

        /// <summary>
        /// Completes the current level without loading another scene.
        /// </summary>
        /// <remarks>
        /// This preserves the old GoalZone behavior for scenes that do not configure a next scene.
        /// </remarks>
        private void CompleteCurrentLevelOnly()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
                EchoEscapeGameManager.Instance.Win();
            }
            else
            {
                Debug.Log("Level complete. Player reached the exit.");
            }
        }

        /// <summary>
        /// Safely checks a tag without throwing if the tag is not defined.
        /// </summary>
        /// <param name="other">Collider whose object or root object should be checked.</param>
        /// <param name="tagName">Tag name to compare.</param>
        /// <returns>True if the collider object or root object has the tag; otherwise false.</returns>
        private bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
