using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：教学弹窗 UI 管理器。它负责问号教程、剧情弹窗、按键高亮、暂停游戏时间和关闭弹窗。
    /// 玩法逻辑：ShowPopup 根据标题和内容显示面板，并决定是否暂停 Time.timeScale；ClosePopup 关闭后恢复时间；样式函数会创建森林风格边框、背景、阴影和装饰。
    /// 协作关系：TutorialPopupTrigger 和 LevelStoryIntroPopup 调用它；它只控制 UI 和暂停，不直接控制玩家。
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
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            ApplyPopupVisualStyle();
            ClosePopupWithoutTimeChange();
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
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
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        /// <param name="popupTitle">弹窗标题，用来决定显示文本和是否暂停游戏。</param>
        /// <param name="popupMessage">弹窗正文内容。</param>
        public void ShowPopup(string popupTitle, string popupMessage)
        {
            if (popupPanel == null)
            {
                // 弹窗面板缺失时只警告，不暂停游戏，避免玩家被卡住。
                Debug.LogWarning("Tutorial popup panel is missing.");
                return;
            }

            ApplyPopupVisualStyle();

            // 先根据标题替换特殊教程文本，再把按键文字加高亮。
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
                    // 教程弹窗暂停游戏；宝箱提示不暂停，避免打断开箱反馈节奏。
                    previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1.0f;
                    Time.timeScale = 0.0f;
                    hasPausedTime = true;
                }
            }
        }
        /// <summary>
        /// 根据弹窗标题决定最终显示文本。Gravity Flip 教程会强制显示准确按键说明。
        /// </summary>
        /// <param name="popupTitle">弹窗标题，用来决定显示文本和是否暂停游戏。</param>
        /// <param name="popupMessage">弹窗正文内容。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string GetResolvedPopupMessage(string popupTitle, string popupMessage)
        {
            if (popupTitle == "Gravity Flip")
            {
                return "Press Up Arrow to flip gravity upward.\nPress Down Arrow to flip back.";
            }

            return popupMessage;
        }
        /// <summary>
        /// 根据当前游戏状态判断是否应该执行某个流程。
        /// </summary>
        /// <param name="popupTitle">弹窗标题，用来决定显示文本和是否暂停游戏。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool ShouldPauseForPopup(string popupTitle)
        {
            return pauseGameWhenOpen && popupTitle != TreasureChestTitle;
        }
        /// <summary>
        /// 把教程文本里的按键词替换成富文本高亮格式，让玩家一眼看到要按哪个键。
        /// </summary>
        /// <param name="message">要显示到 HUD 或写入日志的文字。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string HighlightInputKeys(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            string highlightedMessage = message;
            // 多字符组合先替换，避免后面单独替换 Q/E/F/J/C 时误拆。
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
        /// 高亮单个按键名。使用正则单词边界，避免把普通单词里的字母也高亮。
        /// </summary>
        /// <param name="message">要显示到 HUD 或写入日志的文字。</param>
        /// <param name="keyName">keyName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string HighlightSingleKey(string message, string keyName)
        {
            return Regex.Replace(message, $@"\b{Regex.Escape(keyName)}\b", FormatKey(keyName));
        }
        /// <summary>
        /// 把数据整理成适合玩家阅读或 UI 显示的文字。
        /// </summary>
        /// <param name="keyName">keyName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string FormatKey(string keyName)
        {
            return $"<color={KeyHighlightColor}><b>[{keyName}]</b></color>";
        }
        /// <summary>
        /// 关闭门、面板或通路，恢复阻挡或隐藏状态。
        /// </summary>
        public void ClosePopup()
        {
            ClosePopupWithoutTimeChange();
            RestoreTimeScaleIfNeeded();
        }
        /// <summary>
        /// 关闭门、面板或通路，恢复阻挡或隐藏状态。
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
        /// 如果弹窗被其他逻辑隐藏，但时间还处于暂停状态，就自动恢复 Time.timeScale。
        /// </summary>
        private void RestoreTimeScaleIfPopupWasHidden()
        {
            if (!hasPausedTime)
            {
                return;
            }

            if (!popupOpen || popupPanel == null || !popupPanel.activeInHierarchy)
            {
                // 面板不再可见时，不能继续让游戏保持暂停。
                popupOpen = false;
                RestoreTimeScaleIfNeeded();
            }
        }
        /// <summary>
        /// 对象被禁用时调用。这里通常恢复时间缩放或清理 UI 状态，避免暂停残留。
        /// </summary>
        private void OnDisable()
        {
            RestoreTimeScaleIfNeeded();
        }
        /// <summary>
        /// 对象被销毁时调用。这里通常恢复全局状态或清除引用，避免切场景后残留影响。
        /// </summary>
        private void OnDestroy()
        {
            RestoreTimeScaleIfNeeded();
        }
        /// <summary>
        /// 恢复弹窗打开前的 Time.timeScale。关闭弹窗、禁用对象或销毁对象时都会调用。
        /// </summary>
        private void RestoreTimeScaleIfNeeded()
        {
            if (!pauseGameWhenOpen || !hasPausedTime)
            {
                return;
            }

            // 如果之前的 timeScale 异常或为 0，就恢复到正常速度 1。
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1.0f;
            previousTimeScale = 1.0f;
            hasPausedTime = false;
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
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
        /// 确保弹窗装饰层已经创建。它会添加森林背景、金色边框、宝石和藤蔓装饰。
        /// </summary>
        private void EnsurePopupDecoration()
        {
            if (popupPanel.transform.Find(DecorRootName) != null)
            {
                // 装饰只创建一次，避免每次弹窗都叠加一层边框。
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
        /// 设置弹窗标题文本样式，包括字体、颜色、阴影和位置。
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
        /// 设置弹窗正文文本样式，包括富文本、换行、字体大小和阴影。
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
        /// 设置右下角关闭提示文字，告诉玩家按 C 关闭弹窗。
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
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        /// <param name="text">text 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void ApplyPopupFont(Text text)
        {
            Font popupFont = Resources.Load<Font>(PopupFontResourcePath);
            if (popupFont != null)
            {
                text.font = popupFont;
            }
        }
        /// <summary>
        /// 给 UI 文本添加阴影，让浅色文字在暗色森林背景上更清楚。
        /// </summary>
        /// <param name="text">text 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="distance">distance 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
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
        /// 在场景对象或子物体中查找需要的组件，供后续逻辑使用。
        /// </summary>
        /// <param name="objectName">要创建或查找的 GameObject 名称。</param>
        /// <returns>返回创建或找到的 UI Text 组件。</returns>
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
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <returns>返回加载或生成的 Sprite；资源不存在时可能返回 null。</returns>
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
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="sprite">要显示的 Sprite 图片。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="anchorMin">anchorMin 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchorMax">anchorMax 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="offsetMin">offsetMin 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="offsetMax">offsetMax 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建或找到的 UI Image 组件。</returns>
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
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建或找到的 UI Image 组件。</returns>
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
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="scale">scale 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void CreateCornerOrnament(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float scale)
        {
            CreateDecorRect(name + "_GoldSquare", parent, anchor, anchoredPosition, new Vector2(46f, 46f) * scale, FrameGoldColor);
            CreateDecorRect(name + "_DarkInset", parent, anchor, anchoredPosition, new Vector2(30f, 30f) * scale, PopupBackgroundColor);
            Image center = CreateDecorRect(name + "_GreenInset", parent, anchor, anchoredPosition, new Vector2(16f, 16f) * scale, FrameGreenColor);
            center.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void CreateGem(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size)
        {
            Image outer = CreateDecorRect(name + "_Outer", parent, anchor, anchoredPosition, new Vector2(size, size), FrameGoldColor);
            outer.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image inner = CreateDecorRect(name + "_Inner", parent, anchor, anchoredPosition, new Vector2(size * 0.56f, size * 0.56f), GemGreenColor);
            inner.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="mirror">mirror 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="scale">scale 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
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
