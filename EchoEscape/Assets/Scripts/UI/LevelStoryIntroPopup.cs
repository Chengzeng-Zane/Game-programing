using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Simple level story pop-up script. It is used to delay the display of a piece of story text after entering the level.
/// Gameplay logic: Start Then wait for a short period of time before finding or creating TutorialPopupManager, and then display the configured title and content.
/// Collaborates with: and TutorialPopupManager Cooperate. The current more complete multi-page introduction is mainly provided by LevelIntroSequence Responsible.
    /// </summary>
    public class LevelStoryIntroPopup : MonoBehaviour
    {
        [SerializeField] private TutorialPopupManager popupManager;
        [SerializeField] private string storyTitle = "Echo Wizard";
        [SerializeField] [TextArea(3, 8)] private string storyMessage;
        [SerializeField] private float delayBeforeShow = 0.3f;

        private bool hasShown;
        /// <summary>
/// Unity Called before the first frame. Here the scene object is usually connected to start the initial UI, tutorial or level process.
        /// </summary>
        private void Start()
        {
// Delay the display a little so that the scene and UI Initialization is completed first.
            StartCoroutine(ShowStoryAfterDelay());
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator ShowStoryAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delayBeforeShow));

            if (hasShown)
            {
// Prevent the same story from popping up multiple times when the coroutine is started repeatedly.
                yield break;
            }

            hasShown = true;
            TutorialPopupManager manager = ResolvePopupManager();
            if (manager != null)
            {
// The specific pause, display and closing logic is handed over to TutorialPopupManager。
                manager.ShowPopup(storyTitle, storyMessage);
            }
        }
        /// <summary>
/// find available TutorialPopupManager。Inspector If it is not configured, first look for it in the scene. If it cannot be found, create it at runtime.
        /// </summary>
/// <returns>return TutorialPopupManager Type result for the caller to continue to judge or use. </returns>
        private TutorialPopupManager ResolvePopupManager()
        {
            if (popupManager != null)
            {
// priority use Inspector The specified popup manager.
                return popupManager;
            }

            popupManager = FindObjectOfType<TutorialPopupManager>();
            if (popupManager != null)
            {
// Reuse when the scene already has a pop-up manager to avoid duplication Canvas。
                return popupManager;
            }

// No pop-ups UI Create a minimum usable version.
            popupManager = CreateRuntimePopupManager();
            return popupManager;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <returns>return TutorialPopupManager Type result for the caller to continue to judge or use. </returns>
        private static TutorialPopupManager CreateRuntimePopupManager()
        {
            GameObject canvasObject = new GameObject("TutorialPopupUI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("TutorialPopupPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(740f, 380f);
            panelObject.AddComponent<CanvasRenderer>();
            panelObject.AddComponent<Image>();

            Text titleText = CreateText("TitleText", panelObject.transform);
            Text bodyText = CreateText("BodyText", panelObject.transform);
            CreateText("CloseHintText", panelObject.transform);

            TutorialPopupManager manager = canvasObject.AddComponent<TutorialPopupManager>();
            manager.popupPanel = panelObject;
            manager.titleText = titleText;
            manager.bodyText = bodyText;
            manager.pauseGameWhenOpen = true;
            return manager;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="objectName">to create or find GameObject name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <returns>Returns a created or found UI Text components. </returns>
        private static Text CreateText(string objectName, Transform parent)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            textObject.AddComponent<CanvasRenderer>();
            Text text = textObject.AddComponent<Text>();
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }
    }
}
