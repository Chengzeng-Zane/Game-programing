using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Tutorial Pop-up Window UI manager. It is responsible for question mark tutorials, plot pop-ups, button highlighting, pausing game time and closing pop-ups.
/// Gameplay logic: ShowPopup Show panels based on title and content and decide whether to pause Time. timeScale；ClosePopup Recovery time after closing; style functions create forest-style borders, backgrounds, shadows, and decorations.
/// Collaborates with: TutorialPopupTrigger and LevelStoryIntroPopup Call it; it only controls UI and pause, which do not directly control the player.
    /// </summary>
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
        public GameObject popupPanel;

        [Tooltip("The title text shown at the top of the popup.")]
        public Text titleText;

        [Tooltip("The body text shown inside the popup.")]
        public Text bodyText;

        [Tooltip("If true, the game pauses while the popup is open.")]
        public bool pauseGameWhenOpen = true;

        private bool popupOpen = false;
        private float previousTimeScale = 1.0f;
        private bool hasPausedTime = false;
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            ApplyPopupVisualStyle();
            ClosePopupWithoutTimeChange();
        }
        /// <summary>
/// Unity Called every frame. This handles input, timers, UI Real-time refresh of state or non-physics.
        /// </summary>
        private void Update()
        {
            RestoreTimeScaleIfPopupWasHidden();

            if (popupOpen && Input.GetKeyDown(KeyCode.C))
            {
                ClosePopup();
            }
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
/// <param name="popupTitle">Pop-up window title, used to determine whether to display text and pause the game. </param>
/// <param name="popupMessage">The text content of the pop-up window. </param>
        public void ShowPopup(string popupTitle, string popupMessage)
        {
            if (popupPanel == null)
            {
// When the pop-up panel is missing, it only warns and does not pause the game to prevent players from getting stuck.
                Debug.LogWarning("Tutorial popup panel is missing.");
                return;
            }

            ApplyPopupVisualStyle();

// First replace the special tutorial text according to the title, and then highlight the button text.
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
// The tutorial pop-up window pauses the game; the chest prompt does not pause to avoid interrupting the rhythm of unboxing feedback.
                    previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1.0f;
                    Time.timeScale = 0.0f;
                    hasPausedTime = true;
                }
            }
        }
        /// <summary>
/// The final displayed text is determined based on the pop-up window title. Gravity Flip The tutorial will force the display of accurate key instructions.
        /// </summary>
/// <param name="popupTitle">Pop-up window title, used to determine whether to display text and pause the game. </param>
/// <param name="popupMessage">The text content of the pop-up window. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string GetResolvedPopupMessage(string popupTitle, string popupMessage)
        {
            if (popupTitle == "Gravity Flip")
            {
                return "Press Up Arrow to flip gravity upward.\nPress Down Arrow to flip back.";
            }

            return popupMessage;
        }
        /// <summary>
/// Determine whether a certain process should be executed based on the current game state.
        /// </summary>
/// <param name="popupTitle">Pop-up window title, used to determine whether to display text and pause the game. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool ShouldPauseForPopup(string popupTitle)
        {
            return pauseGameWhenOpen && popupTitle != TreasureChestTitle;
        }
        /// <summary>
/// Replace the key words in the tutorial text with rich text highlighting format so players can see which key to press at a glance.
        /// </summary>
/// <param name="message">to be displayed to HUD Or the text written in the log. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string HighlightInputKeys(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            string highlightedMessage = message;
// Multi-character combinations are replaced first to avoid individual replacement later. Q/E/F/J/C Removed by mistake.
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
/// Highlight individual key names. Use regular word boundaries to avoid highlighting letters in common words.
        /// </summary>
/// <param name="message">to be displayed to HUD Or the text written in the log. </param>
/// <param name="keyName">keyName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string HighlightSingleKey(string message, string keyName)
        {
            return Regex.Replace(message, $@"\b{Regex.Escape(keyName)}\b", FormatKey(keyName));
        }
        /// <summary>
/// Organize the data into a format suitable for players to read or UI The text displayed.
        /// </summary>
/// <param name="keyName">keyName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string FormatKey(string keyName)
        {
            return $"<color={KeyHighlightColor}><b>[{keyName}]</b></color>";
        }
        /// <summary>
