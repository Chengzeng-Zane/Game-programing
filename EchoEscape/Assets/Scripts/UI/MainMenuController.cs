using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：主菜单控制器。它在运行时搭建主菜单场景，包括背景装饰、标题、开始按钮、设置面板、退出按钮和事件系统。
    /// 玩法逻辑：Start Game 会检查目标场景是否存在于 Build Settings，重置新一局宝箱领取状态，然后加载第一关。菜单音乐通过 BackgroundMusic 保持和关卡统一。
    /// 协作关系：SceneManager 负责场景切换；BackgroundMusic 负责音乐；EchoEscapeGameManager.ResetChestClaimsForNewRun 负责新游戏状态。
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        public string gameSceneName = string.Empty;

        private readonly Color buttonNormal = new Color(0.075f, 0.14f, 0.13f, 0.96f);
        private readonly Color buttonHover = new Color(0.16f, 0.29f, 0.22f, 1f);
        private readonly Color buttonPressed = new Color(0.94f, 0.67f, 0.22f, 1f);
        private readonly Color textLight = new Color(1f, 0.95f, 0.78f, 1f);
        private readonly Color textMuted = new Color(0.72f, 0.84f, 0.76f, 1f);
        private readonly Color panelDark = new Color(0.035f, 0.06f, 0.055f, 0.94f);
        private readonly Color gold = new Color(0.96f, 0.68f, 0.25f, 1f);
        private readonly Color runeGold = new Color(1f, 0.82f, 0.36f, 0.92f);

        private Font menuFont;
        private GameObject settingsRoot;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            // 主菜单完全运行时搭建；先加载像素字体，再创建相机、背景、Canvas 和音乐。
            menuFont = Resources.Load<Font>("BrackeysPlatformer/Fonts/PixelOperator8-Bold");

            EnsureCamera();
            BuildWorld();
            BuildCanvas();
            BackgroundMusic.EnsurePlaying();
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
        /// </summary>
        private void Update()
        {
            if (settingsRoot != null && settingsRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                // 设置面板打开时按 Esc 关闭，符合普通菜单操作习惯。
                HideSettings();
            }
        }
        /// <summary>
        /// 开始新游戏。它会确认第一关在 Build Settings 里，重置新一局宝箱领取记录，然后加载目标关卡。
        /// </summary>
        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName) || !SceneExistsInBuild(gameSceneName))
            {
                // 场景没配置好时只警告，不让按钮把游戏切到无效场景。
                Debug.LogWarning("Gameplay scene is not ready yet.");
                return;
            }

            // 新游戏要清空“本轮已拿过宝箱”的静态状态，否则重进游戏拿不到宝箱。
            EchoEscapeGameManager.ResetChestClaimsForNewRun();
            SceneManager.LoadScene(gameSceneName);
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        public void ShowSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(true);
            }
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
        /// </summary>
        public void HideSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(false);
            }
        }
        /// <summary>
        /// 关闭门、面板或通路，恢复阻挡或隐藏状态。
        /// </summary>
        public void CloseModal()
        {
            HideSettings();
        }
        /// <summary>
        /// 退出游戏。编辑器中停止 Play Mode，正式打包后调用 Application.Quit。
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested from Main Menu.");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        /// <summary>
        /// 确保主菜单有相机和 AudioListener。场景没手动放相机时会自动创建。
        /// </summary>
        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                // 主菜单场景允许空场景启动，脚本会补 Main Camera。
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(0.018f, 0.035f, 0.04f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            if (FindObjectOfType<AudioListener>() == null)
            {
                // 音乐和按钮音效需要 AudioListener 才能被听到。
                camera.gameObject.AddComponent<AudioListener>();
            }
        }
        /// <summary>
        /// 检查目标场景名是否存在于 Build Settings，避免 Start Game 加载不存在的场景。
        /// </summary>
        /// <param name="sceneName">目标场景名称，用于检查 Build Settings 或加载下一关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool SceneExistsInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                // Build Settings 里保存的是完整路径，这里取文件名和配置的场景名比较。
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneFileName == sceneName)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 组装一组运行时对象或 UI 元素，用来形成完整菜单、面板或视觉结构。
        /// </summary>
        private void BuildWorld()
        {
            GameObject worldRoot = new GameObject("Pixel Menu World");

            CreateTiledSprite("Deep Forest Backdrop", PixelArtLibrary.StoneTile, new Vector2(0f, 0.65f), new Vector2(24f, 9.4f), -8, worldRoot.transform, new Color(0.01f, 0.055f, 0.052f, 0.72f));
            CreateTiledSprite("Distant Forest Silhouette", PixelArtLibrary.StoneTile, new Vector2(-1.8f, -0.65f), new Vector2(24f, 4.9f), -7, worldRoot.transform, new Color(0.015f, 0.11f, 0.085f, 0.42f));
            CreateTiledSprite("Near Forest Silhouette", PixelArtLibrary.StoneTile, new Vector2(2.1f, -1.2f), new Vector2(24f, 3.8f), -6, worldRoot.transform, new Color(0.008f, 0.045f, 0.04f, 0.68f));

            CreateTiledSprite("Left Ancient Pillar", PixelArtLibrary.StoneTile, new Vector2(-6.6f, -0.2f), new Vector2(0.92f, 8.1f), -5, worldRoot.transform, new Color(0.08f, 0.16f, 0.11f, 0.72f));
            CreateTiledSprite("Right Ancient Pillar", PixelArtLibrary.StoneTile, new Vector2(6.6f, -0.2f), new Vector2(0.92f, 8.1f), -5, worldRoot.transform, new Color(0.08f, 0.16f, 0.11f, 0.72f));
            CreateTiledSprite("Low Ruin Ground Shadow", PixelArtLibrary.StoneTile, new Vector2(0f, -4.12f), new Vector2(22f, 1.05f), -4, worldRoot.transform, new Color(0.05f, 0.11f, 0.09f, 0.82f));

            CreateSprite("Central Ruin Mark Glow", PixelArtLibrary.DoorTile, new Vector2(0f, -0.9f), new Vector3(6.2f, 6.2f, 1f), -3, worldRoot.transform, new Color(0.24f, 0.9f, 0.68f, 0.12f));
            CreateSprite("Central Ruin Mark", PixelArtLibrary.DoorTile, new Vector2(0f, -0.9f), new Vector3(4.65f, 4.65f, 1f), -2, worldRoot.transform, new Color(0.82f, 0.74f, 0.48f, 0.2f));
        }
        /// <summary>
        /// 组装一组运行时对象或 UI 元素，用来形成完整菜单、面板或视觉结构。
        /// </summary>
        private void BuildCanvas()
        {
            EnsureEventSystem();

            Canvas canvas = CreateCanvas();
            Transform canvasTransform = canvas.transform;

            CreatePanel("Forest Vignette Top", canvasTransform, new Color(0f, 0f, 0f, 0.22f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(1320f, 68f));
            CreatePanel("Forest Vignette Bottom", canvasTransform, new Color(0f, 0f, 0f, 0.34f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(1320f, 76f));

            GameObject heroPanel = CreateFramedPanel("Main Menu Panel", canvasTransform, panelDark, new Vector2(0.5f, 0.52f), new Vector2(0f, 0f), new Vector2(560f, 500f), new Color(0.06f, 0.42f, 0.28f, 0.72f));
            CreateText("Small Kicker", heroPanel.transform, "FOREST VAULT", 18, runeGold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(460f, 36f));

            Text title = CreateText("Title", heroPanel.transform, "ECHO ESCAPE", 62, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(500f, 92f));
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);

            Text subtitle = CreateText("Subtitle", heroPanel.transform, "Record your echo. Secure the loot. Escape alive.", 22, textMuted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(470f, 46f));
            subtitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.72f);

            CreateDivider("Title Gold Divider", heroPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(390f, 4f), gold);

            CreateButton("Start Game Button", heroPanel.transform, "START GAME", new Vector2(0.5f, 1f), new Vector2(0f, -272f), StartGame, new Vector2(330f, 62f));
            CreateButton("Settings Button", heroPanel.transform, "SETTINGS", new Vector2(0.5f, 1f), new Vector2(0f, -350f), ShowSettings, new Vector2(330f, 58f));
            CreateButton("Quit Button", heroPanel.transform, "QUIT", new Vector2(0.5f, 1f), new Vector2(0f, -422f), QuitGame, new Vector2(330f, 58f));

            BuildSettingsPanel(canvasTransform);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <returns>返回创建好的 Canvas。</returns>
        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Main Menu Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }
        /// <summary>
        /// 组装一组运行时对象或 UI 元素，用来形成完整菜单、面板或视觉结构。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        private void BuildSettingsPanel(Transform parent)
        {
            settingsRoot = new GameObject("Settings Panel");
            settingsRoot.transform.SetParent(parent, false);

            Image blocker = settingsRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0.015f, 0.01f, 0.72f);

            RectTransform blockerRect = settingsRoot.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            GameObject panel = CreateFramedPanel("Settings Content Panel", settingsRoot.transform, new Color(0.035f, 0.075f, 0.065f, 0.98f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 470f), new Color(0.96f, 0.68f, 0.25f, 0.9f));
            CreateText("Settings Title", panel.transform, "CONTROLS", 40, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 70f));
            CreateDivider("Settings Divider", panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(470f, 3f), new Color(1f, 0.78f, 0.3f, 0.9f));

            Text controls = CreateText(
                "Controls List",
                panel.transform,
                "Move: A / D\n" +
                "Jump: Space\n" +
                "Record Echo: Q\n" +
                "Replay Echo: E\n" +
                "Open Chest: F\n" +
                "Attack: J\n" +
                "Gravity Flip: Up / Down",
                24,
                textMuted,
                TextAnchor.UpperLeft,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(440f, 230f));
            controls.lineSpacing = 1.2f;
            controls.verticalOverflow = VerticalWrapMode.Overflow;

            CreateButton("Back Button", panel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 48f), HideSettings, new Vector2(220f, 56f));
            settingsRoot.SetActive(false);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="label">label 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="action">action 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建好的 UI Button 组件。</returns>
        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            return CreateButton(name, parent, label, anchor, anchoredPosition, action, new Vector2(300f, 58f));
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="label">label 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="action">action 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建好的 UI Button 组件。</returns>
        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, Vector2 size)
        {
            GameObject buttonObject = CreatePanel(name, parent, buttonNormal, anchor, anchoredPosition, size);
            Image image = buttonObject.GetComponent<Image>();
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.94f, 0.66f, 0.2f, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(4f, -4f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = buttonNormal,
                highlightedColor = buttonHover,
                pressedColor = buttonPressed,
                selectedColor = buttonHover,
                disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            Text text = CreateText(name + " Text", buttonObject.transform, label, 26, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.86f);
            text.raycastTarget = false;
            return button;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="outlineColor">outlineColor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建或找到的 GameObject，方便调用方继续添加组件或设置位置。</returns>
        private GameObject CreateFramedPanel(string name, Transform parent, Color color, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color outlineColor)
        {
            GameObject panel = CreatePanel(name, parent, color, anchor, anchoredPosition, size);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(3f, -3f);

            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(8f, -8f);

            CreateCornerAccent(name + " Top Left Rune", panel.transform, new Vector2(0f, 1f), new Vector2(28f, -28f));
            CreateCornerAccent(name + " Top Right Rune", panel.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f));
            CreateCornerAccent(name + " Bottom Left Rune", panel.transform, new Vector2(0f, 0f), new Vector2(28f, 28f));
            CreateCornerAccent(name + " Bottom Right Rune", panel.transform, new Vector2(1f, 0f), new Vector2(-28f, 28f));

            return panel;
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
        private void CreateDivider(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject divider = CreatePanel(name, parent, color, anchor, anchoredPosition, size);
            Shadow shadow = divider.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void CreateCornerAccent(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition)
        {
            GameObject accent = CreatePanel(name, parent, runeGold, anchor, anchoredPosition, new Vector2(16f, 16f));
            RectTransform rect = accent.GetComponent<RectTransform>();
            rect.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建或找到的 GameObject，方便调用方继续添加组件或设置位置。</returns>
        private GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            Image image = panel.AddComponent<Image>();
            image.color = color;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return panel;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="value">要设置的新参数值。</param>
        /// <param name="fontSize">fontSize 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="alignment">alignment 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建或找到的 UI Text 组件。</returns>
        private Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return text;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="sprite">要显示的 Sprite 图片。</param>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="sortingOrder">sortingOrder 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回 SpriteRenderer 类型结果，供调用方继续判断或使用。</returns>
        private SpriteRenderer CreateTiledSprite(string name, Sprite sprite, Vector2 position, Vector2 size, int sortingOrder, Transform parent, Color color)
        {
            SpriteRenderer renderer = CreateSprite(name, sprite, position, Vector3.one, sortingOrder, parent, color);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            return renderer;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="sprite">要显示的 Sprite 图片。</param>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        /// <param name="scale">scale 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="sortingOrder">sortingOrder 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回 SpriteRenderer 类型结果，供调用方继续判断或使用。</returns>
        private SpriteRenderer CreateSprite(string name, Sprite sprite, Vector2 position, Vector3 scale, int sortingOrder, Transform parent, Color color)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.position = new Vector3(position.x, position.y, 0f);
            spriteObject.transform.localScale = scale;

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
        /// <summary>
        /// 确保场景有 EventSystem。没有它，按钮无法响应鼠标点击和键盘导航。
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                // 已有 EventSystem 时复用，避免多个 EventSystem 抢输入。
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
