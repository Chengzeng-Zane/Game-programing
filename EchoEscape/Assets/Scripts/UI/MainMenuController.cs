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
    /// It creates menu world art, UI buttons, controls/credits modals, background music,
    /// and safely starts the gameplay scene when one is assigned in Build Settings.
    /// </remarks>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        /// <summary>
        /// Name of the gameplay scene loaded by Start Game.
        /// </summary>
        public string gameSceneName = string.Empty;

        [Header("Menu Motion")]
        /// <summary>
        /// Speed used by decorative menu coin bobbing animation.
        /// </summary>
        public float coinBobSpeed = 2.2f;

        /// <summary>
        /// Vertical distance used by decorative menu coin bobbing animation.
        /// </summary>
        public float coinBobHeight = 0.12f;

        private readonly Color buttonNormal = new Color(0.075f, 0.1f, 0.15f, 0.96f);
        private readonly Color buttonHover = new Color(0.18f, 0.26f, 0.36f, 1f);
        private readonly Color buttonPressed = new Color(0.94f, 0.66f, 0.2f, 1f);
        private readonly Color textLight = new Color(0.94f, 0.96f, 1f, 1f);
        private readonly Color textMuted = new Color(0.74f, 0.8f, 0.88f, 1f);

        private Transform[] bobbingCoins;
        private Vector3[] coinBasePositions;
        private Font menuFont;
        private GameObject modalRoot;
        private Text modalTitle;
        private Text modalBody;

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
        /// Animates menu coins and lets Escape close an open modal.
        /// </remarks>
        private void Update()
        {
            AnimateCoins();

            if (modalRoot != null && modalRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseModal();
            }
        }

        /// <summary>
        /// Attempts to load the configured gameplay scene.
        /// </summary>
        /// <remarks>
        /// Called by the Start Game button. If no valid gameplay scene is assigned, a safe modal is shown instead.
        /// </remarks>
        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName) || !SceneExistsInBuild(gameSceneName))
            {
                ShowModal("GAMEPLAY", "Gameplay scene is not ready yet.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Opens the controls modal.
        /// </summary>
        /// <remarks>
        /// Called by the Controls button.
        /// </remarks>
        public void ShowControls()
        {
            ShowModal(
                "CONTROLS",
                "MOVE      A / D OR ARROW KEYS\n" +
                "JUMP      SPACE / W / UP\n" +
                "RECORD    Q\n" +
                "REPLAY    E\n" +
                "OPEN      F\n" +
                "RESTART   R\n\n" +
                "Record a short route, replay your echo, hold switches, open random chests, and extract before death removes unbanked loot.");
        }

        /// <summary>
        /// Opens the credits modal.
        /// </summary>
        /// <remarks>
        /// Called by the Credits button.
        /// </remarks>
        public void ShowCredits()
        {
            ShowModal(
                "CREDITS",
                "Echo Escape prototype\n\n" +
                "Pixel art, font, music, and sound effects are from the Brackeys Platformer Bundle.\n\n" +
                "License: CC0 1.0 Universal\n\n" +
                "Bundle credits include Brackeys, analogStudios_, RottingPixels, Asbjorn Thirslund, Jayvee Enaguas, and HarvettFox96.");
        }

        /// <summary>
        /// Closes the currently open menu modal.
        /// </summary>
        /// <remarks>
        /// Called by the Back button and by Escape in Update.
        /// </remarks>
        public void CloseModal()
        {
            modalRoot.SetActive(false);
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
            camera.backgroundColor = new Color(0.045f, 0.055f, 0.08f, 1f);
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

            CreateTiledSprite("Distant Vault Floor", PixelArtLibrary.StoneTile, new Vector2(0f, -3.95f), new Vector2(22f, 1.2f), -2, worldRoot.transform, new Color(0.5f, 0.62f, 0.74f, 0.32f));
            CreateTiledSprite("Main Platform", PixelArtLibrary.GroundTile, new Vector2(0f, -3.4f), new Vector2(18.5f, 1f), -1, worldRoot.transform, Color.white);
            CreateTiledSprite("Left Ledge", PixelArtLibrary.GroundTile, new Vector2(-5.6f, -1.9f), new Vector2(3.4f, 0.65f), -1, worldRoot.transform, Color.white);
            CreateTiledSprite("Right Ledge", PixelArtLibrary.GroundTile, new Vector2(5.2f, -1.55f), new Vector2(3.4f, 0.65f), -1, worldRoot.transform, Color.white);

            CreateSprite("Vault Door Decoration", PixelArtLibrary.DoorTile, new Vector2(0f, -1.08f), new Vector3(5.4f, 5.4f, 1f), -3, worldRoot.transform, new Color(0.55f, 0.65f, 0.88f, 0.16f));
            CreateSprite("Menu Chest", PixelArtLibrary.ChestTile, new Vector2(-5.8f, -1.28f), new Vector3(1.35f, 1.35f, 1f), 2, worldRoot.transform, new Color(1f, 1f, 1f, 0.9f));
            CreateSprite("Menu Slime", PixelArtLibrary.HazardSlime, new Vector2(5.45f, -2.62f), new Vector3(1.3f, 1.3f, 1f), 2, worldRoot.transform, new Color(1f, 1f, 1f, 0.9f));
            CreateSprite("Exit Gem Decoration", PixelArtLibrary.ExitGem, new Vector2(6.35f, -0.82f), new Vector3(1.35f, 1.35f, 1f), 2, worldRoot.transform, new Color(1f, 1f, 1f, 0.9f));

            bobbingCoins = new Transform[5];
            coinBasePositions = new Vector3[bobbingCoins.Length];
            for (int i = 0; i < bobbingCoins.Length; i++)
            {
                float x = -1.24f + (i * 0.62f);
                SpriteRenderer coin = CreateSprite("Menu Coin " + (i + 1), PixelArtLibrary.CoinTile, new Vector2(x, -2.72f), new Vector3(0.9f, 0.9f, 1f), 2, worldRoot.transform, new Color(1f, 1f, 1f, 0.95f));
                bobbingCoins[i] = coin.transform;
                coinBasePositions[i] = coin.transform.position;
            }
        }

        /// <summary>
        /// Creates the menu UI canvas, title, buttons, build note, and modal.
        /// </summary>
        private void BuildCanvas()
        {
            EnsureEventSystem();

            Canvas canvas = CreateCanvas();
            Transform canvasTransform = canvas.transform;

            Text title = CreateText("Title", canvasTransform, "ECHO ESCAPE", 78, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(860f, 110f));
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            Text subtitle = CreateText("Subtitle", canvasTransform, "record yourself, raid the vault, escape with the loot", 24, textMuted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(920f, 46f));
            subtitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.65f);

            CreateButton("Start Game Button", canvasTransform, "START GAME", new Vector2(0.5f, 0.5f), new Vector2(0f, 92f), StartGame, new Vector2(360f, 64f));
            CreateButton("Controls Button", canvasTransform, "CONTROLS", new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), ShowControls, new Vector2(360f, 64f));
            CreateButton("Credits Button", canvasTransform, "CREDITS", new Vector2(0.5f, 0.5f), new Vector2(0f, -68f), ShowCredits, new Vector2(360f, 64f));
            CreateButton("Quit Button", canvasTransform, "QUIT", new Vector2(0.5f, 0.5f), new Vector2(0f, -148f), QuitGame, new Vector2(360f, 64f));

            CreateText("Build Note", canvasTransform, "Sprint 2 prototype menu", 18, new Color(0.68f, 0.74f, 0.84f, 1f), TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(26f, 24f), new Vector2(420f, 34f));

            BuildModal(canvasTransform);
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
        /// Builds the modal overlay used for controls, credits, and safe gameplay messages.
        /// </summary>
        /// <param name="parent">Canvas transform that owns the modal.</param>
        private void BuildModal(Transform parent)
        {
            modalRoot = new GameObject("Menu Modal");
            modalRoot.transform.SetParent(parent, false);

            Image blocker = modalRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.62f);

            RectTransform blockerRect = modalRoot.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            GameObject panel = CreatePanel("Modal Panel", modalRoot.transform, new Color(0.08f, 0.11f, 0.16f, 0.98f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 430f));
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.96f, 0.68f, 0.25f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            modalTitle = CreateText("Modal Title", panel.transform, string.Empty, 38, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(680f, 70f));
            modalBody = CreateText("Modal Body", panel.transform, string.Empty, 22, textMuted, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(660f, 250f));
            modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            modalBody.verticalOverflow = VerticalWrapMode.Overflow;
            modalBody.lineSpacing = 1.1f;

            CreateButton("Close Modal Button", panel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 46f), CloseModal, new Vector2(220f, 56f));
            modalRoot.SetActive(false);
        }

        /// <summary>
        /// Shows the menu modal with a title and body message.
        /// </summary>
        /// <param name="title">Modal title text.</param>
        /// <param name="body">Modal body text.</param>
        private void ShowModal(string title, string body)
        {
            modalTitle.text = title;
            modalBody.text = body;
            modalRoot.SetActive(true);
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
            outline.effectColor = new Color(0.94f, 0.66f, 0.2f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

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
            text.raycastTarget = false;
            return button;
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
        /// Animates decorative menu coins with a simple vertical sine wave.
        /// </summary>
        private void AnimateCoins()
        {
            if (bobbingCoins == null || coinBasePositions == null)
            {
                return;
            }

            for (int i = 0; i < bobbingCoins.Length; i++)
            {
                float y = Mathf.Sin((Time.time * coinBobSpeed) + i) * coinBobHeight;
                bobbingCoins[i].position = coinBasePositions[i] + new Vector3(0f, y, 0f);
            }
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