/// Close a door, panel or passage to restore it to a blocked or hidden state.
        /// </summary>
        public void ClosePopup()
        {
            ClosePopupWithoutTimeChange();
            RestoreTimeScaleIfNeeded();
        }
        /// <summary>
/// Close a door, panel or passage to restore it to a blocked or hidden state.
        /// </summary>
        private void ClosePopupWithoutTimeChange()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            popupOpen = false;
        }
        /// <summary>
/// If the pop-up window is hidden by other logic but the time is still paused, it will be automatically restored. Time. timeScale。
        /// </summary>
        private void RestoreTimeScaleIfPopupWasHidden()
        {
            if (!hasPausedTime)
            {
                return;
            }

            if (!popupOpen || popupPanel == null || !popupPanel.activeInHierarchy)
            {
// The game cannot remain paused when the panel is no longer visible.
                popupOpen = false;
                RestoreTimeScaleIfNeeded();
            }
        }
        /// <summary>
/// Called when the object is disabled. Here usually recovery time scales or cleans UI status to avoid pause residue.
        /// </summary>
        private void OnDisable()
        {
            RestoreTimeScaleIfNeeded();
        }
        /// <summary>
/// Called when the object is destroyed. Here we usually restore the global state or clear references to avoid residual effects after cutting the scene.
        /// </summary>
        private void OnDestroy()
        {
            RestoreTimeScaleIfNeeded();
        }
        /// <summary>
/// Restore the previous pop-up window Time. timeScale. It will be called when closing the pop-up window, disabling the object or destroying the object.
        /// </summary>
        private void RestoreTimeScaleIfNeeded()
        {
            if (!pauseGameWhenOpen || !hasPausedTime)
            {
                return;
            }

// If the previous timeScale abnormal or for 0, it will return to normal speed 1。
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1.0f;
            previousTimeScale = 1.0f;
            hasPausedTime = false;
        }
        /// <summary>
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
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
/// Make sure the pop-up window decoration layer has been created. It will add forest background, gold border, gems and vine decoration.
        /// </summary>
        private void EnsurePopupDecoration()
        {
            if (popupPanel.transform.Find(DecorRootName) != null)
            {
// The decoration is only created once to avoid overlaying a layer of borders every time the pop-up window appears.
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
/// Set the pop-up title text style, including font, color, shadow and position.
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
/// Set the text style of the pop-up window body, including rich text, line wrapping, font size and shadow.
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
/// Set the closing prompt text in the lower right corner to tell the player to press C Close the pop-up window.
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
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
/// <param name="text">text Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void ApplyPopupFont(Text text)
        {
            Font popupFont = Resources.Load<Font>(PopupFontResourcePath);
            if (popupFont != null)
            {
                text.font = popupFont;
            }
        }
        /// <summary>
/// Give UI Shadows are added to the text to make the light text stand out more clearly against the dark forest background.
        /// </summary>
/// <param name="text">text Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="distance">distance Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
/// Find the required components in the scene object or sub-object for subsequent logic use.
        /// </summary>
/// <param name="objectName">to create or find GameObject name. </param>
/// <returns>Returns a created or found UI Text components. </returns>
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
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <returns>Returns the loaded or generated Sprite; May be returned when the resource does not exist null。</returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="sprite">to be displayed Sprite picture. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="anchorMin">anchorMin Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchorMax">anchorMax Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="offsetMin">offsetMin Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="offsetMax">offsetMax Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns a created or found UI Image components. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns a created or found UI Image components. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="scale">scale Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void CreateCornerOrnament(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float scale)
        {
            CreateDecorRect(name + "_GoldSquare", parent, anchor, anchoredPosition, new Vector2(46f, 46f) * scale, FrameGoldColor);
            CreateDecorRect(name + "_DarkInset", parent, anchor, anchoredPosition, new Vector2(30f, 30f) * scale, PopupBackgroundColor);
            Image center = CreateDecorRect(name + "_GreenInset", parent, anchor, anchoredPosition, new Vector2(16f, 16f) * scale, FrameGreenColor);
            center.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void CreateGem(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size)
        {
            Image outer = CreateDecorRect(name + "_Outer", parent, anchor, anchoredPosition, new Vector2(size, size), FrameGoldColor);
            outer.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image inner = CreateDecorRect(name + "_Inner", parent, anchor, anchoredPosition, new Vector2(size * 0.56f, size * 0.56f), GemGreenColor);
            inner.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="mirror">mirror Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="scale">scale Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
