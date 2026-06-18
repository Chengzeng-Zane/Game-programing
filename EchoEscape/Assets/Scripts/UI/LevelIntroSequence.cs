using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: level plot introduction and ending process controller. It is responsible for the story introduction at the beginning of each level, the ending of the third level, and the skipping of the already viewed introduction after death and respawn.
/// Gameplay logic: multi-page plots are displayed and the game is paused when entering a level; time is restored after the player closes the level; the same level is skipped through static recording when reloading after death intro; You can wait first when clearing the third level. loot After the feedback is displayed, the wizard's closing remarks are played.
/// Collaborates with: GoalZone Call the end process; TutorialPopupManager/runtime UI Responsible for display; Time. timeScale Control the game pause.
    /// </summary>
    public class LevelIntroSequence : MonoBehaviour
    {
        private const string PixelFontPath = "BrackeysPlatformer/Fonts/PixelOperator8-Bold";
        private const string DefaultContinueHint = "Press C to continue.";
        private const string EndingContinueHint = "Press C to return to Main Menu.";
        private const string EndingBodyText = "You escaped the Silent Forest.\nThank you for playing Echo Escape.";
        private static readonly HashSet<string> scenesSkippingNextIntro = new HashSet<string>();

        [SerializeField] private GameObject introRoot;
        [SerializeField] private Text storyText;
        [SerializeField] private Text continueHintText;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite dialogueBackgroundSprite;
        [SerializeField] private int dialogueStartPageIndex = 3;
        [SerializeField] private string dialogueSpeakerName = "Echo Wizard";
        [SerializeField] private string[] pages;
        [SerializeField] private KeyCode continueKey = KeyCode.C;
        [SerializeField] private bool pauseGameplayDuringIntro = true;

        private GameObject forestIntroPanel;
        private GameObject dialoguePanel;
        private Text speakerNameText;
        private Text dialogueBodyText;
        private Text dialogueContinueHintText;
        private string[] activePages;
        private int activeDialogueStartPageIndex;
        private string activeDialogueSpeakerName;
        private string activeContinueHint = DefaultContinueHint;
        private string sceneToLoadOnComplete;
        private int pageIndex;
        private bool introActive;
        private bool pausedGameplay;
        private bool loadSceneOnComplete;
        private float previousTimeScale = 1f;

        private const string OldSpeakerName = "Echo Sage";
        private const string CurrentSpeakerName = "Echo Wizard";
        /// <summary>
/// Unity Called before the first frame. Here the scene object is usually connected to start the initial UI, tutorial or level process.
        /// </summary>
        private void Start()
        {
            if (pages == null || pages.Length == 0)
            {
// Not displayed when story page is not configured intro, to prevent empty panels from blocking the game.
                return;
            }

            if (ConsumeSkipIntroForCurrentScene())
            {
// A skip mark will be set when reloading the current level after death; this way players will not have to re-watch the plot every time they die.
                if (introRoot != null)
                {
                    introRoot.SetActive(false);
                }

                RestoreGameplayTime();
                return;
            }

            EnsureIntroUi();
            ShowIntro();
        }
        /// <summary>
/// Record a scene to skip the intro the next time it loads. Before Death Reload GameManager/HazardZone call.
        /// </summary>
/// <param name="sceneName">Target scene name for inspection Build Settings Or load the next level. </param>
        public static void SkipNextIntroForScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                scenesSkippingNextIntro.Add(sceneName);
            }
        }
        /// <summary>
/// Check if the current scene has "Skip intro" mark; consume it if you have it, and make sure you only skip the next time.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private static bool ConsumeSkipIntroForCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName) || !scenesSkippingNextIntro.Contains(sceneName))
            {
                return false;
            }

// Removing the mark means skipping it only once, and players will still see the story when they re-enter the level from the main menu.
            scenesSkippingNextIntro.Remove(sceneName);
            return true;
        }
        /// <summary>
