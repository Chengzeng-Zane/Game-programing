using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Builds and controls the Echo Escape main menu at runtime.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Main Menu Controller object in the MainMenu scene.
    /// It creates menu background art, UI buttons, a settings panel, background music,
    /// and safely starts the gameplay scene when one is assigned in Build Settings.
    /// </remarks>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        /// <summary>
        /// Name of the gameplay scene loaded by Start Game.
        /// </summary>
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
        /// Unity event method called when the menu controller is created.
        /// </summary>
        /// <remarks>
        /// Builds the runtime camera, world decoration, UI canvas, and menu music.
        /// </remarks>
        private void Awake()
        {
            menuFont = Resources.Load<Font>("BrackeysPlatformer/Fonts/PixelOperator8-Bold");

            EnsureCamera();
            BuildWorld();
            BuildCanvas();
            StartMusic();
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Lets Escape close the settings panel.
        /// </remarks>
        private void Update()
        {
            if (settingsRoot != null && settingsRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                HideSettings();
            }
        }

        /// <summary>
        /// Attempts to load the configured gameplay scene.
        /// </summary>
        /// <remarks>
        /// Called by the Start Game button. If no valid gameplay scene is assigned, a warning is logged.
        /// </remarks>
        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName) || !SceneExistsInBuild(gameSceneName))
            {
                Debug.LogWarning("Gameplay scene is not ready yet.");
                return;
            }

            EchoEscapeGameManager.ResetChestClaimsForNewRun();
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Shows the settings panel.
        /// </summary>
        /// <remarks>
        /// Called by the Settings button.
        /// </remarks>
        public void ShowSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the settings panel.
        /// </summary>
        /// <remarks>
        /// Called by the Back button and by Escape in Update.
        /// </remarks>
        public void HideSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Closes the settings panel.
        /// </summary>
        /// <remarks>
        /// Kept for older button references.
        /// </remarks>
        public void CloseModal()
        {
            HideSettings();
        }

        /// <summary>
        /// Quits the application or stops Play Mode in the Unity Editor.
        /// </summary>
        /// <remarks>
        /// Called by the Quit button.
        /// </remarks>
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
        /// Ensures a Main Camera exists and configures it for the menu scene.
        /// </summary>
        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(0.018f, 0.035f, 0.04f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        /// <summary>
        /// Checks whether a scene name exists in Unity Build Settings.
        /// </summary>
        /// <param name="sceneName">Scene name without file extension.</param>
        /// <returns>True if the scene appears in Build Settings; otherwise false.</returns>
        private bool SceneExistsInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
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
        /// Creates pixel-art world decoration behind the menu UI.
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
        /// Creates the menu UI canvas, title, buttons, and settings panel.
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
        /// Creates the root screen-space Canvas used by the main menu.
        /// </summary>
        /// <returns>The configured Canvas component.</returns>
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
        /// Builds the settings overlay used by the main menu.
        /// </summary>
        /// <param name="parent">Canvas transform that owns the settings panel.</param>
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
        /// Creates a standard menu button with default size.
        /// </summary>
        /// <param name="name">GameObject name for the button.</param>
        /// <param name="parent">Parent transform for the button.</param>
        /// <param name="label">Text shown inside the button.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        /// <param name="action">Action called when the button is clicked.</param>
        /// <returns>The created Button component.</returns>
        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            return CreateButton(name, parent, label, anchor, anchoredPosition, action, new Vector2(300f, 58f));
        }

        /// <summary>
        /// Creates a menu button with a custom size.
        /// </summary>
        /// <param name="name">GameObject name for the button.</param>
        /// <param name="parent">Parent transform for the button.</param>
        /// <param name="label">Text shown inside the button.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        /// <param name="action">Action called when the button is clicked.</param>
        /// <param name="size">Width and height of the button RectTransform.</param>
        /// <returns>The created Button component.</returns>
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
        /// Creates a panel with outline, shadow, and small corner rune accents.
        /// </summary>
        /// <param name="name">GameObject name for the panel.</param>
        /// <param name="parent">Parent transform for the panel.</param>
        /// <param name="color">Main panel color.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        /// <param name="size">Width and height of the panel.</param>
        /// <param name="outlineColor">Outline color used for the frame.</param>
        /// <returns>The created framed panel GameObject.</returns>
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
        /// Creates a thin decorative UI divider.
        /// </summary>
        /// <param name="name">GameObject name for the divider.</param>
        /// <param name="parent">Parent transform for the divider.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        /// <param name="size">Width and height of the divider.</param>
        /// <param name="color">Divider color.</param>
        private void CreateDivider(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject divider = CreatePanel(name, parent, color, anchor, anchoredPosition, size);
            Shadow shadow = divider.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }

        /// <summary>
        /// Creates one square corner accent for a framed menu panel.
        /// </summary>
        /// <param name="name">GameObject name for the accent.</param>
        /// <param name="parent">Parent transform for the accent.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        private void CreateCornerAccent(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition)
        {
            GameObject accent = CreatePanel(name, parent, runeGold, anchor, anchoredPosition, new Vector2(16f, 16f));
            RectTransform rect = accent.GetComponent<RectTransform>();
            rect.rotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>
        /// Creates a colored UI panel with an Image and RectTransform.
        /// </summary>
        /// <param name="name">GameObject name for the panel.</param>
        /// <param name="parent">Parent transform for the panel.</param>
        /// <param name="color">Image color.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        /// <param name="size">Width and height of the panel.</param>
        /// <returns>The created panel GameObject.</returns>
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
        /// Creates a UI Text element using the menu font.
        /// </summary>
        /// <param name="name">GameObject name for the text.</param>
        /// <param name="parent">Parent transform for the text.</param>
        /// <param name="value">Initial text value.</param>
        /// <param name="fontSize">Font size in UI points.</param>
        /// <param name="color">Text color.</param>
        /// <param name="alignment">Text alignment.</param>
        /// <param name="anchor">Anchor position for the RectTransform.</param>
        /// <param name="anchoredPosition">Anchored UI position.</param>
        /// <param name="size">Width and height of the text RectTransform.</param>
        /// <returns>The created Text component.</returns>
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
        /// Creates a tiled SpriteRenderer for menu platforms.
        /// </summary>
        /// <param name="name">GameObject name for the sprite.</param>
        /// <param name="sprite">Sprite asset to render.</param>
        /// <param name="position">World position.</param>
        /// <param name="size">Tiled sprite size.</param>
        /// <param name="sortingOrder">Sprite sorting order.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="color">Sprite color tint.</param>
        /// <returns>The created SpriteRenderer.</returns>
        private SpriteRenderer CreateTiledSprite(string name, Sprite sprite, Vector2 position, Vector2 size, int sortingOrder, Transform parent, Color color)
        {
            SpriteRenderer renderer = CreateSprite(name, sprite, position, Vector3.one, sortingOrder, parent, color);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            return renderer;
        }

        /// <summary>
        /// Creates a simple SpriteRenderer object for menu decoration.
        /// </summary>
        /// <param name="name">GameObject name for the sprite.</param>
        /// <param name="sprite">Sprite asset to render.</param>
        /// <param name="position">World position.</param>
        /// <param name="scale">World scale applied to the sprite object.</param>
        /// <param name="sortingOrder">Sprite sorting order.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="color">Sprite color tint.</param>
        /// <returns>The created SpriteRenderer.</returns>
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
        /// Starts menu background music if the clip exists in Resources.
        /// </summary>
        private void StartMusic()
        {
            AudioClip music = PixelArtLibrary.LoadMusic("time_for_adventure");
            if (music == null)
            {
                return;
            }

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = music;
            source.loop = true;
            source.volume = 0.24f;
            source.playOnAwake = false;
            source.Play();
        }

        /// <summary>
        /// Ensures the scene has an EventSystem so UI buttons can receive input.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
