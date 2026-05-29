using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EchoEscape
{
    // 这个脚本负责生成简单的 Level2_LootTutorial 原型地图布局。
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

#if UNITY_EDITOR
        private const int MaxEditorBuildAttempts = 5;
        private int editorBuildAttempts;
#endif

        // 这个函数在引导对象创建时运行，用来初始化或安排场景生成。
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

        // 这个函数在游戏开始时运行一次。
        private void Start()
        {
            if (buildOnAwake && Application.isPlaying)
            {
                BuildIfMissing();
            }
        }

        // 这个函数在引导对象启用时运行，用来在编辑器中排队生成场景内容。
        /// <summary>
        /// Unity event method called when the bootstrap object becomes enabled.
        /// </summary>
        /// <remarks>
        /// In edit mode this builds the visible layout when the scene is opened, so the Game view is not camera-less.
        /// </remarks>
        private void OnEnable()
        {
            if (!buildOnAwake || Application.isPlaying)
            {
                return;
            }

#if UNITY_EDITOR
            QueueEditorBuildAfterSceneLoad();
#else
            BuildIfMissing();
#endif
        }

#if UNITY_EDITOR
        // 这个函数在脚本停用时运行。
        private void OnDisable()
        {
            EditorApplication.delayCall -= BuildIfSceneLoadedInEditor;
        }

        // 这个函数处理 QueueEditorBuildAfterSceneLoad 相关逻辑。
        private void QueueEditorBuildAfterSceneLoad()
        {
            editorBuildAttempts = 0;
            EditorApplication.delayCall -= BuildIfSceneLoadedInEditor;
            EditorApplication.delayCall += BuildIfSceneLoadedInEditor;
        }

        // 这个函数处理 BuildIfSceneLoadedInEditor 相关逻辑。
        private void BuildIfSceneLoadedInEditor()
        {
            EditorApplication.delayCall -= BuildIfSceneLoadedInEditor;

            if (this == null || Application.isPlaying || !buildOnAwake)
            {
                return;
            }

            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                editorBuildAttempts++;
                if (editorBuildAttempts < MaxEditorBuildAttempts)
                {
                    EditorApplication.delayCall += BuildIfSceneLoadedInEditor;
                }

                return;
            }

            BuildIfMissing();
        }
#endif

        // 这个函数在当前场景还没有新版方块布局时，生成 Level2 地图。
        /// <summary>
        /// Builds the Level2 layout unless the current block-style layout already exists.
        /// </summary>
        public void BuildIfMissing()
        {
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                return;
            }

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

        // 这个函数创建完整的 Level2_LootTutorial 地图和原型玩法对象。
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

        // 这个函数创建命名根节点，用来整理生成出来的 Level2 对象层级。
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

        // 这个函数创建基础方向光，让场景里的物体能被照亮。
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

        // 这个函数创建从左到右的地面方块，以及宝箱后的小危险坑。
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

        // 这个函数创建 PlayerStart 标记，供 EchoEscapeGameManager 复活玩家时使用。
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

        // 这个函数创建可操控玩家，并挂上移动、录制和攻击脚本。
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

        // 这个函数创建摄像机，让它跟随玩家通过第二个教程房间。
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

            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            CameraFollow follow = cameraObject.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = cameraObject.AddComponent<CameraFollow>();
            }

            follow.target = target;
            follow.offset = new Vector3(2f, 1.4f, -10f);
            follow.followSpeed = 5f;
        }

        // 这个函数检查当前场景里是否已经有新版方块风格生成布局。
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

        // 这个函数在重新生成干净的方块布局前，先删除旧版生成的 Level2 对象。
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

        // 这个函数从当前引导对象所在场景里删除指定名字的根对象。
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

        // 这个函数创建样式标记，用来识别当前 Level2 是否已经是新版方块原型布局。
        /// <summary>
        /// Creates the marker used to recognize the current Level2 block prototype style.
        /// </summary>
        /// <param name="parent">Generated Level2 root transform.</param>
        private static void CreateStyleMarker(Transform parent)
        {
            GameObject marker = new GameObject(StyleMarkerName);
            marker.transform.SetParent(parent, false);
        }

        // 这个函数创建攻击教学用的简单方块敌人。
        /// <summary>
        /// Creates the simple block enemy used by the attack tutorial.
        /// </summary>
        /// <param name="parent">Root object used to organize the enemy.</param>
        private static void CreateEnemyBlock(Transform parent)
        {
            GameObject enemy = CreateBlock("Enemy_Block", new Vector2(-1.25f, -1.65f), new Vector2(0.8f, 0.65f), new Color(0.72f, 0.18f, 0.82f), false, parent);
            enemy.AddComponent<SimpleEnemy>();
        }

        // 这个函数创建宝箱生成点，供 GameManager 生成教学宝箱。
        /// <summary>
        /// Creates the marker used by the Game Manager to spawn the tutorial chest.
        /// </summary>
        /// <param name="parent">Root object used to organize the chest marker.</param>
        private static void CreateChestSpawn(Transform parent)
        {
            GameObject marker = new GameObject("Chest_Block_Spawn");
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(4.65f, -1.72f, 0f);
            marker.AddComponent<ChestSpawnPoint>();
        }

        // 这个函数在宝箱后小坑下方创建红色危险触发区。
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

        // 这个函数创建 Level2 出口触发区，玩家进入后结束演示而不是加载不存在的下一关。
        /// <summary>
        /// Creates the Level2 exit trigger. It completes the demo instead of loading a missing next scene.
        /// </summary>
        /// <param name="parent">Root object used to organize the exit.</param>
        private static void CreateExit(Transform parent)
        {
            GameObject exit = CreateBlock("Exit_Level2", new Vector2(13.9f, -1.45f), new Vector2(0.85f, 1.55f), new Color(0.1f, 0.9f, 0.55f, 0.85f), false, parent);
            exit.AddComponent<GoalZone>();
        }

        // 这个函数创建 Level2 的 GameManager、奖励表和 HUD。
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
            manager.useLootFeedbackUi = true;
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

        // 这个函数创建一个教学问号触发区和它的可见图标。
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

        // 这个函数创建 Level2 问号提示使用的深色教学弹窗 UI。
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

        // 这个函数确保场景里有 EventSystem，让 UI 能接收输入。
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

        // 这个函数创建一个带 RectTransform 的 UI 对象。
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

        // 这个函数为教学弹窗创建一个 UI Text 文字对象。
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

        // 这个函数创建一个简单有颜色、带 2D 碰撞器的方块。
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

        // 这个函数根据当前是在编辑器还是运行时，使用正确的 Unity 删除方法销毁对象。
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
