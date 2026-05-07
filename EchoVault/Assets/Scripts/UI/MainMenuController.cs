using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoVault
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes")]
        public string gameSceneName = "PrototypeScene";

        [Header("Animation")]
        public float knightFramesPerSecond = 5f;
        public float coinBobSpeed = 2.2f;
        public float coinBobHeight = 0.12f;

        private readonly Color buttonNormal = new Color(0.09f, 0.12f, 0.18f, 0.94f);
        private readonly Color buttonHover = new Color(0.18f, 0.26f, 0.36f, 0.96f);
        private readonly Color buttonPressed = new Color(0.94f, 0.66f, 0.2f, 1f);
        private readonly Color textLight = new Color(0.94f, 0.96f, 1f, 1f);
        private readonly Color textMuted = new Color(0.74f, 0.8f, 0.88f, 1f);

        private SpriteRenderer knightRenderer;
        private SpriteRenderer echoRenderer;
        private Transform[] bobbingCoins;
        private Vector3[] coinBasePositions;
        private Sprite[] idleFrames;
        private Font menuFont;
        private GameObject modalRoot;
        private Text modalTitle;
        private Text modalBody;
        private float animationTimer;
        private int frameIndex;

        private void Awake()
        {
            menuFont = Resources.Load<Font>("BrackeysPlatformer/Fonts/PixelOperator8-Bold");
            idleFrames = PixelArtLibrary.KnightIdleFrames;

            EnsureCamera();
            BuildWorld();
            BuildCanvas();
            StartMusic();
        }

        private void Update()
        {
            AnimateKnight();
            AnimateCoins();

            if (modalRoot != null && modalRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseModal();
            }
        }

        public void StartGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

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

        public void ShowCredits()
        {
            ShowModal(
                "CREDITS",
                "EchoVault prototype\n\n" +
                "Pixel art, font, music, and sound effects are from the Brackeys Platformer Bundle.\n\n" +
                "License: CC0 1.0 Universal\n\n" +
                "Bundle credits include Brackeys, analogStudios_, RottingPixels, Asbjorn Thirslund, Jayvee Enaguas, and HarvettFox96.");
        }

        public void CloseModal()
        {
            modalRoot.SetActive(false);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

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

        private void BuildWorld()
        {
            GameObject worldRoot = new GameObject("Pixel Menu World");

            CreateTiledSprite("Distant Vault Floor", PixelArtLibrary.StoneTile, new Vector2(0f, -3.95f), new Vector2(22f, 1.2f), -2, worldRoot.transform, new Color(0.5f, 0.62f, 0.74f, 0.32f));
            CreateTiledSprite("Main Platform", PixelArtLibrary.GroundTile, new Vector2(0f, -3.4f), new Vector2(18.5f, 1f), -1, worldRoot.transform, Color.white);
            CreateTiledSprite("Left Ledge", PixelArtLibrary.GroundTile, new Vector2(-5.6f, -1.9f), new Vector2(3.4f, 0.65f), -1, worldRoot.transform, Color.white);
            CreateTiledSprite("Right Ledge", PixelArtLibrary.GroundTile, new Vector2(5.2f, -1.55f), new Vector2(3.4f, 0.65f), -1, worldRoot.transform, Color.white);

            CreateSprite("Vault Door Decoration", PixelArtLibrary.DoorTile, new Vector2(0f, -1.15f), new Vector3(4.4f, 4.4f, 1f), -3, worldRoot.transform, new Color(0.55f, 0.65f, 0.88f, 0.18f));
            CreateSprite("Menu Chest", PixelArtLibrary.ChestTile, new Vector2(-5.8f, -1.28f), new Vector3(1.4f, 1.4f, 1f), 2, worldRoot.transform, Color.white);
            CreateSprite("Menu Slime", PixelArtLibrary.HazardSlime, new Vector2(5.45f, -2.62f), new Vector3(1.35f, 1.35f, 1f), 2, worldRoot.transform, Color.white);
            CreateSprite("Exit Gem Decoration", PixelArtLibrary.ExitGem, new Vector2(6.35f, -0.82f), new Vector3(1.4f, 1.4f, 1f), 2, worldRoot.transform, Color.white);

            echoRenderer = CreateSprite("Echo Menu Knight", PixelArtLibrary.KnightIdle, new Vector2(-1.25f, -2.58f), new Vector3(2.4f, 2.4f, 1f), 3, worldRoot.transform, new Color(0.22f, 0.9f, 1f, 0.42f));
            knightRenderer = CreateSprite("Menu Knight", PixelArtLibrary.KnightIdle, new Vector2(-0.62f, -2.52f), new Vector3(2.4f, 2.4f, 1f), 4, worldRoot.transform, Color.white);

            bobbingCoins = new Transform[5];
            coinBasePositions = new Vector3[bobbingCoins.Length];
            for (int i = 0; i < bobbingCoins.Length; i++)
            {
                float x = -3.4f + (i * 0.62f);
                SpriteRenderer coin = CreateSprite("Menu Coin " + (i + 1), PixelArtLibrary.CoinTile, new Vector2(x, -0.88f), new Vector3(0.95f, 0.95f, 1f), 2, worldRoot.transform, Color.white);
                bobbingCoins[i] = coin.transform;
                coinBasePositions[i] = coin.transform.position;
            }
        }

        private void BuildCanvas()
        {
            EnsureEventSystem();

            Canvas canvas = CreateCanvas();
            Transform canvasTransform = canvas.transform;

            Text title = CreateText("Title", canvasTransform, "ECHOVAULT", 78, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(860f, 110f));
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            Text subtitle = CreateText("Subtitle", canvasTransform, "record yourself, raid the vault, escape with the loot", 24, textMuted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(920f, 46f));
            subtitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.65f);

            CreateButton("Start Game Button", canvasTransform, "START GAME", new Vector2(1f, 0.5f), new Vector2(-235f, 92f), StartGame);
            CreateButton("Controls Button", canvasTransform, "CONTROLS", new Vector2(1f, 0.5f), new Vector2(-235f, 20f), ShowControls);
            CreateButton("Credits Button", canvasTransform, "CREDITS", new Vector2(1f, 0.5f), new Vector2(-235f, -52f), ShowCredits);
            CreateButton("Quit Button", canvasTransform, "QUIT", new Vector2(1f, 0.5f), new Vector2(-235f, -124f), QuitGame);

            CreateText("Build Note", canvasTransform, "Sprint 2 prototype menu", 18, new Color(0.68f, 0.74f, 0.84f, 1f), TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(26f, 24f), new Vector2(420f, 34f));

            BuildModal(canvasTransform);
        }

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

        private void ShowModal(string title, string body)
        {
            modalTitle.text = title;
            modalBody.text = body;
            modalRoot.SetActive(true);
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            return CreateButton(name, parent, label, anchor, anchoredPosition, action, new Vector2(300f, 58f));
        }

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

        private Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
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

        private SpriteRenderer CreateTiledSprite(string name, Sprite sprite, Vector2 position, Vector2 size, int sortingOrder, Transform parent, Color color)
        {
            SpriteRenderer renderer = CreateSprite(name, sprite, position, Vector3.one, sortingOrder, parent, color);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            return renderer;
        }

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

        private void AnimateKnight()
        {
            if (idleFrames == null || idleFrames.Length == 0 || knightRenderer == null)
            {
                return;
            }

            animationTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, knightFramesPerSecond);
            while (animationTimer >= frameDuration)
            {
                animationTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % idleFrames.Length;
            }

            knightRenderer.sprite = idleFrames[frameIndex];
            if (echoRenderer != null)
            {
                int echoFrame = (frameIndex + 1) % idleFrames.Length;
                echoRenderer.sprite = idleFrames[echoFrame];
            }
        }

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
