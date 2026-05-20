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

public static class EchoEscapeLevelBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string SpritesPath = "Assets/Sprites";

    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Level1ScenePath = "Assets/Scenes/Level1_Tutorial.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level2_EchoPuzzleIntro.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level3_RiskReward.unity";

    private const string PlaceholderSpritePath = SpritesPath + "/placeholder_square.png";

    [MenuItem("Echo Escape/Build Prototype Levels")]
    public static void BuildPrototypeLevels()
    {
        EnsureProjectFolders();
        EnsureTag("Echo");
        EnsureWhitePlaceholderSprite();
        BuildLevel1Tutorial();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Echo Escape Level1_Tutorial rebuilt as a clean first tutorial segment. Level2 and Level3 were preserved.");
    }

    public static void BuildPrototypeLevelsFromCommandLine()
    {
        BuildPrototypeLevels();
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets", "Sprites");
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

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

    private static void BuildLevel1Tutorial()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Transform root = CreateRoot("Level1_Tutorial_CleanStart");
        CreateLight();
        CreateGroundAndJumpPractice(root);
        CreateRecordPuzzleArea(root);

        Transform playerStart = CreatePlayerStart(root);
        PlayerController2D player = CreatePlayer(playerStart.position);
        CreateCamera(player.transform);
        TutorialPopupManager popupManager = CreateTutorialPopupUi();
        CreateQuestionMarkJump(root, popupManager);
        CreateQuestionMarkRecord(root, popupManager);

        SaveScene(scene, Level1ScenePath);
    }

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

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.75f;
    }

    private static void CreateGroundAndJumpPractice(Transform parent)
    {
        GameObject groundRoot = new GameObject("Ground");
        groundRoot.transform.SetParent(parent, false);

        CreateGroundBlock("Ground_Main", new Vector2(-4f, -2.5f), new Vector2(6f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_Step_01", new Vector2(1f, -1.8f), new Vector2(2f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_Step_02", new Vector2(5f, -1.35f), new Vector2(4f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_Record_Safe", new Vector2(10f, -1.35f), new Vector2(4f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_Record_Puzzle", new Vector2(14f, -1.35f), new Vector2(4f, 0.8f), groundRoot.transform);
        CreateGroundBlock("Ground_After_Door", new Vector2(18.5f, -1.35f), new Vector2(4f, 0.8f), groundRoot.transform);
    }

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

    private static Transform CreatePlayerStart(Transform parent)
    {
        GameObject start = new GameObject("PlayerStart");
        start.transform.SetParent(parent, false);
        start.transform.position = new Vector3(-6f, -1.65f, 0f);

        SpriteRenderer renderer = start.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.color = new Color(0.2f, 0.72f, 1f, 0.75f);
        renderer.sortingOrder = 3;
        renderer.drawMode = SpriteDrawMode.Simple;
        start.transform.localScale = new Vector3(0.28f, 1.0f, 1f);

        return start.transform;
    }

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

        playerObject.AddComponent<ActionRecorder>();

        SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.color = new Color(0.88f, 0.94f, 1f);
        renderer.sortingOrder = 5;
        playerObject.transform.localScale = new Vector3(0.65f, 1.35f, 1f);

        return controller;
    }

    private static void CreateQuestionMarkJump(Transform parent, TutorialPopupManager popupManager)
    {
        CreateQuestionMark(
            "QuestionMark_Jump",
            new Vector2(-2.3f, -1.55f),
            "Jump",
            "Press Space to jump.\nUse A/D or Left/Right Arrow to move.\nJump onto the next platform to continue.",
            popupManager,
            parent);
    }

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

    private static void CreateRecordPuzzleArea(Transform parent)
    {
        GameObject puzzleRoot = new GameObject("RecordPuzzle");
        puzzleRoot.transform.SetParent(parent, false);

        Door door = CreateDoor("Door_RecordPuzzle", new Vector2(15.8f, 0.15f), new Vector2(0.55f, 2.6f), puzzleRoot.transform);
        PressurePlate pressurePlate = CreatePressurePlate("PressurePlate_RecordPuzzle", new Vector2(13.1f, -0.78f), puzzleRoot.transform);
        pressurePlate.linkedDoor = door;

        CreateExit("Exit", new Vector2(19.6f, -0.12f), puzzleRoot.transform);
    }

    private static Door CreateDoor(string name, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject doorObject = CreateSpriteBlock(name, position, size, new Color(0.85f, 0.18f, 0.14f), false, 4, parent);
        Door door = doorObject.AddComponent<Door>();
        door.closedColor = new Color(0.85f, 0.18f, 0.14f);
        door.openColor = new Color(0.12f, 0.75f, 0.32f, 0.45f);
        return door;
    }

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

    private static void CreateExit(string name, Vector2 position, Transform parent)
    {
        GameObject exitObject = CreateSpriteBlock(name, position, new Vector2(0.8f, 1.45f), new Color(0.1f, 0.9f, 0.55f, 0.85f), true, 4, parent);
        exitObject.AddComponent<GoalZone>();
    }

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
        textObject.transform.localPosition = new Vector3(0f, 0.03f, -0.05f);

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
        panelRect.sizeDelta = new Vector2(700f, 250f);

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
            new Vector2(640f, 138f),
            Color.white);

        Text closeText = CreateUiText(
            "CloseHintText",
            panelObject.transform,
            "Press Esc to close.",
            16,
            FontStyle.Normal,
            new Vector2(0f, -216f),
            new Vector2(640f, 24f),
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

    private static void EnsureEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name);
        uiObject.transform.SetParent(parent, false);
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

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

    private static Font LoadUiFont()
    {
        Font pixelFont = Resources.Load<Font>("BrackeysPlatformer/Fonts/PixelOperator8-Bold");
        return pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static Transform CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        return root.transform;
    }

    private static void SaveScene(Scene scene, string scenePath)
    {
        EditorSceneManager.SaveScene(scene, scenePath);
    }

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

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static string ToDiskPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }
}
