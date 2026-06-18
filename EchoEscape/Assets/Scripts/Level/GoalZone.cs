using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Level Exit/Portal script. It is responsible for settlement when the player reaches the exit loot, play through level feedback, and decide whether to load the next level or play the final ending.
/// Gameplay logic: After entering the trigger, first confirm that the object is a real player; then pending loot settled into secured loot；Level1/Level2 Will enter the next level; Level3 will be displayed first loot, and then play the wizard's ending.
/// Collaboration: call EchoEscapeGameManager、LootFeedbackUI、LevelIntroSequence and SceneManager。
    /// </summary>
    public class GoalZone : MonoBehaviour
    {
        private const string Level3SceneName = "Level 3 - Escape from the Silent Forest";
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
/// Set the name of the next level to be loaded after passing the level at runtime. Used to dynamically configure exports for scenarios or code.
        /// </summary>
/// <param name="sceneName">Target scene name for inspection Build Settings Or load the next level. </param>
        public void ConfigureNextScene(string sceneName)
        {
            nextSceneName = sceneName;
            useNextBuildIndex = false;
        }
        /// <summary>
/// When the player enters the exit trigger, the clearance logic is executed: player confirmation, settlement loot, play feedback, load the next level or trigger the ending.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered || !IsPlayer(other))
            {
// The end point can only be triggered once, and it must be a real player; Echo Cannot clear levels for players.
                return;
            }

            hasTriggered = true;

            if (debugLogs)
            {
                Debug.Log("Goal reached by Player.");
            }

            if (TryLoadNextScene())
            {
// Successfully entered the switch/At the end of the process, the backup logic of "only completing the current level" is no longer executed.
                return;
            }

// When the next level is not configured or is unavailable, at least let the current level be settled according to the victory process.
            CompleteCurrentLevelOnly();
        }
        /// <summary>
/// determines entry into the end trigger Collider Whether it is a real player. Echo Shouldn't pass.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsPlayer(Collider2D other)
        {
            return HasTag(other, "Player") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null;
        }
        /// <summary>
/// Try to load the configured next level. If there is no loadable scene, only the current level will be completed without forced switching.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool TryLoadNextScene()
        {
            string explicitSceneName = string.IsNullOrWhiteSpace(nextSceneName) ? string.Empty : nextSceneName.Trim();
            if (!string.IsNullOrEmpty(explicitSceneName))
            {
// priority use Inspector Explicitly configure the scene name to avoid Build Index A change in sequence results in entering the wrong level.
                return LoadSceneIfAvailable(explicitSceneName);
            }

            if (!useNextBuildIndex)
            {
// Not enabled Build Index When automatically switching off, the exporter is only responsible for settling the current switch.
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

// through Build Index Before automatically entering the next level, you must also first pending loot settled into secured loot。
            SecurePendingLootBeforeSceneLoad();
            SceneManager.LoadScene(nextIndex);
            return true;
        }
        /// <summary>
/// examine Build Settings Whether the target scene exists in it, it will be called only if it exists. SceneManager. LoadScene。
        /// </summary>
/// <param name="sceneName">Target scene name for inspection Build Settings Or load the next level. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
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
// The third level needs to be displayed before going to the main menu. loot, and then display the ending plot, so this is handed over to the coroutine.
                return true;
            }

// Settlement before switching to normal levels lootto ensure that the reward is saved when entering the next level.
            SecurePendingLootBeforeSceneLoad();
            SceneManager.LoadScene(sceneName);
            return true;
        }
        /// <summary>
/// The third level ends with special logic. it will give way first loot Settlement UI appears, and then play the final plot to ensure that the sequence matches the player experience.
        /// </summary>
/// <param name="sceneName">Target scene name for inspection Build Settings Or load the next level. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool TryShowLevel3EndingBeforeSceneLoad(string sceneName)
        {
            if (SceneManager.GetActiveScene().name != Level3SceneName || sceneName != MainMenuSceneName)
            {
// only Level3 -> MainMenu Only the final plot is needed, other aspects follow ordinary logic.
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
/// Settlement before switching scenes pendingLoot, to prevent the items that players just got after clearing the level from not entering. securedLoot。
        /// </summary>
/// <returns>Returns an integer result, usually representing the quantity, index, or quantity of this settlement. </returns>
        private int SecurePendingLootBeforeSceneLoad()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
                return EchoEscapeGameManager.Instance.SecurePendingLoot();
            }

            return 0;
        }
        /// <summary>
/// coroutine: wait loot The feedback is displayed for a period of time, then the ending of the third level is played, and finally the target scene is entered.
        /// </summary>
/// <param name="targetSceneName">targetSceneName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="introSequence">introSequence Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator ShowLevel3EndingAfterLootFeedback(string targetSceneName, LevelIntroSequence introSequence)
        {
            int securedLootCount = SecurePendingLootBeforeSceneLoad();
            if (securedLootCount > 0)
            {
// have loot give first UI Time to show "what was gained" and then play the wizard's closing words.
                yield return new WaitForSecondsRealtime(Level3EndingDelayAfterLoot);
            }

            if (introSequence != null && introSequence.ShowEndingSequence(targetSceneName))
            {
// LevelIntroSequence It will play the ending by itself and be responsible for loading the target scene.
                yield break;
            }

// If there is no ending system or the playback fails, it will still switch back to the target scene to avoid players getting stuck at the end.
            SceneManager.LoadScene(targetSceneName);
        }
        /// <summary>
/// When there is no next level to load, only the current level is marked as completed and the level completion feedback is played.
        /// </summary>
        private void CompleteCurrentLevelOnly()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
// use GameManager. Win Unified playback of successful sound effects and settlement loot and updates HUD。
                EchoEscapeGameManager.Instance.Win();
            }
            else
            {
                Debug.Log("Level complete. Player reached the exit.");
            }
        }
        /// <summary>
/// security check Collider or root object tag, to avoid objects that do not correspond to tag time throw UnityException。
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <param name="tagName">tagName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
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