/// Unity Called every frame. This handles input, timers, UI Real-time refresh of state or non-physics.
        /// </summary>
        private void Update()
        {
            if (!introActive || !Input.GetKeyDown(continueKey))
            {
                return;
            }

// C key to turn to the next page; end the current one when past the last page intro/ending process.
            pageIndex++;
            if (activePages == null || pageIndex >= activePages.Length)
            {
                CompleteActiveSequence();
                return;
            }

            ApplyPageText();
        }
        /// <summary>
/// Called when the object is disabled. Here usually recovery time scales or cleans UI status to avoid pause residue.
        /// </summary>
        private void OnDisable()
        {
            RestoreGameplayTime();
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
        private void ShowIntro()
        {
// activePages let the same UI The system can display both the opening story and the end of the third level.
            activePages = pages;
            activeDialogueStartPageIndex = dialogueStartPageIndex;
            activeDialogueSpeakerName = dialogueSpeakerName;
            activeContinueHint = DefaultContinueHint;
            loadSceneOnComplete = false;
            sceneToLoadOnComplete = string.Empty;

            pageIndex = 0;
            introActive = true;
            ApplyPageText();

            if (introRoot != null)
            {
                introRoot.SetActive(true);
            }

            if (pauseGameplayDuringIntro && !pausedGameplay)
            {
// The game is paused during the opening story, and the player cannot be affected by enemies or gravity while reading the story.
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedGameplay = true;
            }
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
/// <param name="targetSceneName">targetSceneName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        public bool ShowEndingSequence(string targetSceneName)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
// The ending is not played when there is no target scene to avoid not knowing where to cut to after the end.
                return false;
            }

            EnsureIntroUi();

// The dialogue panel is reused at the end, only one page of the wizard's closing words is displayed, and the target scene is loaded after the end.
            activePages = new[] { EndingBodyText };
            activeDialogueStartPageIndex = 0;
            activeDialogueSpeakerName = CurrentSpeakerName;
            activeContinueHint = EndingContinueHint;
            sceneToLoadOnComplete = targetSceneName.Trim();
            loadSceneOnComplete = true;

            pageIndex = 0;
            introActive = true;
            ApplyPageText();

            if (introRoot != null)
            {
                introRoot.SetActive(true);
            }

            if (pauseGameplayDuringIntro && !pausedGameplay)
            {
// The game is also paused when playing at the end to prevent the player from continuing to move and trigger other objects.
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedGameplay = true;
            }

            return true;
        }
        /// <summary>
/// Hide correspondence UI Or visual state, usually called when the prompt ends, the pop-up window is closed, or the process is cleaned up.
        /// </summary>
        private void HideIntro()
        {
            introActive = false;
            if (introRoot != null)
            {
                introRoot.SetActive(false);
            }

            RestoreGameplayTime();
        }
        /// <summary>
/// Complete the current opening or closing sequence. it will hide UI, recovery time, and load the target scene when needed at the end of the process.
        /// </summary>
        private void CompleteActiveSequence()
        {
// Cache first whether you need to switch scenes, because HideIntro Will clear UI state.
            bool shouldLoadScene = loadSceneOnComplete;
            string targetSceneName = sceneToLoadOnComplete;

            HideIntro();

            loadSceneOnComplete = false;
            sceneToLoadOnComplete = string.Empty;

            if (shouldLoadScene && !string.IsNullOrWhiteSpace(targetSceneName))
            {
// The main menu is loaded only after the ending process is completed to prevent half of the plot from being cut away.
                SceneManager.LoadScene(targetSceneName);
            }
        }
        /// <summary>
