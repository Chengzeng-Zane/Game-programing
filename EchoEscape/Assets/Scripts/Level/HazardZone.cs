using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Detects when the player enters a dangerous trigger area.
    /// </summary>
    /// <remarks>
    /// Attach this script to hazard or death-zone objects with Collider2D set as Trigger.
    /// It notifies EchoEscapeGameManager so the player respawns and pending loot is lost.
    /// </remarks>
    public class HazardZone : MonoBehaviour
    {
        /// <summary>
        /// Message shown in the status text when the player dies in this hazard.
        /// </summary>
        public string deathReason = "hit a hazard";

        [SerializeField]
        private bool debugLogs;

        private static bool fallbackDeathInProgress;

        /// <summary>
        /// Unity physics event called when another 2D collider enters this hazard trigger.
        /// </summary>
        /// <param name="other">The collider that entered the hazard area.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            bool isPlayer = other != null &&
                (other.GetComponent<PlayerController2D>() != null || other.GetComponentInParent<PlayerController2D>() != null);

            if (!isPlayer)
            {
                return;
            }

            KillPlayerUsingExistingDeathFlow(this, deathReason, name, debugLogs);
        }

        /// <summary>
        /// Runs the same death flow used by hazard triggers in scenes with or without a Game Manager.
        /// </summary>
        /// <param name="runner">Component used to start the fallback reload coroutine.</param>
        /// <param name="reason">Short text explaining why the player died.</param>
        /// <param name="sourceName">Name of the hazard object that requested death.</param>
        /// <param name="writeDebugLogs">If true, writes death-flow diagnostics to the console.</param>
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
                EchoEscapeGameManager.Instance.KillPlayer(reason);
                return;
            }

            if (fallbackDeathInProgress || runner == null)
            {
                return;
            }

            fallbackDeathInProgress = true;
            runner.StartCoroutine(ShowDeathAndReloadCurrentScene());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ResetFallbackDeathState()
        {
            fallbackDeathInProgress = false;
        }

        private static IEnumerator ShowDeathAndReloadCurrentScene()
        {
            CreateFallbackDeathUi();
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1f;
            string sceneName = SceneManager.GetActiveScene().name;
            LevelIntroSequence.SkipNextIntroForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }

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
