using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Main Menu Controller. It builds the main menu scene at runtime, including background decoration, title, start button, settings panel, exit button and event system.
/// Gameplay logic: Start Game Will check whether the target scene exists in Build Settings, reset the treasure chest collection status for the new round, and then load the first level. menu music pass BackgroundMusic Keep it consistent with the level.
/// Collaborates with: SceneManager Responsible for scene switching; BackgroundMusic Responsible for music; EchoEscapeGameManager. ResetChestClaimsForNewRun Responsible for the new game state.
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
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
// The main menu is built when it is fully running; first load the pixel font, then create the camera, background, Canvas and music.
            menuFont = Resources.Load<Font>("BrackeysPlatformer/Fonts/PixelOperator8-Bold");

            EnsureCamera();
            BuildWorld();
            BuildCanvas();
            BackgroundMusic.EnsurePlaying("main_menu_music", 0.32f);
        }
        /// <summary>
/// Unity Called every frame. This handles input, timers, UI Real-time refresh of state or non-physics.
        /// </summary>
        private void Update()
        {
            if (settingsRoot != null && settingsRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
// When the settings panel opens, press Esc Close, consistent with normal menu operation habits.
                HideSettings();
            }
        }
        /// <summary>
/// Start a new game. It will confirm that the first level is in Build Settings Here, reset the treasure chest collection record for a new game, and then load the target level.
        /// </summary>
        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName) || !SceneExistsInBuild(gameSceneName))
            {
// It only warns when the scene is not configured properly, and does not allow the button to switch the game to an invalid scene.
                Debug.LogWarning("Gameplay scene is not ready yet.");
                return;
            }

// The new game must clear the static state of "the treasure chest has been taken in this round", otherwise you will not be able to get the treasure chest when you re-enter the game.
            EchoEscapeGameManager.ResetChestClaimsForNewRun();
            SceneManager.LoadScene(gameSceneName);
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
        public void ShowSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(true);
            }
        }
        /// <summary>
/// Hide correspondence UI Or visual state, usually called when the prompt ends, the pop-up window is closed, or the process is cleaned up.
        /// </summary>
        public void HideSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(false);
            }
        }
        /// <summary>
/// Close a door, panel or passage to restore it to a blocked or hidden state.
        /// </summary>
        public void CloseModal()
        {
            HideSettings();
        }
        /// <summary>
/// Exit the game. Stop in editor Play Mode, called after formal packaging Application. Quit。
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
/// Make sure the main menu has Camera and AudioListener. The scene is created automatically when the camera is not manually placed.
        /// </summary>
        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
// The main menu scene allows empty scenes to be started, and the script will fill in the Main Camera。
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
// Music and button sound effects required AudioListener To be heard.
                camera.gameObject.AddComponent<AudioListener>();
            }
        }
        /// <summary>
/// Check if the target scene name exists in Build Settings, avoid Start Game Loading a scene that doesn't exist.
        /// </summary>
/// <param name="sceneName">Target scene name for inspection Build Settings Or load the next level. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool SceneExistsInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
// Build Settings What is saved here is the complete path. Here, the file name is compared with the configured scene name.
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
/// Assemble a set of runtime objects or UI Elements used to form a complete menu, panel, or visual structure.
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
/// Assemble a set of runtime objects or UI Elements used to form a complete menu, panel, or visual structure.
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
            CreateButton("How To Play Button", heroPanel.transform, "HOW TO PLAY", new Vector2(0.5f, 1f), new Vector2(0f, -350f), ShowSettings, new Vector2(330f, 58f));
            CreateButton("Quit Button", heroPanel.transform, "QUIT", new Vector2(0.5f, 1f), new Vector2(0f, -422f), QuitGame, new Vector2(330f, 58f));

            BuildSettingsPanel(canvasTransform);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <returns>Return the created Canvas。</returns>
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
/// Assemble a set of runtime objects or UI Elements used to form a complete menu, panel, or visual structure.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
        private void BuildSettingsPanel(Transform parent)
        {
            settingsRoot = new GameObject("How To Play Panel");
            settingsRoot.transform.SetParent(parent, false);

            Image blocker = settingsRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0.015f, 0.01f, 0.72f);

            RectTransform blockerRect = settingsRoot.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            GameObject panel = CreateFramedPanel("How To Play Content Panel", settingsRoot.transform, new Color(0.035f, 0.075f, 0.065f, 0.98f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 470f), new Color(0.96f, 0.68f, 0.25f, 0.9f));
            CreateText("How To Play Title", panel.transform, "HOW TO PLAY", 40, textLight, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(560f, 70f));
            CreateDivider("How To Play Divider", panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(510f, 3f), new Color(1f, 0.78f, 0.3f, 0.9f));

            Text controls = CreateText(
                "Controls List",
                panel.transform,
                "Move: A / D\n" +
                "Jump: Space\n" +
                "Record Echo: Q\n" +
                "Replay Echo: E\n" +
                "Open Chest: F\n" +
                "Attack: J\n" +
                "Gravity Flip: Up/Down",
                24,
                textMuted,
                TextAnchor.UpperLeft,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(500f, 230f));
            controls.lineSpacing = 1.2f;
            controls.verticalOverflow = VerticalWrapMode.Overflow;

            CreateButton("Back Button", panel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 48f), HideSettings, new Vector2(220f, 56f));
            settingsRoot.SetActive(false);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="label">label Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="action">action Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Return the created UI Button components. </returns>
        private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            return CreateButton(name, parent, label, anchor, anchoredPosition, action, new Vector2(300f, 58f));
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="label">label Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="action">action Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Return the created UI Button components. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="outlineColor">outlineColor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns a created or found GameObjectto facilitate the caller to continue adding components or setting locations. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
        private void CreateDivider(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject divider = CreatePanel(name, parent, color, anchor, anchoredPosition, size);
            Shadow shadow = divider.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private void CreateCornerAccent(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition)
        {
            GameObject accent = CreatePanel(name, parent, runeGold, anchor, anchoredPosition, new Vector2(16f, 16f));
            RectTransform rect = accent.GetComponent<RectTransform>();
            rect.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns a created or found GameObjectto facilitate the caller to continue adding components or setting locations. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="value">The new parameter value to set. </param>
/// <param name="fontSize">fontSize Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="alignment">alignment Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns a created or found UI Text components. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="sprite">to be displayed Sprite picture. </param>
/// <param name="position">Target world coordinates, often used for respawn, spawning objects or recording Echo frame. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="sortingOrder">sortingOrder Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>return SpriteRenderer Type result for the caller to continue to judge or use. </returns>
        private SpriteRenderer CreateTiledSprite(string name, Sprite sprite, Vector2 position, Vector2 size, int sortingOrder, Transform parent, Color color)
        {
            SpriteRenderer renderer = CreateSprite(name, sprite, position, Vector3.one, sortingOrder, parent, color);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            return renderer;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="sprite">to be displayed Sprite picture. </param>
/// <param name="position">Target world coordinates, often used for respawn, spawning objects or recording Echo frame. </param>
/// <param name="scale">scale Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="sortingOrder">sortingOrder Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>return SpriteRenderer Type result for the caller to continue to judge or use. </returns>
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
/// Make sure the scene has EventSystem. Without it, buttons fail to respond to mouse clicks and keyboard navigation.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
// Already EventSystem time reuse to avoid multiple EventSystem Grab input.
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
