using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Shows a fullscreen story sequence before level gameplay begins.
    /// </summary>
    /// <remarks>
    /// Attach this script to a scene object and configure pages in the Inspector.
    /// It creates a simple fullscreen Canvas when UI references are not assigned.
    /// </remarks>
    public class LevelIntroSequence : MonoBehaviour
    {
        private const string PixelFontPath = "BrackeysPlatformer/Fonts/PixelOperator8-Bold";
        private const string DefaultContinueHint = "Press C to continue.";
        private const string EndingContinueHint = "Press C to return to Main Menu.";
        private const string EndingBodyText = "You escaped the Silent Forest.\nThank you for playing Echo Escape.";

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
        /// Description:
        /// Creates the intro UI if needed and shows the first story page.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Start()
        {
            if (pages == null || pages.Length == 0)
            {
                return;
            }

            EnsureIntroUi();
            ShowIntro();
        }

        /// <summary>
        /// Description:
        /// Advances the story when the continue key is pressed.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Update()
        {
            if (!introActive || !Input.GetKeyDown(continueKey))
            {
                return;
            }

            pageIndex++;
            if (activePages == null || pageIndex >= activePages.Length)
            {
                CompleteActiveSequence();
                return;
            }

            ApplyPageText();
        }

        /// <summary>
        /// Description:
        /// Restores gameplay time if this object is disabled during the intro.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void OnDisable()
        {
            RestoreGameplayTime();
        }

        /// <summary>
        /// Description:
        /// Shows the intro root and pauses gameplay if configured.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void ShowIntro()
        {
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
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedGameplay = true;
            }
        }

        /// <summary>
        /// Description:
        /// Shows the final Echo Wizard ending page and loads the target scene when complete.
        /// Inputs:
        /// targetSceneName - scene loaded after the player presses continue
        /// Returns:
        /// bool - true when the ending sequence was shown
        /// </summary>
        public bool ShowEndingSequence(string targetSceneName)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                return false;
            }

            EnsureIntroUi();

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
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedGameplay = true;
            }

            return true;
        }

        /// <summary>
        /// Description:
        /// Hides the intro and restores gameplay time.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
        /// Description:
        /// Finishes the active intro or ending sequence.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void CompleteActiveSequence()
        {
            bool shouldLoadScene = loadSceneOnComplete;
            string targetSceneName = sceneToLoadOnComplete;

            HideIntro();

            loadSceneOnComplete = false;
            sceneToLoadOnComplete = string.Empty;

            if (shouldLoadScene && !string.IsNullOrWhiteSpace(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }

        /// <summary>
        /// Description:
        /// Restores Time.timeScale if the intro paused gameplay.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void RestoreGameplayTime()
        {
            if (!pausedGameplay)
            {
                return;
            }

            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            previousTimeScale = 1f;
            pausedGameplay = false;
        }

        /// <summary>
        /// Description:
        /// Applies the current page text to the UI.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void ApplyPageText()
        {
            if (activePages == null || activePages.Length == 0)
            {
                return;
            }

            bool isDialoguePage = pageIndex >= activeDialogueStartPageIndex;
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
                storyText.text = currentPage;
            }

            if (!isDialoguePage && continueHintText != null)
            {
                continueHintText.text = activeContinueHint;
            }

            if (isDialoguePage && speakerNameText != null)
            {
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
        /// Description:
        /// Creates a fullscreen story Canvas when no UI has been assigned.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void EnsureIntroUi()
        {
            if (introRoot != null && storyText != null && continueHintText != null)
            {
                return;
            }

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
        /// Description:
        /// Creates the centered forest introduction layout used by the first pages.
        /// Inputs:
        /// parent - parent transform
        /// Returns:
        /// void (no return)
        /// </summary>
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
        /// Description:
        /// Creates the Echo Wizard dialogue layout with text inside the image's bottom dialogue box.
        /// Inputs:
        /// parent - parent transform
        /// Returns:
        /// void (no return)
        /// </summary>
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
        /// Description:
        /// Creates a UI image with the requested color.
        /// Inputs:
        /// objectName - GameObject name
        /// parent - parent transform
        /// color - image color
        /// Returns:
        /// Image - created image
        /// </summary>
        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>
        /// Description:
        /// Creates a fullscreen background image when a story background sprite is assigned.
        /// Inputs:
        /// parent - parent transform
        /// Returns:
        /// void (no return)
        /// </summary>
        private Image CreateBackgroundImage(string objectName, Transform parent, Sprite sprite, AspectRatioFitter.AspectMode aspectMode)
        {
            if (sprite == null)
            {
                return null;
            }

            Image backgroundImage = CreateImage(objectName, parent, Color.white);
            backgroundImage.sprite = sprite;
            backgroundImage.preserveAspect = true;
            StretchToFill(backgroundImage.rectTransform);

            AspectRatioFitter aspectRatioFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = aspectMode;
            aspectRatioFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            return backgroundImage;
        }

        /// <summary>
        /// Description:
        /// Creates a UI text object with the shared pixel font when available.
        /// Inputs:
        /// objectName - GameObject name
        /// parent - parent transform
        /// fontSize - text size
        /// alignment - text alignment
        /// color - text color
        /// Returns:
        /// Text - created text
        /// </summary>
        private static Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            Font pixelFont = Resources.Load<Font>(PixelFontPath);
            if (pixelFont != null)
            {
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
        /// Description:
        /// Replaces outdated speaker naming in serialized story content.
        /// Inputs:
        /// value - story or speaker text
        /// Returns:
        /// string - normalized display text
        /// </summary>
        private static string NormalizeStoryText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Replace(OldSpeakerName, CurrentSpeakerName);
        }

        /// <summary>
        /// Description:
        /// Stretches a RectTransform to fill its parent.
        /// Inputs:
        /// rectTransform - rect to stretch
        /// Returns:
        /// void (no return)
        /// </summary>
        private static void StretchToFill(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
