using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：关卡剧情介绍和结尾流程控制器。它负责每关开始时的故事介绍、第三关结束语，以及死亡重生后跳过已看过介绍。
    /// 玩法逻辑：进入关卡时显示多页剧情并暂停游戏；玩家关闭后恢复时间；死亡重载时通过静态记录跳过同一关 intro；第三关通关时可以先等 loot 反馈显示完，再播放巫师结束语。
    /// 协作关系：GoalZone 调用结尾流程；TutorialPopupManager/运行时 UI 负责显示；Time.timeScale 控制游戏暂停。
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
        /// Unity 在第一帧前调用。这里通常连接场景对象，启动初始 UI、教程或关卡流程。
        /// </summary>
        private void Start()
        {
            if (pages == null || pages.Length == 0)
            {
                // 没配置故事页时不显示 intro，避免空面板挡住游戏。
                return;
            }

            if (ConsumeSkipIntroForCurrentScene())
            {
                // 死亡重载当前关时会设置跳过标记；这样玩家不会每死一次都重新看剧情。
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
        /// 记录某个场景下一次加载时要跳过开场介绍。死亡重载前由 GameManager/HazardZone 调用。
        /// </summary>
        /// <param name="sceneName">目标场景名称，用于检查 Build Settings 或加载下一关。</param>
        public static void SkipNextIntroForScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                scenesSkippingNextIntro.Add(sceneName);
            }
        }
        /// <summary>
        /// 检查当前场景是否有“跳过 intro”标记；有就消费掉，保证只跳过下一次。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private static bool ConsumeSkipIntroForCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName) || !scenesSkippingNextIntro.Contains(sceneName))
            {
                return false;
            }

            // 移除标记表示只跳过一次，玩家从主菜单重新进关仍会看到故事。
            scenesSkippingNextIntro.Remove(sceneName);
            return true;
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
        /// </summary>
        private void Update()
        {
            if (!introActive || !Input.GetKeyDown(continueKey))
            {
                return;
            }

            // C 键翻到下一页；超过最后一页时结束当前 intro/ending 流程。
            pageIndex++;
            if (activePages == null || pageIndex >= activePages.Length)
            {
                CompleteActiveSequence();
                return;
            }

            ApplyPageText();
        }
        /// <summary>
        /// 对象被禁用时调用。这里通常恢复时间缩放或清理 UI 状态，避免暂停残留。
        /// </summary>
        private void OnDisable()
        {
            RestoreGameplayTime();
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        private void ShowIntro()
        {
            // activePages 让同一个 UI 系统既能显示开场故事，也能显示第三关结尾。
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
                // 开场故事期间暂停游戏，玩家不能在读剧情时被敌人或重力影响。
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedGameplay = true;
            }
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        /// <param name="targetSceneName">targetSceneName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        public bool ShowEndingSequence(string targetSceneName)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                // 没有目标场景时不播放结尾，避免结束后不知道要切到哪里。
                return false;
            }

            EnsureIntroUi();

            // 结尾复用对话面板，只显示一页巫师结束语，并在结束后加载目标场景。
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
                // 结尾播放时也暂停游戏，避免玩家继续移动触发其他对象。
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                pausedGameplay = true;
            }

            return true;
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
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
        /// 完成当前开场或结尾序列。它会隐藏 UI、恢复时间，并在结尾流程需要时加载目标场景。
        /// </summary>
        private void CompleteActiveSequence()
        {
            // 先缓存是否需要切场景，因为 HideIntro 会清 UI 状态。
            bool shouldLoadScene = loadSceneOnComplete;
            string targetSceneName = sceneToLoadOnComplete;

            HideIntro();

            loadSceneOnComplete = false;
            sceneToLoadOnComplete = string.Empty;

            if (shouldLoadScene && !string.IsNullOrWhiteSpace(targetSceneName))
            {
                // 结尾流程结束后才加载主菜单，避免剧情显示一半被切走。
                SceneManager.LoadScene(targetSceneName);
            }
        }
        /// <summary>
        /// 恢复剧情开始前的 Time.timeScale。用于关闭 intro、禁用对象或防止暂停状态残留。
        /// </summary>
        private void RestoreGameplayTime()
        {
            if (!pausedGameplay)
            {
                return;
            }

            // 恢复暂停前的 timeScale；如果之前记录异常，就回到正常速度 1。
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            previousTimeScale = 1f;
            pausedGameplay = false;
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        private void ApplyPageText()
        {
            if (activePages == null || activePages.Length == 0)
            {
                return;
            }

            bool isDialoguePage = pageIndex >= activeDialogueStartPageIndex;
            // 前几页用全屏森林叙事，后几页切到巫师对话框。
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
                // 普通故事页显示在全屏中央。
                storyText.text = currentPage;
            }

            if (!isDialoguePage && continueHintText != null)
            {
                continueHintText.text = activeContinueHint;
            }

            if (isDialoguePage && speakerNameText != null)
            {
                // 对话页显示说话人，当前统一成 Echo Wizard。
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
        /// 确保开场/结尾 UI 存在。场景没手动配置 UI 时，会运行时创建 Canvas、背景和文本。
        /// </summary>
        private void EnsureIntroUi()
        {
            if (introRoot != null && storyText != null && continueHintText != null)
            {
                // 场景里已经有 UI 引用时复用，不重复创建 Canvas。
                return;
            }

            // 没手动搭 UI 的场景会运行时生成完整 intro Canvas，保证每关都能显示故事。
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
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
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
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
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
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="objectName">要创建或查找的 GameObject 名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建或找到的 UI Image 组件。</returns>
        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="objectName">要创建或查找的 GameObject 名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="sprite">要显示的 Sprite 图片。</param>
        /// <param name="aspectMode">aspectMode 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建或找到的 UI Image 组件。</returns>
        private Image CreateBackgroundImage(string objectName, Transform parent, Sprite sprite, AspectRatioFitter.AspectMode aspectMode)
        {
            if (sprite == null)
            {
                // 没配图片时返回 null，让调用方用纯色背景继续工作。
                return null;
            }

            Image backgroundImage = CreateImage(objectName, parent, Color.white);
            backgroundImage.sprite = sprite;
            backgroundImage.preserveAspect = true;
            StretchToFill(backgroundImage.rectTransform);

            AspectRatioFitter aspectRatioFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = aspectMode;
            // 用图片原始宽高比适配屏幕，避免剧情背景被拉伸变形。
            aspectRatioFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            return backgroundImage;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="objectName">要创建或查找的 GameObject 名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="fontSize">fontSize 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="alignment">alignment 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建或找到的 UI Text 组件。</returns>
        private static Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            Font pixelFont = Resources.Load<Font>(PixelFontPath);
            if (pixelFont != null)
            {
                // 使用像素字体让剧情 UI 和像素风关卡一致。
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
        /// 整理输入文字格式，避免 UI 显示时出现多余空格或换行问题。
        /// </summary>
        /// <param name="value">要设置的新参数值。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string NormalizeStoryText(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Replace(OldSpeakerName, CurrentSpeakerName);
        }
        /// <summary>
        /// 把 UI RectTransform 拉伸到父对象范围，用于背景或全屏面板。
        /// </summary>
        /// <param name="rectTransform">rectTransform 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void StretchToFill(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
