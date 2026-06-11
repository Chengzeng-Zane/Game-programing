using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Shows a one-time story popup shortly after a level starts.
    /// </summary>
    /// <remarks>
    /// Attach this to a scene object and assign story text in the Inspector.
    /// It reuses TutorialPopupManager when one exists, or creates a small compatible popup UI.
    /// </remarks>
    public class LevelStoryIntroPopup : MonoBehaviour
    {
        [SerializeField] private TutorialPopupManager popupManager;
        [SerializeField] private string storyTitle = "Echo Wizard";
        [SerializeField] [TextArea(3, 8)] private string storyMessage;
        [SerializeField] private float delayBeforeShow = 0.3f;

        private bool hasShown;

        /// <summary>
        /// Description:
        /// Starts the delayed story popup.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Start()
        {
            StartCoroutine(ShowStoryAfterDelay());
        }

        /// <summary>
        /// Description:
        /// Waits briefly, then shows the story popup once.
        /// Inputs:
        /// none
        /// Returns:
        /// IEnumerator - delayed popup routine
        /// </summary>
        private IEnumerator ShowStoryAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delayBeforeShow));

            if (hasShown)
            {
                yield break;
            }

            hasShown = true;
            TutorialPopupManager manager = ResolvePopupManager();
            if (manager != null)
            {
                manager.ShowPopup(storyTitle, storyMessage);
            }
        }

        /// <summary>
        /// Description:
        /// Finds an existing popup manager or creates a compatible runtime one.
        /// Inputs:
        /// none
        /// Returns:
        /// TutorialPopupManager - manager used to display the story
        /// </summary>
        private TutorialPopupManager ResolvePopupManager()
        {
            if (popupManager != null)
            {
                return popupManager;
            }

            popupManager = FindObjectOfType<TutorialPopupManager>();
            if (popupManager != null)
            {
                return popupManager;
            }

            popupManager = CreateRuntimePopupManager();
            return popupManager;
        }

        /// <summary>
        /// Description:
        /// Creates a minimal tutorial popup UI for scenes without one.
        /// Inputs:
        /// none
        /// Returns:
        /// TutorialPopupManager - created popup manager
        /// </summary>
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
        /// Description:
        /// Creates one UI text object under the popup panel.
        /// Inputs:
        /// objectName - text object name
        /// parent - parent transform
        /// Returns:
        /// Text - created text component
        /// </summary>
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
