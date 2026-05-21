using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Builds the simple Level2_LootTutorial prototype layout.
    /// </summary>
    /// <remarks>
    /// Attach this script to a bootstrap object in Level2_LootTutorial.
    /// It creates the attack lesson, block enemy, chest spawn point, hazard gap, exit, player, camera,
    /// popup UI, and Game Manager when the scene is empty. The editor level builder also calls BuildLayout
    /// so the same layout can be saved into the scene asset.
    /// </remarks>
    [ExecuteAlways]
    public class Level2LootTutorialSetup : MonoBehaviour
    {
        private const string GeneratedRootName = "Level2_LootTutorial_Prototype";
        private const string StyleMarkerName = "StyleMarker_Level2_BlockPrototypeV2";

        [SerializeField]
        private bool buildOnAwake = true; // If true, builds the level at runtime when the scene has no player yet.

        private static bool isBuilding;

        /// <summary>
        /// Unity event method called when the bootstrap object is created.
        /// </summary>
        /// <remarks>
        /// Used as a safety fallback so Level2_LootTutorial can build itself when loaded from Build Settings.
        /// </remarks>
        private void Awake()
        {
            if (buildOnAwake && Application.isPlaying)
            {
                BuildIfMissing();
            }
        }

        /// <summary>
        /// Unity event method called when the bootstrap object becomes enabled.
        /// </summary>
        /// <remarks>
        /// In edit mode this builds the visible layout when the scene is opened, so the Game view is not camera-less.
        /// </remarks>
        private void OnEnable()
        {
            if (buildOnAwake && !Application.isPlaying)
            {
                BuildIfMissing();
            }
        }

        /// <summary>
        /// Builds the Level2 layout unless the current block-style layout already exists.
        /// </summary>
        public void BuildIfMissing()
        {
            if (isBuilding || SceneHasCurrentGeneratedLayout())
            {
                return;
            }

            isBuilding = true;
            try
            {
                ClearGeneratedLevelObjects();
                BuildLayout();
            }
            finally
            {
                isBuilding = false;
            }
        }

        /// <summary>
        /// Creates the full Level2_LootTutorial map and prototype gameplay objects.
        /// </summary>
        /// <remarks>
        /// The route is: PlayerStart -> Attack tutorial -> Enemy_Block -> Chest tutorial -> Chest -> Hazard gap -> Exit.
        /// </remarks>
        public static void BuildLayout()
        {
            Transform root = CreateRoot(GeneratedRootName);
            CreateStyleMarker(root);
            CreateLight();
            CreateGround(root);
            Transform playerStart = CreatePlayerStart(root);
            PlayerController2D player = CreatePlayer(playerStart.position, root);
            CreateCamera(player.transform);

            TutorialPopupManager popupManager = CreateTutorialPopupUi();
            CreateQuestionMark(
                "QuestionMark_Attack",
                new Vector2(-4.9f, -1.55f),
                "Attack",
                "Press J to attack.\nDefeat the enemy before moving forward.\nAvoid touching enemies.",
                popupManager,
                root);

            CreateEnemyBlock(root);
            CreateQuestionMark(
                "QuestionMark_Chest",
                new Vector2(2.7f, -1.55f),
                "Treasure Chest",
                "Press F near a chest to open it.\nChests give random loot.\nEscape alive to keep your loot.\nIf you die, the loot is lost.",
                popupManager,
                root);

            CreateChestSpawn(root);
            CreateHazardGap(root);
            CreateExit(root);
            CreateGameManager(player, player.GetComponent<ActionRecorder>(), playerStart);
        }

        /// <summary>
        /// Creates a named root transform for the generated Level2 hierarchy.
        /// </summary>
        /// <param name="name">Name of the root GameObject.</param>
        /// <returns>The created root transform.</returns>
        private static Transform CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            return root.transform;
        }

        /// <summary>
        /// Creates a basic directional light for visibility.
        /// </summary>
        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.8f;
        }

        /// <summary>
        /// Creates the simple left-to-right ground pieces and the small hazard gap.
        /// </summary>
        /// <param name="parent">Root object used to organize the ground objects.</param>
        private static void CreateGround(Transform parent)
        {
            GameObject groundRoot = new GameObject("Ground");
            groundRoot.transform.SetParent(parent, false);

            CreateBlock("Ground_Start", new Vector2(-5.2f, -2.5f), new Vector2(5.8f, 0.8f), new Color(0.32f, 0.34f, 0.38f), true, groundRoot.transform);
            CreateBlock("Ground_Combat", new Vector2(-0.8f, -2.5f), new Vector2(3.4f, 0.8f), new Color(0.32f, 0.34f, 0.38f), true, groundRoot.transform);
            CreateBlock("Ground_Chest", new Vector2(3.9f, -2.5f), new Vector2(4f, 0.8f), new Color(0.32f, 0.34f, 0.38f), true, groundRoot.transform);
            CreateBlock("Ground_AfterHazard", new Vector2(9.2f, -2.5f), new Vector2(3.8f, 0.8f), new Color(0.32f, 0.34f, 0.38f), true, groundRoot.transform);
            CreateBlock("Ground_Exit", new Vector2(13f, -2.5f), new Vector2(3.2f, 0.8f), new Color(0.32f, 0.34f, 0.38f), true, groundRoot.transform);
        }

        /// <summary>
        /// Creates the PlayerStart marker used by EchoEscapeGameManager for respawning.
        /// </summary>
        /// <param name="parent">Root object used to organize the marker.</param>
        /// <returns>The marker transform.</returns>
        private static Transform CreatePlayerStart(Transform parent)
        {
            GameObject start = CreateBlock("PlayerStart", new Vector2(-7f, -1.1f), new Vector2(0.28f, 1f), new Color(0.2f, 0.72f, 1f, 0.75f), false, parent);
            return start.transform;
        }

        /// <summary>
        /// Creates the controllable player with movement, recording, and attack scripts.
        /// </summary>
        /// <param name="startPosition">Position of the PlayerStart marker.</param>
        /// <param name="parent">Root object used to organize the player.</param>
        /// <returns>The player controller component.</returns>
        private static PlayerController2D CreatePlayer(Vector3 startPosition, Transform parent)
        {
            GameObject playerObject = CreateBlock("Player", new Vector2(startPosition.x, startPosition.y), new Vector2(0.65f, 1.35f), new Color(0.88f, 0.94f, 1f), true, parent);
            playerObject.tag = "Player";

            BoxCollider2D blockCollider = playerObject.GetComponent<BoxCollider2D>();
            if (blockCollider != null)
            {
                RemoveObject(blockCollider);
            }

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.4f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D capsule = playerObject.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.65f, 1.5f);
            capsule.offset = new Vector2(0f, -0.05f);

            PlayerController2D controller = playerObject.AddComponent<PlayerController2D>();
            controller.moveSpeed = 5.5f;
            controller.jumpForce = 9f;

            playerObject.AddComponent<ActionRecorder>();
            playerObject.AddComponent<PlayerAttack>();
            return controller;
        }

        /// <summary>
        /// Creates a camera that follows the player through the second tutorial room.
        /// </summary>
        /// <param name="target">Player transform followed by the camera.</param>
        private static void CreateCamera(Transform target)
        {
            Camera existingCamera = Camera.main;
            GameObject cameraObject = existingCamera != null ? existingCamera.gameObject : new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.position + new Vector3(2f, 1.4f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.75f;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.025f);

            CameraFollow follow = cameraObject.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = cameraObject.AddComponent<CameraFollow>();
            }

            follow.target = target;
            follow.offset = new Vector3(2f, 1.4f, -10f);
            follow.followSpeed = 5f;
        }

        /// <summary>
        /// Checks whether this scene already contains the current block-style generated layout.
        /// </summary>
        /// <returns>True if the current style marker exists in the same scene as this bootstrap object.</returns>
        private bool SceneHasCurrentGeneratedLayout()
        {
            Transform[] transforms = FindObjectsOfType<Transform>(true);
            foreach (Transform sceneTransform in transforms)
            {
                if (sceneTransform.gameObject.scene == gameObject.scene && sceneTransform.name == StyleMarkerName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes older generated Level2 objects before rebuilding the clean block-style layout.
        /// </summary>
        private void ClearGeneratedLevelObjects()
        {
            RemoveSceneObject(GeneratedRootName);
            RemoveSceneObject("GameManager");
            RemoveSceneObject("TutorialPopupUI");
            RemoveSceneObject("EventSystem");
            RemoveSceneObject("Directional Light");
            RemoveSceneObject("Prototype HUD");
        }

        /// <summary>
        /// Removes a named root object from the same scene as this bootstrap object.
        /// </summary>
        /// <param name="objectName">Name of the root object to remove.</param>
        private void RemoveSceneObject(string objectName)
        {
            GameObject[] rootObjects = gameObject.scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject.name == objectName)
                {
                    RemoveObject(rootObject);
                    return;
                }
            }
        }

        /// <summary>
        /// Creates the marker used to recognize the current Level2 block prototype style.
        /// </summary>
        /// <param name="parent">Generated Level2 root transform.</param>
        private static void CreateStyleMarker(Transform parent)
        {
            GameObject marker = new GameObject(StyleMarkerName);
            marker.transform.SetParent(parent, false);
        }

        /// <summary>
        /// Creates the simple block enemy used by the attack tutorial.
        /// </summary>
        /// <param name="parent">Root object used to organize the enemy.</param>
        private static void CreateEnemyBlock(Transform parent)
        {
            GameObject enemy = CreateBlock("Enemy_Block", new Vector2(-1.25f, -1.65f), new Vector2(0.8f, 0.65f), new Color(0.72f, 0.18f, 0.82f), false, parent);
            enemy.AddComponent<SimpleEnemy>();
        }

        /// <summary>
        /// Creates the marker used by the Game Manager to spawn the tutorial chest.
        /// </summary>
        /// <param name="parent">Root object used to organize the chest marker.</param>
        private static void CreateChestSpawn(Transform parent)
        {
            GameObject marker = CreateBlock("Chest_Block_Spawn", new Vector2(4.65f, -1.72f), new Vector2(0.85f, 0.55f), new Color(1f, 0.74f, 0.22f), false, parent);
            marker.AddComponent<ChestSpawnPoint>();
        }

        /// <summary>
        /// Creates the red hazard trigger below the small post-chest gap.
        /// </summary>
        /// <param name="parent">Root object used to organize the hazard.</param>
        private static void CreateHazardGap(Transform parent)
        {
            GameObject hazard = CreateBlock("Hazard_Block", new Vector2(6.95f, -3.25f), new Vector2(2f, 0.7f), new Color(0.95f, 0.16f, 0.12f), false, parent);
            HazardZone hazardZone = hazard.AddComponent<HazardZone>();
            hazardZone.deathReason = "fell into the loot risk pit";
        }

        /// <summary>
        /// Creates the Level2 exit trigger. It completes the demo instead of loading a missing next scene.
        /// </summary>
        /// <param name="parent">Root object used to organize the exit.</param>
        private static void CreateExit(Transform parent)
        {
            GameObject exit = CreateBlock("Exit_Level2", new Vector2(13.9f, -1.45f), new Vector2(0.85f, 1.55f), new Color(0.1f, 0.9f, 0.55f, 0.85f), false, parent);
            exit.AddComponent<GoalZone>();
        }

        /// <summary>
        /// Creates the Level2 Game Manager, loot table, and HUD.
        /// </summary>
        /// <param name="player">Player controller created for this scene.</param>
        /// <param name="recorder">Recorder attached to the player.</param>
        /// <param name="playerStart">Spawn transform used when the player dies.</param>
        private static void CreateGameManager(PlayerController2D player, ActionRecorder recorder, Transform playerStart)
        {
            GameObject managerObject = new GameObject("GameManager");
            EchoEscapeGameManager manager = managerObject.AddComponent<EchoEscapeGameManager>();
            manager.player = player;
            manager.recorder = recorder;
            manager.playerSpawn = playerStart;
            manager.chestsPerRun = 1;
            manager.useTutorialDirector = false;
            manager.usePrototypeVisualSkinner = false;
            manager.startingStatusMessage = "Defeat the enemy, open the chest, and escape alive to secure the loot.";
            manager.lootTable = new[]
            {
                new LootDefinition("Old Coin", "Common", 60),
                new LootDefinition("Blue Gem", "Rare", 30),
                new LootDefinition("Golden Relic", "Epic", 10)
            };

            PrototypeVisualSkinner visualSkinner = managerObject.GetComponent<PrototypeVisualSkinner>();
            if (visualSkinner != null)
            {
                visualSkinner.enabled = false;
                RemoveObject(visualSkinner);
            }
        }

        /// <summary>
        /// Creates one tutorial question mark trigger and visible icon.
        /// </summary>
        /// <param name="name">Name of the question mark object.</param>
        /// <param name="position">World position of the trigger.</param>
        /// <param name="title">Popup title.</param>
        /// <param name="message">Popup body text.</param>
        /// <param name="popupManager">Popup manager that displays this trigger.</param>
        /// <param name="parent">Root object used to organize the trigger.</param>
        private static void CreateQuestionMark(string name, Vector2 position, string title, string message, TutorialPopupManager popupManager, Transform parent)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(position.x, position.y, -0.05f);

            BoxCollider2D trigger = marker.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.9f, 1.25f);

            TutorialPopupTrigger popupTrigger = marker.AddComponent<TutorialPopupTrigger>();
            popupTrigger.popupManager = popupManager;
            popupTrigger.tutorialTitle = title;
            popupTrigger.tutorialMessage = message;
            popupTrigger.showOnlyOnce = false;
            popupTrigger.hideAfterUse = false;

            GameObject icon = CreateBlock("QuestionMarkIcon", Vector2.zero, new Vector2(0.45f, 0.45f), new Color(1f, 0.86f, 0.16f), false, marker.transform);
            icon.transform.localPosition = Vector3.zero;

            GameObject textObject = new GameObject("QuestionMarkText");
            textObject.transform.SetParent(marker.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.03f, -0.05f);

            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = "?";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 32;
            text.characterSize = 0.1f;
            text.color = Color.black;

            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingOrder = 7;
            }
        }

        /// <summary>
        /// Creates the dark popup UI used by Level2 question mark triggers.
        /// </summary>
        /// <returns>The configured TutorialPopupManager.</returns>
        private static TutorialPopupManager CreateTutorialPopupUi()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("TutorialPopupUI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            TutorialPopupManager manager = canvasObject.AddComponent<TutorialPopupManager>();
            manager.pauseGameWhenOpen = true;

            GameObject panelObject = CreateUiObject("TutorialPopupPanel", canvasObject.transform);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.88f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -36f);
            panelRect.sizeDelta = new Vector2(760f, 285f);

            Text titleText = CreateUiText("TitleText", panelObject.transform, "Attack", 30, FontStyle.Bold, new Vector2(0f, -28f), new Vector2(700f, 42f), new Color(1f, 0.88f, 0.28f));
            Text bodyText = CreateUiText("BodyText", panelObject.transform, "Press J to attack.", 18, FontStyle.Normal, new Vector2(0f, -84f), new Vector2(700f, 168f), Color.white);
            Text closeText = CreateUiText("CloseHintText", panelObject.transform, "Press Esc to close.", 16, FontStyle.Normal, new Vector2(0f, -246f), new Vector2(700f, 24f), new Color(0.72f, 0.78f, 0.88f));

            titleText.alignment = TextAnchor.MiddleLeft;
            bodyText.alignment = TextAnchor.UpperLeft;
            closeText.alignment = TextAnchor.MiddleRight;

            manager.popupPanel = panelObject;
            manager.titleText = titleText;
            manager.bodyText = bodyText;
            panelObject.SetActive(false);

            return manager;
        }

        /// <summary>
        /// Ensures the scene has an EventSystem for UI input.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// Creates a UI object with a RectTransform.
        /// </summary>
        /// <param name="name">Name of the UI object.</param>
        /// <param name="parent">Parent transform.</param>
        /// <returns>The created UI GameObject.</returns>
        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject uiObject = new GameObject(name);
            uiObject.transform.SetParent(parent, false);
            uiObject.AddComponent<RectTransform>();
            return uiObject;
        }

        /// <summary>
        /// Creates one UI Text object for the tutorial popup.
        /// </summary>
        /// <param name="name">Name of the text object.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="text">Initial text value.</param>
        /// <param name="fontSize">Font size.</param>
        /// <param name="fontStyle">Font style.</param>
        /// <param name="position">Anchored UI position.</param>
        /// <param name="size">RectTransform size.</param>
        /// <param name="color">Text color.</param>
        /// <returns>The created Text component.</returns>
        private static Text CreateUiText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, Vector2 position, Vector2 size, Color color)
        {
            GameObject textObject = CreateUiObject(name, parent);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            Text uiText = textObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.fontStyle = fontStyle;
            uiText.color = color;
            uiText.raycastTarget = false;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        /// <summary>
        /// Creates a simple colored block with a 2D collider.
        /// </summary>
        /// <param name="name">Object name.</param>
        /// <param name="position">World position.</param>
        /// <param name="size">World size.</param>
        /// <param name="color">Visible color.</param>
        /// <param name="solid">True for solid collider; false for trigger collider.</param>
        /// <param name="parent">Optional parent transform.</param>
        /// <returns>The created block GameObject.</returns>
        private static GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color, bool solid, Transform parent)
        {
            return PrototypeFactory.CreateBlock(name, position, size, color, solid, parent);
        }

        /// <summary>
        /// Destroys editor/runtime objects with the correct Unity destroy call.
        /// </summary>
        /// <param name="target">Object or component to remove.</param>
        private static void RemoveObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
