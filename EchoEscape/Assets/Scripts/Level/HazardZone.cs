using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Common danger zone scripts, used for pits, rivers, death boundaries at the bottom of maps, etc.
/// Gameplay logic: After the player enters the trigger, he will go through a unified death process: playback of death feedback and display You Died, lost pending loot, and reload the current level. It also provides fallback, to avoid not having GameManager There was no response at all.
/// Collaborates with: EchoEscapeGameManager It is the main entrance to death; GravityFlipVoidKillZone Reuse its public death function.
    /// </summary>
    public class HazardZone : MonoBehaviour
    {
        public string deathReason = "hit a hazard";

        [SerializeField]
        private bool debugLogs;

        private static bool fallbackDeathInProgress;
        /// <summary>
/// The death process is triggered when the player enters the normal danger zone. This function will filter Echo and non-player objects to avoid accidental killing of players by mechanisms or replays.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            bool isPlayer = other != null &&
                (other.GetComponent<PlayerController2D>() != null || other.GetComponentInParent<PlayerController2D>() != null);

            if (!isPlayer)
            {
// Only real players will be killed in rivers, pit bottoms and bottom death zones; Echo or other triggers will not reload the level when entered.
                return;
            }

            KillPlayerUsingExistingDeathFlow(this, deathReason, name, debugLogs);
        }
        /// <summary>
/// Public death entrance. Priority call EchoEscapeGameManager. KillPlayer; If the scene does not have GameManager, just use fallback UI and reload process to avoid game freezes.
        /// </summary>
/// <param name="runner">runner Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="reason">cause of death or event, used for death UI, status prompts and debugging logs. </param>
/// <param name="sourceName">sourceName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="writeDebugLogs">writeDebugLogs Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public static void KillPlayerUsingExistingDeathFlow(MonoBehaviour runner, string reason, string sourceName, bool writeDebugLogs)
        {
            bool hasGameManager = EchoEscapeGameManager.Instance != null;
            if (writeDebugLogs)
            {
                Debug.Log("[DeathFlow] KillPlayer called");
                Debug.Log($"[DeathFlow] reason = {reason}");
                Debug.Log($"[DeathFlow] will respawn/reload = {hasGameManager || runner != null}");
            }

            if (hasGameManager)
            {
// You should go through all normal levels GameManager: It will handle the death animation, UI、pending loot Lose and reload the current level.
                EchoEscapeGameManager.Instance.KillPlayer(reason);
                return;
            }

            if (fallbackDeathInProgress || runner == null)
            {
// fallback It is only allowed to be started once to avoid multiple dangerous zones triggering at the same time resulting in repeated loading of scenes.
                return;
            }

// No GameManager The test scene should also be able to die and reload, so that the dangerous zone can be tested separately.
            fallbackDeathInProgress = true;
            runner.StartCoroutine(ShowDeathAndReloadCurrentScene());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        /// <summary>
/// reset fallback state of death. Prevent old states from affecting the next death when reloading or testing the scene.
        /// </summary>
        private static void ResetFallbackDeathState()
        {
            fallbackDeathInProgress = false;
        }
        /// <summary>
/// fallback Death coroutine. show simple death UI, wait for a short period of time and then reload the current scene.
        /// </summary>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private static IEnumerator ShowDeathAndReloadCurrentScene()
        {
// fallback No LootFeedbackUI, so it is easiest to temporarily create a You Died panel.
            CreateFallbackDeathUi();
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1f;
            string sceneName = SceneManager.GetActiveScene().name;
// The opening story of this level will be skipped when reloading after death, so players will not have to rewatch the story every time they die.
            LevelIntroSequence.SkipNextIntroForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }
        /// <summary>
/// Dynamically create a simple You Died UI. only if there is no GameManager of fallback It will only be used under certain circumstances.
        /// </summary>
        private static void CreateFallbackDeathUi()
        {
            GameObject canvasObject = new GameObject("DeathFeedbackUI");

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 130;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("DeathPanel");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420f, 150f);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.78f);
            image.raycastTarget = false;

            GameObject textObject = new GameObject("DeathText");
            textObject.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(360f, 80f);

            Text text = textObject.AddComponent<Text>();
            text.text = "You Died";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
        }
    }
}
