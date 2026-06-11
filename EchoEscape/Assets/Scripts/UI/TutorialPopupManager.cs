using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Shows and hides the dark tutorial popup UI used by question mark triggers.
    /// </summary>
    /// <remarks>
    /// Attach this script to the tutorial popup Canvas object.
    /// TutorialPopupTrigger calls ShowPopup when the player enters a question mark trigger.
    /// The manager updates the title/body Text components, pauses the game if configured, and closes on C.
    /// </remarks>
    public class TutorialPopupManager : MonoBehaviour
    {
        private const string DecorRootName = "StyledPopupDecorRoot";
        private const string CloseHintObjectName = "CloseHintText";
        private const string ForestBackdropResourcePath = "Dark Forest 1.0/Backgrounds/Back Forest";
        private const string PopupFontResourcePath = "BrackeysPlatformer/Fonts/PixelOperator8-Bold";
        private const string KeyHighlightColor = "#FFD85A";
        private const string TreasureChestTitle = "Treasure Chest";

        private static readonly Color PopupBackgroundColor = new Color(0.01f, 0.035f, 0.03f, 0.96f);
        private static readonly Color PopupOverlayColor = new Color(0f, 0.015f, 0.01f, 0.62f);
        private static readonly Color FrameGoldColor = new Color(0.94f, 0.72f, 0.28f, 1f);
        private static readonly Color FrameGreenColor = new Color(0.38f, 0.58f, 0.22f, 1f);
        private static readonly Color GemGreenColor = new Color(0.1f, 0.9f, 0.56f, 1f);
        private static readonly Color BodyTextColor = new Color(0.96f, 0.97f, 1f, 1f);
        private static readonly Color CloseHintColor = new Color(0.8f, 0.9f, 1f, 1f);

        [Tooltip("The panel that contains the tutorial popup UI.")]
        /// <summary>
        /// Root panel GameObject that is enabled while a tutorial popup is visible.
        /// </summary>
        public GameObject popupPanel;

        [Tooltip("The title text shown at the top of the popup.")]
        /// <summary>
        /// UI Text component used for the popup title.
        /// </summary>
        public Text titleText;

        [Tooltip("The body text shown inside the popup.")]
        /// <summary>
        /// UI Text component used for the popup message body.
        /// </summary>
        public Text bodyText;

        [Tooltip("If true, the game pauses while the popup is open.")]
        /// <summary>
        /// If true, Time.timeScale is set to zero while the popup is open.
        /// </summary>
        public bool pauseGameWhenOpen = true;

        private bool popupOpen = false;
        private float previousTimeScale = 1.0f;
        private bool hasPausedTime = false;

        /// <summary>
        /// Unity event method called when this object first becomes active.
        /// </summary>
        /// <remarks>
        /// Ensures the popup starts hidden before the player reaches any question mark trigger.
        /// </remarks>
        private void Awake()
        {
            ApplyPopupVisualStyle();
            ClosePopupWithoutTimeChange();
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Listens for C while a popup is open.
        /// </remarks>
        private void Update()
        {
            RestoreTimeScaleIfPopupWasHidden();

            if (popupOpen && Input.GetKeyDown(KeyCode.C))
            {
                ClosePopup();
            }
        }

        /// <summary>
        /// Shows a tutorial popup with the supplied title and message text.
        /// </summary>
        /// <param name="popupTitle">Title displayed at the top of the popup.</param>
        /// <param name="popupMessage">Body message displayed inside the popup.</param>
        public void ShowPopup(string popupTitle, string popupMessage)
        {
            if (popupPanel == null)
            {
                Debug.LogWarning("Tutorial popup panel is missing.");
                return;
            }

            ApplyPopupVisualStyle();

            string resolvedMessage = HighlightInputKeys(GetResolvedPopupMessage(popupTitle, popupMessage));

            if (titleText != null)
            {
                titleText.text = popupTitle;
            }

            if (bodyText != null)
            {
                bodyText.supportRichText = true;
                bodyText.text = resolvedMessage;
            }

            popupPanel.SetActive(true);

            if (!popupOpen)
            {
                popupOpen = true;

                if (ShouldPauseForPopup(popupTitle) && popupPanel.activeInHierarchy && !hasPausedTime)
                {
                    previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1.0f;
                    Time.timeScale = 0.0f;
                    hasPausedTime = true;
                }
            }
        }

        /// <summary>
        /// Description:
        /// Resolves popup copy that should stay consistent across rebuilt or open scenes.
        /// Inputs:
        /// popupTitle - popup title text
        /// popupMessage - scene-authored popup message
        /// Returns:
        /// string - final popup message text
        /// </summary>
        private static string GetResolvedPopupMessage(string popupTitle, string popupMessage)
        {
            if (popupTitle == "Gravity Flip")
            {
                return "Press Up Arrow to flip gravity upward.\nPress Down Arrow to flip back.";
            }

            return popupMessage;
        }

        /// <summary>
        /// Description:
        /// Checks whether this popup should pause gameplay while visible.
        /// Inputs:
        /// popupTitle - popup title text
        /// Returns:
        /// bool - true if opening the popup should pause time
        /// </summary>
        private bool ShouldPauseForPopup(string popupTitle)
        {
            return pauseGameWhenOpen && popupTitle != TreasureChestTitle;
        }

        /// <summary>
        /// Description:
        /// Highlights input keys so tutorial instructions are easier to scan.
        /// Inputs:
        /// message - plain tutorial message
        /// Returns:
        /// string - message with rich text key highlights
        /// </summary>
        private static string HighlightInputKeys(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            string highlightedMessage = message;
            highlightedMessage = highlightedMessage.Replace("Left/Right Arrow", FormatKey("Left / Right"));
            highlightedMessage = highlightedMessage.Replace("Up Arrow", FormatKey("Up"));
            highlightedMessage = highlightedMessage.Replace("Down Arrow", FormatKey("Down"));
            highlightedMessage = highlightedMessage.Replace("Space", FormatKey("Space"));
            highlightedMessage = highlightedMessage.Replace("A/D", FormatKey("A / D"));
            highlightedMessage = HighlightSingleKey(highlightedMessage, "Q");
            highlightedMessage = HighlightSingleKey(highlightedMessage, "E");
            highlightedMessage = HighlightSingleKey(highlightedMessage, "F");
            highlightedMessage = HighlightSingleKey(highlightedMessage, "J");
            highlightedMessage = HighlightSingleKey(highlightedMessage, "C");
            return highlightedMessage;
        }

        /// <summary>
        /// Description:
        /// Highlights one standalone key token without changing words like Echo.
        /// Inputs:
        /// message - tutorial message
        /// keyName - single key name
        /// Returns:
        /// string - message with the key highlighted
        /// </summary>
        private static string HighlightSingleKey(string message, string keyName)
        {
            return Regex.Replace(message, $@"\b{Regex.Escape(keyName)}\b", FormatKey(keyName));
        }

        /// <summary>
        /// Description:
        /// Creates a rich text keycap label.
        /// Inputs:
        /// keyName - input key name
        /// Returns:
        /// string - formatted keycap text
        /// </summary>
        private static string FormatKey(string keyName)
        {
            return $"<color={KeyHighlightColor}><b>[{keyName}]</b></color>";
        }

        /// <summary>
        /// Closes the current tutorial popup and restores gameplay time if it was paused.
        /// </summary>
        public void ClosePopup()
        {
            ClosePopupWithoutTimeChange();
            RestoreTimeScaleIfNeeded();
        }

        /// <summary>
        /// Hides the popup panel without changing Time.timeScale.
        /// </summary>
        /// <remarks>
        /// Used during Awake and by ClosePopup before time scale is restored.
        /// </remarks>
        private void ClosePopupWithoutTimeChange()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            popupOpen = false;
        }

        /// <summary>
        /// Description:
        /// Restores gameplay if a popup panel was hidden while it still owned the pause.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void RestoreTimeScaleIfPopupWasHidden()
        {
            if (!hasPausedTime)
            {
                return;
            }

            if (!popupOpen || popupPanel == null || !popupPanel.activeInHierarchy)
            {
                popupOpen = false;
                RestoreTimeScaleIfNeeded();
            }
        }

        /// <summary>
        /// Description:
        /// Called when the popup manager is disabled.
        /// It restores Time.timeScale if the popup had paused the game.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void OnDisable()
        {
            RestoreTimeScaleIfNeeded();
        }

        /// <summary>
        /// Description:
        /// Called when the popup manager is destroyed.
        /// It restores Time.timeScale as a safety cleanup.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void OnDestroy()
        {
            RestoreTimeScaleIfNeeded();
        }

        /// <summary>
        /// Description:
        /// Restores gameplay time if this popup paused it.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void RestoreTimeScaleIfNeeded()
        {
            if (!pauseGameWhenOpen || !hasPausedTime)
            {
                return;
            }

            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1.0f;
            previousTimeScale = 1.0f;
            hasPausedTime = false;
        }

        /// <summary>
        /// Description:
        /// Applies the forest fantasy frame style to the popup UI.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void ApplyPopupVisualStyle()
        {
            if (popupPanel == null)
            {
                return;
            }

            RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(740f, 380f);
            }

            Image panelImage = popupPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = PopupBackgroundColor;
            }

            EnsurePopupDecoration();
            StyleTitleText();
            StyleBodyText();
            StyleCloseHintText();
        }

        /// <summary>
        /// Description:
        /// Creates decorative frame objects if the popup has not been styled yet.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void EnsurePopupDecoration()
        {
            if (popupPanel.transform.Find(DecorRootName) != null)
            {
                return;
            }

            GameObject decorRootObject = new GameObject(DecorRootName);
            decorRootObject.transform.SetParent(popupPanel.transform, false);
            RectTransform decorRoot = decorRootObject.AddComponent<RectTransform>();
            decorRoot.anchorMin = Vector2.zero;
            decorRoot.anchorMax = Vector2.one;
            decorRoot.offsetMin = Vector2.zero;
            decorRoot.offsetMax = Vector2.zero;
            decorRoot.SetAsFirstSibling();

            CreateStretchDecorImage("ForestBackdrop", decorRoot, LoadForestBackdropSprite(), new Color(0.45f, 0.58f, 0.42f, 0.34f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateStretchDecorImage("DarkForestOverlay", decorRoot, null, PopupOverlayColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateDecorRect("TopOuterFrame", decorRoot, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(670f, 6f), FrameGoldColor);
            CreateDecorRect("TopInnerFrame", decorRoot, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(642f, 4f), FrameGreenColor);
            CreateDecorRect("BottomOuterFrame", decorRoot, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(670f, 6f), FrameGoldColor);
            CreateDecorRect("BottomInnerFrame", decorRoot, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(642f, 4f), FrameGreenColor);
            CreateDecorRect("LeftOuterFrame", decorRoot, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(6f, 326f), FrameGoldColor);
            CreateDecorRect("LeftInnerFrame", decorRoot, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(4f, 302f), FrameGreenColor);
            CreateDecorRect("RightOuterFrame", decorRoot, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(6f, 326f), FrameGoldColor);
            CreateDecorRect("RightInnerFrame", decorRoot, new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(4f, 302f), FrameGreenColor);

            CreateCornerOrnament("TopLeftCorner", decorRoot, new Vector2(0f, 1f), new Vector2(30f, -30f), 0.72f);
            CreateCornerOrnament("TopRightCorner", decorRoot, new Vector2(1f, 1f), new Vector2(-30f, -30f), 0.72f);
            CreateCornerOrnament("BottomLeftCorner", decorRoot, new Vector2(0f, 0f), new Vector2(30f, 30f), 0.72f);
            CreateCornerOrnament("BottomRightCorner", decorRoot, new Vector2(1f, 0f), new Vector2(-30f, 30f), 0.72f);

            CreateGem("TopCenterGem", decorRoot, new Vector2(0.5f, 1f), new Vector2(0f, -26f), 28f);
            CreateGem("BottomCenterGem", decorRoot, new Vector2(0.5f, 0f), new Vector2(0f, 26f), 28f);
            CreateDecorRect("TitleDividerLeft", decorRoot, new Vector2(0f, 1f), new Vector2(150f, -102f), new Vector2(130f, 4f), FrameGoldColor);
            CreateDecorRect("TitleDividerRight", decorRoot, new Vector2(0f, 1f), new Vector2(350f, -102f), new Vector2(130f, 4f), FrameGoldColor);
            CreateGem("TitleDividerGem", decorRoot, new Vector2(0f, 1f), new Vector2(250f, -102f), 18f);
            CreateVineCluster("TopLeftVines", decorRoot, new Vector2(0f, 1f), new Vector2(64f, -54f), false, 0.72f);
            CreateVineCluster("TopRightVines", decorRoot, new Vector2(1f, 1f), new Vector2(-64f, -54f), true, 0.72f);
            CreateVineCluster("BottomLeftVines", decorRoot, new Vector2(0f, 0f), new Vector2(70f, 44f), false, 0.72f);
            CreateVineCluster("BottomRightVines", decorRoot, new Vector2(1f, 0f), new Vector2(-70f, 44f), true, 0.72f);
        }

        /// <summary>
        /// Description:
        /// Styles the title text to match the fantasy introduction panel.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void StyleTitleText()
        {
            if (titleText == null)
            {
                return;
            }

            titleText.color = FrameGoldColor;
            ApplyPopupFont(titleText);
            titleText.supportRichText = true;
            titleText.fontSize = 38;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.verticalOverflow = VerticalWrapMode.Overflow;
            titleText.resizeTextForBestFit = false;
            AddTextShadow(titleText, new Color(0f, 0f, 0f, 0.86f), new Vector2(2f, -2f));

            RectTransform rect = titleText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(68f, -46f);
                rect.sizeDelta = new Vector2(430f, 48f);
            }
        }

        /// <summary>
        /// Description:
        /// Styles the body text for large readable tutorial instructions.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void StyleBodyText()
        {
            if (bodyText == null)
            {
                return;
            }

            bodyText.color = BodyTextColor;
            ApplyPopupFont(bodyText);
            bodyText.supportRichText = true;
            bodyText.fontSize = 22;
            bodyText.fontStyle = FontStyle.Bold;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.lineSpacing = 1.12f;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.resizeTextForBestFit = false;
            AddTextShadow(bodyText, new Color(0f, 0f, 0f, 0.86f), new Vector2(2f, -2f));

            RectTransform rect = bodyText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(70f, -142f);
                rect.sizeDelta = new Vector2(600f, 156f);
            }
        }

        /// <summary>
        /// Description:
        /// Styles the close hint text in the lower-right corner.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void StyleCloseHintText()
        {
            Text closeHintText = FindPopupText(CloseHintObjectName);
            if (closeHintText == null)
            {
                return;
            }

            closeHintText.gameObject.SetActive(true);
            closeHintText.transform.SetAsLastSibling();
            closeHintText.supportRichText = true;
            closeHintText.text = HighlightInputKeys("Press C to close.");
            closeHintText.color = CloseHintColor;
            ApplyPopupFont(closeHintText);
            closeHintText.fontSize = 22;
            closeHintText.fontStyle = FontStyle.Bold;
            closeHintText.alignment = TextAnchor.MiddleRight;
            closeHintText.horizontalOverflow = HorizontalWrapMode.Overflow;
            closeHintText.verticalOverflow = VerticalWrapMode.Overflow;
            closeHintText.resizeTextForBestFit = false;
            AddTextShadow(closeHintText, new Color(0.05f, 0.15f, 0.36f, 0.95f), new Vector2(2f, -2f));

            RectTransform rect = closeHintText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-56f, 38f);
                rect.sizeDelta = new Vector2(280f, 32f);
            }
        }

        /// <summary>
        /// Description:
        /// Applies the shared popup pixel font when the project font is available.
        /// Inputs:
        /// text - text object to style
        /// Returns:
        /// void (no return)
        /// </summary>
        private static void ApplyPopupFont(Text text)
        {
            Font popupFont = Resources.Load<Font>(PopupFontResourcePath);
            if (popupFont != null)
            {
                text.font = popupFont;
            }
        }

        /// <summary>
        /// Description:
        /// Adds a shadow component to a Text object if one is not already present.
        /// Inputs:
        /// text - text object to style
        /// color - shadow color
        /// distance - shadow offset
        /// Returns:
        /// void (no return)
        /// </summary>
        private static void AddTextShadow(Text text, Color color, Vector2 distance)
        {
            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        /// <summary>
        /// Description:
        /// Finds a child Text under the popup panel by GameObject name.
        /// Inputs:
        /// objectName - child object name to find
        /// Returns:
        /// Text - found text component, or null
        /// </summary>
        private Text FindPopupText(string objectName)
        {
            Text[] texts = popupPanel.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Description:
        /// Loads a forest background as a UI sprite.
        /// Inputs:
        /// none
        /// Returns:
        /// Sprite - loaded or generated forest sprite, or null
        /// </summary>
        private static Sprite LoadForestBackdropSprite()
        {
            Texture2D texture = Resources.Load<Texture2D>(ForestBackdropResourcePath);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Description:
        /// Creates a stretched decorative Image.
        /// Inputs:
        /// name - object name
        /// parent - parent transform
        /// sprite - optional image sprite
        /// color - image color
        /// anchorMin - lower anchor
        /// anchorMax - upper anchor
        /// offsetMin - lower offset
        /// offsetMax - upper offset
        /// Returns:
        /// Image - created UI image
        /// </summary>
        private static Image CreateStretchDecorImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return image;
        }

        /// <summary>
        /// Description:
        /// Creates one rectangular decorative UI element.
        /// Inputs:
        /// name - object name
        /// parent - parent transform
        /// anchor - anchor point
        /// anchoredPosition - UI position
        /// size - UI size
        /// color - image color
        /// Returns:
        /// Image - created image
        /// </summary>
        private static Image CreateDecorRect(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject rectObject = new GameObject(name);
            rectObject.transform.SetParent(parent, false);

            Image image = rectObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return image;
        }

        /// <summary>
        /// Description:
        /// Creates a square corner ornament.
        /// Inputs:
        /// name - object name
        /// parent - parent transform
        /// anchor - anchor point
        /// anchoredPosition - UI position
        /// Returns:
        /// void (no return)
        /// </summary>
        private static void CreateCornerOrnament(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float scale)
        {
            CreateDecorRect(name + "_GoldSquare", parent, anchor, anchoredPosition, new Vector2(46f, 46f) * scale, FrameGoldColor);
            CreateDecorRect(name + "_DarkInset", parent, anchor, anchoredPosition, new Vector2(30f, 30f) * scale, PopupBackgroundColor);
            Image center = CreateDecorRect(name + "_GreenInset", parent, anchor, anchoredPosition, new Vector2(16f, 16f) * scale, FrameGreenColor);
            center.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>
        /// Description:
        /// Creates a green gem decoration.
        /// Inputs:
        /// name - object name
        /// parent - parent transform
        /// anchor - anchor point
        /// anchoredPosition - UI position
        /// size - gem size
        /// Returns:
        /// void (no return)
        /// </summary>
        private static void CreateGem(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size)
        {
            Image outer = CreateDecorRect(name + "_Outer", parent, anchor, anchoredPosition, new Vector2(size, size), FrameGoldColor);
            outer.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image inner = CreateDecorRect(name + "_Inner", parent, anchor, anchoredPosition, new Vector2(size * 0.56f, size * 0.56f), GemGreenColor);
            inner.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>
        /// Description:
        /// Creates simple vine-like green accents near the popup corners.
        /// Inputs:
        /// name - object name
        /// parent - parent transform
        /// anchor - anchor point
        /// anchoredPosition - base UI position
        /// mirror - true to mirror the cluster horizontally
        /// Returns:
        /// void (no return)
        /// </summary>
        private static void CreateVineCluster(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, bool mirror, float scale)
        {
            float direction = mirror ? -1f : 1f;
            CreateDecorRect(name + "_Stem", parent, anchor, anchoredPosition, new Vector2(8f, 56f) * scale, new Color(0.12f, 0.36f, 0.12f, 0.78f));
            CreateDecorRect(name + "_LeafA", parent, anchor, anchoredPosition + new Vector2(18f * direction, 16f) * scale, new Vector2(28f, 10f) * scale, new Color(0.18f, 0.48f, 0.16f, 0.82f));
            CreateDecorRect(name + "_LeafB", parent, anchor, anchoredPosition + new Vector2(28f * direction, -4f) * scale, new Vector2(24f, 10f) * scale, new Color(0.14f, 0.4f, 0.14f, 0.82f));
            CreateDecorRect(name + "_LeafC", parent, anchor, anchoredPosition + new Vector2(14f * direction, -24f) * scale, new Vector2(20f, 8f) * scale, new Color(0.2f, 0.5f, 0.18f, 0.82f));
        }
    }
}