/// Restore the situation before the plot started Time. timeScale. used to close intro, disable the object or prevent the paused state from remaining.
        /// </summary>
        private void RestoreGameplayTime()
        {
            if (!pausedGameplay)
            {
                return;
            }

// Resume before suspension timeScale; If abnormality was recorded before, return to normal speed 1。
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            previousTimeScale = 1f;
            pausedGameplay = false;
        }
        /// <summary>
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
        private void ApplyPageText()
        {
            if (activePages == null || activePages.Length == 0)
            {
                return;
            }

            bool isDialoguePage = pageIndex >= activeDialogueStartPageIndex;
// The first few pages use a full-screen forest narrative, and the next few pages cut to the wizard's dialog box.
            if (forestIntroPanel != null)
            {
                forestIntroPanel.SetActive(!isDialoguePage);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(isDialoguePage);
            }

            string currentPage = NormalizeStoryText(activePages[Mathf.Clamp(pageIndex, 0, activePages.Length - 1)]);
            if (!isDialoguePage && storyText != null)
            {
// Normal story pages appear in the center of the full screen.
                storyText.text = currentPage;
            }

            if (!isDialoguePage && continueHintText != null)
            {
                continueHintText.text = activeContinueHint;
            }

            if (isDialoguePage && speakerNameText != null)
            {
// The conversation page displays the speaker, currently unified into Echo Wizard。
                speakerNameText.text = NormalizeStoryText(activeDialogueSpeakerName);
            }

            if (isDialoguePage && dialogueBodyText != null)
            {
                dialogueBodyText.text = currentPage;
            }

            if (isDialoguePage && dialogueContinueHintText != null)
            {
                dialogueContinueHintText.text = activeContinueHint;
            }
        }
        /// <summary>
