using EchoEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Echo Escape/Build Prototype Scene")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("Prototype Level");

        Transform spawn = CreateMarker("Player Spawn", new Vector2(-7.8f, -1.2f), root.transform).transform;

        EchoEscapeGameManager manager = new GameObject("Game Manager").AddComponent<EchoEscapeGameManager>();
        manager.gameObject.AddComponent<TutorialDirector>();
        manager.gameObject.AddComponent<PrototypeAudio>();
        PrototypeVisualSkinner skinner = manager.gameObject.AddComponent<PrototypeVisualSkinner>();
        manager.playerSpawn = spawn;
        manager.chestsPerRun = 2;

        GameObject player = CreatePlayer(spawn.position);
        manager.player = player.GetComponent<PlayerController2D>();
        manager.recorder = player.GetComponent<ActionRecorder>();

        CreateCamera(player.transform);
        new GameObject("Prototype HUD").AddComponent<PrototypeHud>();

        CreatePlatform("Ground", new Vector2(0f, -2.6f), new Vector2(18f, 0.6f), new Color(0.18f, 0.2f, 0.24f), root.transform);
        CreatePlatform("Left Training Ledge", new Vector2(-5.7f, -0.55f), new Vector2(2.6f, 0.35f), new Color(0.24f, 0.28f, 0.34f), root.transform);
        CreatePlatform("Chest Ledge", new Vector2(4.9f, -0.55f), new Vector2(2.6f, 0.35f), new Color(0.24f, 0.28f, 0.34f), root.transform);
        CreatePlatform("Exit Platform", new Vector2(7.8f, -1.55f), new Vector2(2.2f, 0.35f), new Color(0.24f, 0.28f, 0.34f), root.transform);

        Door door = PrototypeFactory.CreateBlock("Echo Door", new Vector2(1.8f, -1.3f), new Vector2(0.45f, 2.6f), new Color(0.85f, 0.18f, 0.14f), true, root.transform).AddComponent<Door>();

        GameObject plateObject = PrototypeFactory.CreateBlock("Pressure Plate", new Vector2(-2.4f, -2.16f), new Vector2(1.15f, 0.18f), new Color(1f, 0.85f, 0.15f), false, root.transform);
        PressurePlate plate = plateObject.AddComponent<PressurePlate>();
        plate.linkedDoor = door;

        GameObject spikePit = PrototypeFactory.CreateBlock("Spike Hazard", new Vector2(4.45f, -2.16f), new Vector2(1.4f, 0.25f), new Color(1f, 0.2f, 0.18f), false, root.transform);
        HazardZone hazard = spikePit.AddComponent<HazardZone>();
        hazard.deathReason = "fell into the red hazard";

        GameObject goal = PrototypeFactory.CreateBlock("Extraction Exit", new Vector2(8.35f, -0.95f), new Vector2(0.75f, 1.25f), new Color(0.1f, 0.9f, 0.55f), false, root.transform);
        goal.AddComponent<GoalZone>();

        CreateChestSpawn("Chest Spawn A", new Vector2(-6.1f, -0.05f), root.transform);
        CreateChestSpawn("Chest Spawn B", new Vector2(-0.7f, -2.05f), root.transform);
        CreateChestSpawn("Chest Spawn C", new Vector2(4.9f, -0.05f), root.transform);
        CreateChestSpawn("Chest Spawn D", new Vector2(6.1f, -2.05f), root.transform);

        CreateLabel("Record yourself standing on this plate", new Vector2(-3.2f, -1.55f), root.transform);
        CreateLabel("Door opens while the plate is held", new Vector2(0.9f, 0.35f), root.transform);
        CreateLabel("Random chests: loot is lost if you die", new Vector2(3.5f, 0.15f), root.transform);
        CreateLabel("Exit banks your loot", new Vector2(6.9f, 0.25f), root.transform);

        skinner.SkinAll();

        EditorSceneManager.SaveScene(scene, ScenePath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }
        else
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void BuildFromCommandLine()
    {
        BuildScene();
    }

    private static GameObject CreatePlayer(Vector3 position)
    {
        GameObject player = new GameObject("Player Knight");
        player.transform.position = position;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 2.4f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.65f, 1.8f);
        collider.offset = new Vector2(0f, -0.15f);

        player.AddComponent<PlayerController2D>();
        player.AddComponent<ActionRecorder>();

        PixelCharacterVisual visual = player.AddComponent<PixelCharacterVisual>();
        visual.SetStyle(false, Color.white);

        return player;
    }

    private static void CreateCamera(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.2f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = target.position + new Vector3(0f, 1.25f, -10f);

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.target = target;
    }

    private static GameObject CreatePlatform(string name, Vector2 position, Vector2 size, Color color, Transform parent)
    {
        return PrototypeFactory.CreateBlock(name, position, size, color, true, parent);
    }

    private static void CreateChestSpawn(string name, Vector2 position, Transform parent)
    {
        GameObject marker = CreateMarker(name, position, parent);
        marker.AddComponent<ChestSpawnPoint>();
    }

    private static GameObject CreateMarker(string name, Vector2 position, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = new Vector3(position.x, position.y, 0f);
        return marker;
    }

    private static void CreateLabel(string text, Vector2 position, Transform parent)
    {
        GameObject labelObject = new GameObject("Label - " + text);
        labelObject.transform.SetParent(parent);
        labelObject.transform.position = new Vector3(position.x, position.y, -0.2f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 28;
        label.characterSize = 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(0.85f, 0.88f, 0.92f);
    }
}
