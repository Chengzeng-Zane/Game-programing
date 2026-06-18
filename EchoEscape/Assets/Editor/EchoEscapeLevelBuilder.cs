using System.IO;
using System.Linq;
using EchoEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the current Echo Escape prototype level scenes from editor menu commands.
/// </summary>
/// <remarks>
/// This is an Editor-only utility script.
/// Use the Unity menu item Echo Escape/Build Prototype Levels to keep the art-authored
/// level scenes in the build and update Build Settings.
/// </remarks>
public static class EchoEscapeLevelBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string SpritesPath = "Assets/Sprites";

    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Level1ScenePath = "Assets/Scenes/Level 1 - The First Echo.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level 2 - Relics of the Forest.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level 3 - Escape from the Silent Forest.unity";
    private const string Level2SceneName = "Level 2 - Relics of the Forest";

    private const string PlaceholderSpritePath = SpritesPath + "/placeholder_square.png";

    /// <summary>
    /// Rebuilds the prototype level setup used by the current project.
    /// </summary>
    /// <remarks>
    /// Called from the Unity editor menu. It preserves the official art-authored level scenes.
    /// </remarks>
    [MenuItem("Echo Escape/Build Prototype Levels")]
    public static void BuildPrototypeLevels()
    {
        EnsureProjectFolders();
        EnsureTag("Echo");
        EnsureWhitePlaceholderSprite();
        EnsureOfficialLevel1SceneExists();
        EnsureOfficialLevel2SceneExists();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Echo Escape prototype level scenes checked and preserved. Build Settings were updated.");
    }

    /// <summary>
    /// Command-line entry point for automated scene generation.
    /// </summary>
    /// <remarks>
    /// This wrapper lets Unity batch mode call the same builder used by the editor menu.
    /// </remarks>
    public static void BuildPrototypeLevelsFromCommandLine()
    {
        BuildPrototypeLevels();
    }

    /// <summary>
    /// Command-line entry point for validating the official Level 1 - The First Echo scene.
    /// </summary>
    /// <remarks>
    /// Level 1 - The First Echo is now an art-authored scene, so this command does not overwrite it.
    /// </remarks>
    public static void BuildLevel1TutorialFromCommandLine()
    {
        EnsureProjectFolders();
        EnsureTag("Echo");
        EnsureOfficialLevel1SceneExists();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Official Level 1 - The First Echo scene checked and preserved.");
    }

    /// <summary>
    /// Confirms the official art-authored Level 1 - The First Echo scene exists before build settings are updated.
    /// </summary>
    /// <remarks>
    /// The old procedural Level1 builder is intentionally no longer invoked for the official first level,
    /// because it would overwrite the dark forest Ruby character version stored in Level 1 - The First Echo.unity.
    /// </remarks>
    private static void EnsureOfficialLevel1SceneExists()
    {
        if (!File.Exists(ToDiskPath(Level1ScenePath)))
        {
            Debug.LogError("Official Level 1 - The First Echo scene is missing. Restore Assets/Scenes/Level 1 - The First Echo.unity before building prototype levels.");
        }
    }

    /// <summary>
    /// Confirms the official art-authored Level 2 - Relics of the Forest scene exists before build settings are updated.
    /// </summary>
    /// <remarks>
    /// Level 2 - Relics of the Forest is now an art-authored scene, so the old procedural Level2 bootstrap is no longer invoked.
    /// </remarks>
    private static void EnsureOfficialLevel2SceneExists()
    {
        if (!File.Exists(ToDiskPath(Level2ScenePath)))
        {
            Debug.LogError("Official Level 2 - Relics of the Forest scene is missing. Restore Assets/Scenes/Level 2 - Relics of the Forest.unity before building prototype levels.");
        }
    }

    /// <summary>
    /// Ensures the project folders needed by the generated level exist.
    /// </summary>
    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets", "Sprites");
    }

    /// <summary>
    /// Creates a Unity asset folder if it does not already exist.
    /// </summary>
    /// <param name="parent">Parent folder path inside Assets.</param>
    /// <param name="folder">Folder name to create under the parent.</param>
    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    /// <summary>
    /// Ensures a Unity tag exists in ProjectSettings/TagManager.asset.
    /// </summary>
    /// <param name="tag">Tag name that should exist, such as Echo.</param>
    /// <remarks>
    /// The Echo tag lets PressurePlate recognize spawned Echo objects by tag in addition to component checks.
    /// </remarks>
    private static void EnsureTag(string tag)
    {
        Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0)
        {
            return;
        }

        SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    /// <summary>
    /// Creates and imports the white placeholder square sprite used by generated prototype objects.
    /// </summary>
    private static void EnsureWhitePlaceholderSprite()
    {
        string diskPath = ToDiskPath(PlaceholderSpritePath);
        if (!File.Exists(diskPath))
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = Enumerable.Repeat(Color.white, 16 * 16).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(diskPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        ConfigureSpriteImport(PlaceholderSpritePath, 16f);
    }

    /// <summary>
    /// Configures a texture asset so Unity imports it as a crisp pixel-art sprite.
    /// </summary>
    /// <param name="assetPath">Unity asset path to the texture.</param>
    /// <param name="pixelsPerUnit">Pixels-per-unit value assigned to the sprite importer.</param>
    private static void ConfigureSpriteImport(string assetPath, float pixelsPerUnit)
    {
        if (!File.Exists(ToDiskPath(assetPath)))
        {
            return;
        }

        AssetDatabase.ImportAsset(assetPath);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Creates the Level 1 - The First Echo scene with movement, jump, record, Echo, pressure plate, door, and exit objects.
    /// </summary>
    private static void BuildLevel1Tutorial()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Transform root = CreateRoot("Level 1 - The First Echo_CleanStart");
        CreateLight();
        CreateGroundAndJumpPractice(root);
        CreateRecordPuzzleArea(root);
        CreateGravityTutorialArea(root);

        Transform playerStart = CreatePlayerStart(root);
        PlayerController2D player = CreatePlayer(playerStart.position);
        CreateCamera(player.transform);
        TutorialPopupManager popupManager = CreateTutorialPopupUi();
        CreateQuestionMarkJump(root, popupManager);
        CreateQuestionMarkRecord(root, popupManager);
        CreateQuestionMarkGravity(root, popupManager);

        SaveScene(scene, Level1ScenePath);
    }

    /// <summary>
    /// Creates the main camera and attaches CameraFollow to track the player.
    /// </summary>
    /// <param name="target">Transform the camera should follow.</param>
    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = target.position + new Vector3(2f, 1.4f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.75f;
        camera.backgroundColor = new Color(0.02f, 0.02f, 0.025f);

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.target = target;
        follow.offset = new Vector3(2f, 1.4f, -10f);
        follow.followSpeed = 5f;
    }

    /// <summary>
    /// Adds a basic directional light to the generated scene.
    /// </summary>
    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.75f;
    }

    /// <summary>
    /// Builds the simple block-based ground and jump practice route.
    /// </summary>
    /// <param name="parent">Root transform that owns the ground objects.</param>
    private static void CreateGroundAndJumpPractice(Transform parent)
    {
        GameObject groundRoot = new GameObject("Ground");
        groundRoot.transform.SetParent(parent, false);

        CreateGroundBlock("Ground_Main", new Vector2(-4f, -2.5f), new Vector2(6f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_Step_01", new Vector2(1f, -1.8f), new Vector2(2f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_Step_02", new Vector2(5f, -1.35f), new Vector2(4f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_RecordArea", new Vector2(11.5f, -1.35f), new Vector2(9f, 0.8f), groundRoot.transform);
        CreateGroundBlock("SafeGround_AfterDoor", new Vector2(20.2f, -1.35f), new Vector2(8.4f, 0.8f), groundRoot.transform);
    }

    /// <summary>
    /// Creates one solid ground or platform block.
    /// </summary>
    /// <param name="name">Name of the platform GameObject.</param>
    /// <param name="position">World position of the platform center.</param>
    /// <param name="size">Width and height of the platform.</param>
    /// <param name="parent">Parent transform for scene hierarchy organization.</param>
    private static void CreateGroundBlock(string name, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject ground = new GameObject(name);
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.color = new Color(0.32f, 0.34f, 0.38f);
        renderer.sortingOrder = 1;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = size;
    }

    /// <summary>
    /// Creates the PlayerStart marker used to position the generated player.
    /// </summary>
    /// <param name="parent">Root transform that owns the marker.</param>
    /// <returns>The Transform of the created PlayerStart marker.</returns>
    private static Transform CreatePlayerStart(Transform parent)
    {
        GameObject start = new GameObject("PlayerStart");
        start.transform.SetParent(parent, false);
        start.transform.position = new Vector3(-6f, -1.65f, 0f);

        SpriteRenderer renderer = start.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.enabled = false;
        renderer.color = new Color(0.2f, 0.72f, 1f, 0.75f);
        renderer.sortingOrder = 3;
        renderer.drawMode = SpriteDrawMode.Simple;
        start.transform.localScale = new Vector3(0.28f, 1.0f, 1f);

        return start.transform;
    }

    /// <summary>
    /// Creates the controllable player object and attaches movement and recording scripts.
    /// </summary>
    /// <param name="startPosition">Position of the PlayerStart marker.</param>
    /// <returns>The PlayerController2D attached to the generated player.</returns>
    private static PlayerController2D CreatePlayer(Vector3 startPosition)
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.tag = "Player";
        playerObject.transform.position = new Vector3(startPosition.x, startPosition.y + 0.35f, 0f);

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

        playerObject.AddComponent<GravityFlipController>();
        playerObject.AddComponent<ActionRecorder>();

        SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.color = new Color(0.88f, 0.94f, 1f);
        renderer.sortingOrder = 5;
        playerObject.transform.localScale = new Vector3(0.65f, 1.35f, 1f);

        return controller;
    }

    /// <summary>
    /// Creates the first question mark prompt for jump controls.
    /// </summary>
    /// <param name="parent">Root transform that owns the prompt.</param>
    /// <param name="popupManager">Popup manager that displays the prompt content.</param>
    private static void CreateQuestionMarkJump(Transform parent, TutorialPopupManager popupManager)
    {
        CreateQuestionMark(
            "QuestionMark_Jump",
            new Vector2(-3.4f, -2.45f),
            "Jump",
            "Press Space to jump.\nUse A/D or Left/Right Arrow to move.\nJump onto the next platform to continue.",
            popupManager,
            parent);
    }

    /// <summary>
    /// Creates the Record Yourself question mark prompt.
    /// </summary>
    /// <param name="parent">Root transform that owns the prompt.</param>
    /// <param name="popupManager">Popup manager that displays the prompt content.</param>
    /// <remarks>
    /// This prompt explains the Q/Q/E recording flow without giving every puzzle answer directly.
    /// </remarks>
    private static void CreateQuestionMarkRecord(Transform parent, TutorialPopupManager popupManager)
    {
        CreateQuestionMark(
            "QuestionMark_Record",
            new Vector2(9.4f, -0.3f),
            "Record Yourself",
            "Press Q to start recording.\nPress Q again to stop recording.\nPress E to replay your Echo.\n\nUse your Echo to press buttons and solve puzzles.",
            popupManager,
            parent);
    }

    /// <summary>
    /// Creates the Gravity Flip question mark prompt.
    /// </summary>
    /// <param name="parent">Root transform that owns the prompt.</param>
    /// <param name="popupManager">Popup manager that displays the prompt content.</param>
    private static void CreateQuestionMarkGravity(Transform parent, TutorialPopupManager popupManager)
    {
        CreateQuestionMark(
            "QuestionMark_Gravity",
            new Vector2(20.4f, -0.28f),
            "Gravity Flip",
            "Press Up Arrow to flip gravity upward.\nPress Down Arrow to flip back.",
            popupManager,
            parent);
    }

    /// <summary>
    /// Creates the Level1 pressure plate, door, and exit puzzle section.
    /// </summary>
    /// <param name="parent">Root transform that owns the puzzle section.</param>
    private static void CreateRecordPuzzleArea(Transform parent)
    {
        GameObject puzzleRoot = new GameObject("RecordPuzzle");
        puzzleRoot.transform.SetParent(parent, false);

        Door door = CreateDoor("Door_RecordPuzzle", new Vector2(15.8f, 0.15f), new Vector2(0.55f, 2.6f), puzzleRoot.transform);
        PressurePlate pressurePlate = CreatePressurePlate("PressurePlate_RecordPuzzle", new Vector2(13.1f, -0.78f), puzzleRoot.transform);
        pressurePlate.linkedDoor = door;
    }

    /// <summary>
    /// Creates the Level1 ceiling platform and upper exit for the gravity flip lesson.
    /// </summary>
    /// <param name="parent">Root transform that owns the gravity tutorial objects.</param>
    private static void CreateGravityTutorialArea(Transform parent)
    {
        GameObject gravityRoot = new GameObject("GravityFlipTutorial");
        gravityRoot.transform.SetParent(parent, false);

        CreateGroundBlock("Ground_UpperGravityPlatform", new Vector2(22.9f, 2.55f), new Vector2(5.4f, 0.45f), gravityRoot.transform);

        CreateExit("UpperExit_Level1", new Vector2(24.6f, 2.1f), gravityRoot.transform);
    }

    /// <summary>
    /// Creates a generated door object and attaches the Door script.
    /// </summary>
    /// <param name="name">Name of the door object.</param>
    /// <param name="position">World position of the door.</param>
    /// <param name="size">Width and height of the door block.</param>
    /// <param name="parent">Parent transform for the door.</param>
    /// <returns>The Door component on the generated object.</returns>
    private static Door CreateDoor(string name, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject doorObject = CreateSpriteBlock(name, position, size, new Color(0.85f, 0.18f, 0.14f), false, 4, parent);
        Door door = doorObject.AddComponent<Door>();
        door.closedColor = new Color(0.85f, 0.18f, 0.14f);
        door.openColor = new Color(0.12f, 0.75f, 0.32f, 0.45f);
        return door;
    }

    /// <summary>
    /// Creates a generated pressure plate trigger and attaches PressurePlate.
    /// </summary>
    /// <param name="name">Name of the pressure plate object.</param>
    /// <param name="position">World position of the plate.</param>
    /// <param name="parent">Parent transform for the pressure plate.</param>
    /// <returns>The PressurePlate component on the generated object.</returns>
    private static PressurePlate CreatePressurePlate(string name, Vector2 position, Transform parent)
    {
        GameObject plateObject = CreateSpriteBlock(name, position, new Vector2(1.25f, 0.18f), new Color(1f, 0.85f, 0.15f), true, 5, parent);
        BoxCollider2D trigger = plateObject.GetComponent<BoxCollider2D>();
        if (trigger != null)
        {
            trigger.size = new Vector2(1.5f, 0.75f);
            trigger.offset = new Vector2(0f, 0.25f);
        }

        return plateObject.AddComponent<PressurePlate>();
    }

    /// <summary>
    /// Creates the exit trigger used to complete the level.
    /// </summary>
    /// <param name="name">Name of the exit object.</param>
    /// <param name="position">World position of the exit.</param>
    /// <param name="parent">Parent transform for the exit.</param>
    private static void CreateExit(string name, Vector2 position, Transform parent)
    {
        GameObject exitObject = CreateSpriteBlock(name, position, new Vector2(0.8f, 1.45f), new Color(0.1f, 0.9f, 0.55f, 0.85f), true, 4, parent);
        GoalZone goalZone = exitObject.AddComponent<GoalZone>();
        goalZone.ConfigureNextScene(Level2SceneName);
    }

    /// <summary>
    /// Creates a sprite block with SpriteRenderer and BoxCollider2D.
    /// </summary>
    /// <param name="name">Name of the created block.</param>
    /// <param name="position">World position of the block.</param>
    /// <param name="size">Sprite and collider size.</param>
    /// <param name="color">Sprite color tint.</param>
    /// <param name="trigger">True to make the collider a trigger.</param>
    /// <param name="sortingOrder">SpriteRenderer sorting order.</param>
    /// <param name="parent">Parent transform for the block.</param>
    /// <returns>The created GameObject.</returns>
    private static GameObject CreateSpriteBlock(string name, Vector2 position, Vector2 size, Color color, bool trigger, int sortingOrder, Transform parent)
    {
        GameObject block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;

        BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
        collider.isTrigger = trigger;
        collider.size = size;

        return block;
    }

    /// <summary>
    /// Creates a question mark trigger and its visible icon.
    /// </summary>
    /// <param name="name">Name of the question mark object.</param>
    /// <param name="position">World position of the trigger.</param>
    /// <param name="title">Popup title displayed when triggered.</param>
    /// <param name="message">Popup body displayed when triggered.</param>
    /// <param name="popupManager">Popup manager used to display the message.</param>
    /// <param name="parent">Parent transform for the prompt.</param>
    private static void CreateQuestionMark(string name, Vector2 position, string title, string message, TutorialPopupManager popupManager, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = new Vector3(position.x, position.y, -0.05f);

        BoxCollider2D trigger = marker.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = Vector2.zero;
        trigger.size = new Vector2(0.6f, 0.8f);

        TutorialPopupTrigger popupTrigger = marker.AddComponent<TutorialPopupTrigger>();
        popupTrigger.popupManager = popupManager;
        popupTrigger.tutorialTitle = title;
        popupTrigger.tutorialMessage = message;
        popupTrigger.showOnlyOnce = true;
        popupTrigger.hideAfterUse = false;

        GameObject icon = new GameObject("QuestionMarkIcon");
        icon.transform.SetParent(marker.transform, false);
        icon.transform.localPosition = Vector3.zero;

        SpriteRenderer bubble = icon.AddComponent<SpriteRenderer>();
        bubble.sprite = LoadSprite(PlaceholderSpritePath);
        bubble.color = new Color(1f, 0.86f, 0.16f);
        bubble.sortingOrder = 6;
        icon.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        GameObject textObject = new GameObject("QuestionMarkText");
        textObject.transform.SetParent(marker.transform, false);
        textObject.transform.localPosition = Vector3.zero;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = "?";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 32;
        text.characterSize = 0.1f;
        text.color = Color.black;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = 7;
    }

    /// <summary>
    /// Creates the Canvas, popup panel, and Text components used by question mark prompts.
    /// </summary>
    /// <returns>The TutorialPopupManager configured with generated UI references.</returns>
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
        panelRect.sizeDelta = new Vector2(720f, 280f);

        Text titleText = CreateUiText(
            "TitleText",
            panelObject.transform,
            "Jump",
            30,
            FontStyle.Bold,
            new Vector2(0f, -28f),
            new Vector2(640f, 42f),
            new Color(1f, 0.88f, 0.28f));

        Text bodyText = CreateUiText(
            "BodyText",
            panelObject.transform,
            "Press Space to jump.\nUse A/D or Left/Right Arrow to move.\nJump onto the next platform to continue.",
            18,
            FontStyle.Normal,
            new Vector2(0f, -84f),
            new Vector2(660f, 154f),
            Color.white);

        Text closeText = CreateUiText(
            "CloseHintText",
            panelObject.transform,
            "Press C to close.",
            16,
            FontStyle.Normal,
            new Vector2(0f, -248f),
            new Vector2(660f, 24f),
            new Color(0.72f, 0.78f, 0.88f));

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
    /// Creates a basic EventSystem for generated UI input.
    /// </summary>
    private static void EnsureEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// Creates a UI GameObject with RectTransform.
    /// </summary>
    /// <param name="name">Name of the UI object.</param>
    /// <param name="parent">Parent transform for the UI object.</param>
    /// <returns>The created GameObject.</returns>
    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name);
        uiObject.transform.SetParent(parent, false);
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

    /// <summary>
    /// Creates a UI Text element for the tutorial popup.
    /// </summary>
    /// <param name="name">Name of the text object.</param>
    /// <param name="parent">Parent transform for the text object.</param>
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
        uiText.font = LoadUiFont();
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.color = color;
        uiText.raycastTarget = false;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        return uiText;
    }

    /// <summary>
    /// Loads the preferred pixel UI font or falls back to Arial.
    /// </summary>
    /// <returns>The font used by generated UI text.</returns>
    private static Font LoadUiFont()
    {
        Font pixelFont = Resources.Load<Font>("BrackeysPlatformer/Fonts/PixelOperator8-Bold");
        return pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>
    /// Creates a named root object for the generated scene hierarchy.
    /// </summary>
    /// <param name="name">Name of the root GameObject.</param>
    /// <returns>The root transform.</returns>
    private static Transform CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        return root.transform;
    }

    /// <summary>
    /// Saves the active generated scene to the requested asset path.
    /// </summary>
    /// <param name="scene">Scene instance to save.</param>
    /// <param name="scenePath">Unity asset path where the scene should be saved.</param>
    private static void SaveScene(Scene scene, string scenePath)
    {
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    /// <summary>
    /// Updates Unity Build Settings to include MainMenu and available prototype level scenes.
    /// </summary>
    private static void UpdateBuildSettings()
    {
        string[] scenePaths =
        {
            MainMenuScenePath,
            Level1ScenePath,
            Level2ScenePath,
            Level3ScenePath
        };

        EditorBuildSettings.scenes = scenePaths
            .Where(path => File.Exists(ToDiskPath(path)))
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    /// <summary>
    /// Loads a sprite asset from a Unity asset path.
    /// </summary>
    /// <param name="assetPath">Asset path to a texture or sprite.</param>
    /// <returns>The loaded Sprite, or the first sprite sub-asset if needed.</returns>
    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    /// <summary>
    /// Converts a Unity asset path into an absolute disk path.
    /// </summary>
    /// <param name="assetPath">Unity asset path using forward slashes.</param>
    /// <returns>Absolute path on disk.</returns>
    private static string ToDiskPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }
}