/// ensure opening/ending UI exist. The scene is not configured manually UI will be created at runtime Canvas, background and text.
        /// </summary>
        private void EnsureIntroUi()
        {
            if (introRoot != null && storyText != null && continueHintText != null)
            {
// Already in the scene UI Reuse when referencing, do not create repeatedly Canvas。
                return;
            }

// No manual ride UI The scenario will be generated completely when running intro Canvas, ensuring that every level can display the story.
            GameObject canvasObject = new GameObject("LevelIntroCanvas");
            canvasObject.transform.SetParent(transform, false);
            introRoot = canvasObject;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            CreateForestIntroPanel(canvasObject.transform);
            CreateDialoguePanel(canvasObject.transform);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
        private void CreateForestIntroPanel(Transform parent)
        {
            forestIntroPanel = new GameObject("ForestIntroPanel", typeof(RectTransform));
            forestIntroPanel.transform.SetParent(parent, false);
            StretchToFill(forestIntroPanel.GetComponent<RectTransform>());

            CreateBackgroundImage("BackgroundImage", forestIntroPanel.transform, backgroundSprite, AspectRatioFitter.AspectMode.EnvelopeParent);

            Color overlayColor = backgroundSprite != null
                ? new Color(0.005f, 0.008f, 0.014f, 0.54f)
                : new Color(0.005f, 0.008f, 0.014f, 0.96f);
            Image overlay = CreateImage("DarkOverlay", forestIntroPanel.transform, overlayColor);
            StretchToFill(overlay.rectTransform);

            storyText = CreateText("StoryText", forestIntroPanel.transform, 32, TextAnchor.MiddleCenter, new Color(0.92f, 0.94f, 1f, 1f));
            RectTransform storyRect = storyText.GetComponent<RectTransform>();
            storyRect.anchorMin = new Vector2(0.5f, 0.5f);
            storyRect.anchorMax = new Vector2(0.5f, 0.5f);
            storyRect.pivot = new Vector2(0.5f, 0.5f);
            storyRect.anchoredPosition = new Vector2(0f, 24f);
            storyRect.sizeDelta = new Vector2(880f, 260f);

            continueHintText = CreateText("ContinueHintText", forestIntroPanel.transform, 20, TextAnchor.MiddleRight, new Color(0.78f, 0.88f, 1f, 1f));
            RectTransform hintRect = continueHintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-54f, 42f);
            hintRect.sizeDelta = new Vector2(360f, 42f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
        private void CreateDialoguePanel(Transform parent)
        {
            dialoguePanel = new GameObject("EchoSageDialoguePanel", typeof(RectTransform));
            dialoguePanel.transform.SetParent(parent, false);
            StretchToFill(dialoguePanel.GetComponent<RectTransform>());

            Image backdrop = CreateImage("FullScreenBlackBackdrop", dialoguePanel.transform, new Color(0.005f, 0.006f, 0.01f, 1f));
            StretchToFill(backdrop.rectTransform);

            Image dialogueBackgroundImage = CreateBackgroundImage("DialogueBackgroundImage", dialoguePanel.transform, dialogueBackgroundSprite, AspectRatioFitter.AspectMode.FitInParent);
            Transform textAreaParent = dialogueBackgroundImage != null ? dialogueBackgroundImage.transform : dialoguePanel.transform;

            GameObject textAreaObject = new GameObject("DialogueTextArea", typeof(RectTransform));
            textAreaObject.transform.SetParent(textAreaParent, false);
            RectTransform textAreaRect = textAreaObject.GetComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0.34f, 0.075f);
            textAreaRect.anchorMax = new Vector2(0.925f, 0.315f);
            textAreaRect.pivot = new Vector2(0.5f, 0.5f);
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;

            speakerNameText = CreateText("SpeakerNameText", textAreaObject.transform, 28, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.28f, 1f));
            RectTransform speakerRect = speakerNameText.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0f, 0.70f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0.5f, 0.5f);
            speakerRect.offsetMin = new Vector2(30f, 20f);
            speakerRect.offsetMax = new Vector2(-30f, 20f);

            dialogueBodyText = CreateText("DialogueBodyText", textAreaObject.transform, 21, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 1f, 1f));
            dialogueBodyText.lineSpacing = 1.12f;
            RectTransform bodyRect = dialogueBodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0.36f);
            bodyRect.anchorMax = new Vector2(0.84f, 0.72f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.offsetMin = new Vector2(30f, 20f);
            bodyRect.offsetMax = new Vector2(-10f, 20f);

            dialogueContinueHintText = CreateText("ContinueHintText", textAreaObject.transform, 18, TextAnchor.MiddleRight, new Color(0.78f, 0.88f, 1f, 1f));
            dialogueContinueHintText.horizontalOverflow = HorizontalWrapMode.Overflow;
            dialogueContinueHintText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform hintRect = dialogueContinueHintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.58f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0.28f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.offsetMin = new Vector2(0f, 10f);
            hintRect.offsetMax = new Vector2(-30f, 0f);

            dialoguePanel.SetActive(false);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="objectName">to create or find GameObject name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns a created or found UI Image components. </returns>
        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="objectName">to create or find GameObject name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="sprite">to be displayed Sprite picture. </param>
/// <param name="aspectMode">aspectMode Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns a created or found UI Image components. </returns>
        private Image CreateBackgroundImage(string objectName, Transform parent, Sprite sprite, AspectRatioFitter.AspectMode aspectMode)
        {
            if (sprite == null)
            {
// Return when there is no picture null, letting the caller continue working with a solid color background.
                return null;
            }

            Image backgroundImage = CreateImage(objectName, parent, Color.white);
            backgroundImage.sprite = sprite;
            backgroundImage.preserveAspect = true;
            StretchToFill(backgroundImage.rectTransform);

            AspectRatioFitter aspectRatioFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = aspectMode;
// Use the original aspect ratio of the picture to fit the screen to prevent the plot background from being stretched and deformed.
            aspectRatioFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            return backgroundImage;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="objectName">to create or find GameObject name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="fontSize">fontSize Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="alignment">alignment Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns a created or found UI Text components. </returns>
        private static Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            Font pixelFont = Resources.Load<Font>(PixelFontPath);
            if (pixelFont != null)
            {
// Use pixel fonts to bring drama UI Same as the pixel style level.
                text.font = pixelFont;
            }

            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.25f;
            return text;
        }
        /// <summary>
/// Organize the input text format to avoid UI There are extra spaces or line breaks when displaying.
        /// </summary>
/// <param name="value">The new parameter value to set. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string NormalizeStoryText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Replace(OldSpeakerName, CurrentSpeakerName);
        }
        /// <summary>
/// Bundle UI RectTransform Stretch to parent object extent, for use with backgrounds or full-screen panels.
        /// </summary>
/// <param name="rectTransform">rectTransform Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void StretchToFill(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
