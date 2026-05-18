using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class EchoEscapeLevelBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string PrefabsPath = "Assets/Prefabs";
    private const string SpritesPath = "Assets/Sprites";

    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Level1ScenePath = "Assets/Scenes/Level1_Tutorial.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level2_EchoPuzzleIntro.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level3_RiskReward.unity";

    private const string ForestBackgroundPath = "Assets/Art/Backgrounds/pure-pixel-forest.png";
    private const string SourcePlatformPath = "Assets/Resources/BrackeysPlatformer/Sprites/platforms.png";
    private const string SourceCoinPath = "Assets/Resources/BrackeysPlatformer/Sprites/coin.png";
    private const string SourceFruitPath = "Assets/Resources/BrackeysPlatformer/Sprites/fruit.png";
    private const string GroundTilePath = SpritesPath + "/Level1_GroundTile.png";
    private const string PlatformTilePath = SpritesPath + "/Level1_PlatformTile.png";
    private const string PlaceholderSpritePath = SpritesPath + "/placeholder_square.png";

    [MenuItem("Echo Escape/Build Prototype Levels")]
    public static void BuildPrototypeLevels()
    {
        EnsureProjectFolders();
        EnsureLevelSprites();
        CreatePlaceholderPrefabs();
        BuildLevel1Tutorial();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Echo Escape Level1_Tutorial rebuilt. Level2 and Level3 were preserved.");
    }

    public static void BuildPrototypeLevelsFromCommandLine()
    {
        BuildPrototypeLevels();
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Sprites");
        EnsureFolder("Assets", "Audio");
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void EnsureLevelSprites()
    {
        EnsureWhitePlaceholderSprite();
        EnsureCroppedSprite(SourcePlatformPath, GroundTilePath, 0);
        EnsureCroppedSprite(SourcePlatformPath, PlatformTilePath, 1);

        ConfigureSpriteImport(ForestBackgroundPath, 100f);
        ConfigureSpriteImport(SourcePlatformPath, 16f);
        ConfigureSpriteImport(SourceCoinPath, 16f);
        ConfigureSpriteImport(SourceFruitPath, 16f);
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

    private static void EnsureCroppedSprite(string sourcePath, string outputPath, int rowFromTop)
    {
        if (!File.Exists(ToDiskPath(sourcePath)))
        {
            return;
        }

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(ToDiskPath(sourcePath)));

        int rows = 4;
        int cropHeight = Mathf.Max(1, source.height / rows);
        int cropY = Mathf.Clamp(source.height - cropHeight * (rowFromTop + 1), 0, source.height - cropHeight);
        Color[] pixels = source.GetPixels(0, cropY, source.width, cropHeight);

        Texture2D cropped = new Texture2D(source.width, cropHeight, TextureFormat.RGBA32, false);
        cropped.SetPixels(pixels);
        cropped.Apply();
        File.WriteAllBytes(ToDiskPath(outputPath), cropped.EncodeToPNG());

        Object.DestroyImmediate(source);
        Object.DestroyImmediate(cropped);

        AssetDatabase.ImportAsset(outputPath);
        ConfigureSpriteImport(outputPath, 16f);
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

    private static void CreatePlaceholderPrefabs()
    {
        Sprite groundSprite = LoadSprite(GroundTilePath);
        Sprite platformSprite = LoadSprite(PlatformTilePath) ?? groundSprite;
        Sprite coinSprite = LoadSprite(SourceCoinPath) ?? groundSprite;

        SaveBlockPrefab("PlayerStart", LoadSprite(SourceFruitPath) ?? coinSprite, Color.white, true, 12);
        SaveBlockPrefab("Exit", coinSprite, new Color(0.72f, 1f, 0.72f), true, 12);
        SaveBlockPrefab("DoorPlaceholder", groundSprite, new Color(0.86f, 0.22f, 0.18f), false, 10);
        SaveBlockPrefab("PressurePlatePlaceholder", platformSprite, new Color(1f, 0.86f, 0.18f), true, 11);
        SaveBlockPrefab("ChestPlaceholder", coinSprite, new Color(1f, 0.68f, 0.18f), true, 11);
        SaveBlockPrefab("HazardPlaceholder", groundSprite, new Color(0.9f, 0.08f, 0.12f), true, 11);
        SaveBlockPrefab("DeathZone", LoadSprite(PlaceholderSpritePath), new Color(0.75f, 0.05f, 0.08f, 0.25f), true, -5);
        SaveBlockPrefab("GroundBlock", groundSprite, Color.white, false, 4);
        SaveBlockPrefab("PlatformBlock", platformSprite, Color.white, false, 5);
    }

    private static void SaveBlockPrefab(string name, Sprite sprite, Color color, bool isTrigger, int sortingOrder)
    {
        GameObject root = new GameObject(name);

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = Vector2.one;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = isTrigger;
        collider.size = Vector2.one;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabsPath + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildLevel1Tutorial()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Vector3(2.1f, 0.15f, -10f), 6.25f);
        CreateLight();

        Transform root = CreateRoot("Level1_Tutorial_Map");
        CreateForestBackground(root);

        Transform groundRoot = CreateRoot("Ground");
        groundRoot.SetParent(root, false);
        CreatePlatform("Ground_Start_Runway", new Vector2(-8.6f, -2.65f), new Vector2(7.2f, 1.05f), groundRoot, true);
        CreatePlatform("Ground_Before_Pit", new Vector2(-2.1f, -2.65f), new Vector2(4.2f, 1.05f), groundRoot, true);
        CreatePlatform("Ground_After_Pit", new Vector2(4.8f, -2.65f), new Vector2(5.8f, 1.05f), groundRoot, true);
        CreatePlatform("Ground_Exit_Runway", new Vector2(10.9f, -2.65f), new Vector2(5.8f, 1.05f), groundRoot, true);

        Transform platformRoot = CreateRoot("Platforms");
        platformRoot.SetParent(root, false);
        CreatePlatform("Platform_Jump_01", new Vector2(-4.2f, -1.25f), new Vector2(3.0f, 0.45f), platformRoot, false);
        CreatePlatform("Platform_Jump_02", new Vector2(-0.9f, -0.2f), new Vector2(2.8f, 0.45f), platformRoot, false);
        CreatePlatform("Platform_Jump_03_High", new Vector2(2.35f, 0.85f), new Vector2(2.8f, 0.45f), platformRoot, false);
        CreatePlatform("Platform_After_Pit_Recovery", new Vector2(6.55f, -0.85f), new Vector2(3.4f, 0.45f), platformRoot, false);

        CreateMarker("PlayerStart", LoadSprite(SourceFruitPath), new Vector2(-10.45f, -1.72f), new Vector2(0.75f, 0.9f), root, new Color(0.65f, 0.9f, 1f), true);
        CreateDeathZone(root);
        CreateExit(root);

        CreateQuestionMark("TutorialQuestionMark_Move", new Vector2(-9.45f, -1.05f), root);
        CreateQuestionMark("TutorialQuestionMark_Jump", new Vector2(-4.2f, -0.35f), root);
        CreateQuestionMark("TutorialQuestionMark_Exit", new Vector2(13.15f, -0.85f), root);

        SaveScene(scene, Level1ScenePath);
    }

    private static void CreateCamera(Vector3 position, float orthographicSize)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = position;

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.backgroundColor = new Color(0.48f, 0.68f, 0.72f);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
    }

    private static void CreateForestBackground(Transform parent)
    {
        Sprite backgroundSprite = LoadSprite(ForestBackgroundPath);
        GameObject background = new GameObject("Background");
        background.transform.SetParent(parent, false);
        background.transform.position = new Vector3(2.1f, 0.25f, 1.25f);

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = -50;
        renderer.color = Color.white;

        if (backgroundSprite != null)
        {
            Vector2 targetSize = new Vector2(27.5f, 13.2f);
            Vector2 spriteSize = backgroundSprite.bounds.size;
            float scale = Mathf.Max(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y);
            background.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            renderer.sprite = LoadSprite(PlaceholderSpritePath);
            renderer.color = new Color(0.18f, 0.28f, 0.30f);
            background.transform.localScale = new Vector3(27.5f, 13.2f, 1f);
        }
    }

    private static void CreatePlatform(string name, Vector2 position, Vector2 size, Transform parent, bool ground)
    {
        GameObject block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(ground ? GroundTilePath : PlatformTilePath);
        renderer.sortingOrder = ground ? 4 : 5;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;

        BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = size;
    }

    private static void CreateDeathZone(Transform parent)
    {
        GameObject deathZone = new GameObject("DeathZone");
        deathZone.transform.SetParent(parent, false);
        deathZone.transform.position = new Vector3(1.4f, -3.95f, 0f);

        SpriteRenderer renderer = deathZone.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlaceholderSpritePath);
        renderer.color = new Color(0.85f, 0.04f, 0.08f, 0.22f);
        renderer.sortingOrder = 2;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(3.6f, 0.55f);

        BoxCollider2D collider = deathZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(3.6f, 0.55f);
    }

    private static void CreateExit(Transform parent)
    {
        GameObject exit = new GameObject("Exit");
        exit.transform.SetParent(parent, false);
        exit.transform.position = new Vector3(13.25f, -1.62f, 0f);

        SpriteRenderer renderer = exit.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(SourceCoinPath) ?? LoadSprite(PlaceholderSpritePath);
        renderer.color = new Color(0.65f, 1f, 0.68f);
        renderer.sortingOrder = 12;
        renderer.drawMode = SpriteDrawMode.Simple;
        exit.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1.45f);
    }

    private static void CreateMarker(string name, Sprite sprite, Vector2 position, Vector2 size, Transform parent, Color color, bool trigger)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = new Vector3(position.x, position.y, 0f);
        marker.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite ?? LoadSprite(PlaceholderSpritePath);
        renderer.color = color;
        renderer.sortingOrder = 12;

        BoxCollider2D collider = marker.AddComponent<BoxCollider2D>();
        collider.isTrigger = trigger;
    }

    private static void CreateQuestionMark(string name, Vector2 position, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = new Vector3(position.x, position.y, -0.05f);

        SpriteRenderer coin = marker.AddComponent<SpriteRenderer>();
        coin.sprite = LoadSprite(SourceCoinPath) ?? LoadSprite(PlaceholderSpritePath);
        coin.color = new Color(1f, 0.9f, 0.25f);
        coin.sortingOrder = 13;
        marker.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        GameObject question = new GameObject("QuestionMarkIcon");
        question.transform.SetParent(marker.transform, false);
        question.transform.localPosition = new Vector3(0f, 0.03f, -0.05f);

        TextMesh text = question.AddComponent<TextMesh>();
        text.text = "?";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 32;
        text.characterSize = 0.12f;
        text.color = new Color(0.09f, 0.07f, 0.03f);
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
