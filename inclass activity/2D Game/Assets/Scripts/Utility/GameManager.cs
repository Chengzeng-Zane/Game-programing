using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Class which manages the game
/// </summary>
public class GameManager : MonoBehaviour
{
    // The script that manages all others
    public static GameManager instance = null;

    [Tooltip("The player gameobject")]
    public GameObject player = null;

    [Header("Scores")]
    // The current player score in the game
    [Tooltip("The player's score")]
    [SerializeField] private int gameManagerScore = 0;

    // Static getter/setter for player score (for convenience)
    public static int score
    {
        get
        {
            if (instance == null)
            {
                return PlayerPrefs.GetInt("score", 0);
            }
            return instance.gameManagerScore;
        }
        set
        {
            if (instance != null)
            {
                instance.gameManagerScore = value;
            }
            else
            {
                PlayerPrefs.SetInt("score", value);
            }
        }
    }

    // The highest score obtained by this player
    [Tooltip("The highest score acheived on this device")]
    public int highScore = 0;

    [Header("Game Progress / Victory Settings")]
    [Tooltip("Whether the game is winnable or not \nDefault: true")]
    public bool gameIsWinnable = true;
    [Tooltip("The number of enemies that must be defeated to win the game")]
    public int enemiesToDefeat = 10;
    
    // The number of enemies defeated in game
    private int enemiesDefeated = 0;

    public int EnemiesDefeated
    {
        get
        {
            return enemiesDefeated;
        }
    }

    [Tooltip("Whether or not to print debug statements about whether the game can be won or not according to the game manager's" +
        " search at start up")]
    public bool printDebugOfWinnableStatus = true;
    [Tooltip("Page index in the UIManager to go to on winning the game")]
    public int gameVictoryPageIndex = 0;
    [Tooltip("The effect to create upon winning the game")]
    public GameObject victoryEffect;

    //The number of enemies observed by the game manager in this scene at start up"
    private int numberOfEnemiesFoundAtStart;

    [Header("HUD Improvement")]
    [Tooltip("Creates a compact gameplay HUD that always shows score, lives, objective progress, and power-up status.")]
    public bool createImprovedHud = true;
    [Tooltip("Screen position for the improved HUD, measured from the top-left corner.")]
    public Vector2 improvedHudOffset = new Vector2(24, -24);
    [Tooltip("Text color used by the improved HUD.")]
    public Color improvedHudTextColor = Color.white;

    private Canvas improvedHudCanvas;
    private TextMeshProUGUI improvedHudText;
    private TextMeshProUGUI livesHudText;
    private TextMeshProUGUI objectiveHudText;
    private TextMeshProUGUI powerUpHudText;
    private string hudMessage = "";
    private float hudMessageUntil = 0f;

    [Header("Rapid Fire Power-Up")]
    [Tooltip("Spawns a pickup that temporarily lowers the player's fire cooldown.")]
    public bool enableRapidFirePowerUps = true;
    [Tooltip("Delay before the first rapid fire pickup appears.")]
    public float firstPowerUpDelay = 6f;
    [Tooltip("Delay between rapid fire pickup spawns.")]
    public float powerUpSpawnInterval = 18f;
    [Tooltip("How long rapid fire lasts after pickup.")]
    public float rapidFireDuration = 6f;
    [Tooltip("Multiplier applied to the player's fire rate. Lower values fire faster.")]
    [Range(0.1f, 1f)] public float rapidFireRateMultiplier = 0.35f;
    [Tooltip("Maximum distance from the player where a pickup can appear.")]
    public float powerUpSpawnRadius = 7f;
    [Tooltip("How long a pickup remains available if the player does not collect it.")]
    public float powerUpLifetime = 12f;

    private float nextPowerUpSpawnTime = Mathf.Infinity;
    private bool rapidFireActive = false;
    private float rapidFireEndsAt = 0f;
    private float normalPlayerFireRate = 0f;
    private ShootingController playerShootingController;
    private Sprite rapidFireSprite;
    private GameObject rapidFirePowerUpPrefab;

    /// <summary>
    /// Description:
    /// Standard Unity function called when the script is loaded, called before start
    /// 
    /// When this component is first added or activated, setup the global reference
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            DestroyImmediate(this);
        }

        if ((player == null) && (FindObjectOfType<Controller>() != null))
        {
            player = FindObjectOfType<Controller>().gameObject;
        }
        else if ((player == null) && (SceneManager.GetActiveScene().name!="MainMenu"))
        {
            Debug.Log("Player is not set and cannot find it in the scene. This is not a problem in non-playable scenes, such as the Main Menu.");
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first Update
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void Start()
    {
        HandleStartUp();
        SetUpImprovedHud();
        ScheduleNextPowerUp(firstPowerUpDelay);
        SetHudMessage("Power-Up: Find pickup", 4f);
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once per frame.
    /// Updates the added HUD and checks the rapid fire power-up feature.
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void Update()
    {
        if (!IsGameplayScene())
        {
            return;
        }

        UpdateRapidFirePowerUp();
        TrySpawnRapidFirePowerUp();
        UpdateImprovedHud();
    }

    /// <summary>
    /// Description:
    /// Handles necessary activities on start up such as getting the highscore and score, updating UI elements, 
    /// and checking the number of enemies
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    void HandleStartUp()
    {
        if (PlayerPrefs.HasKey("highscore"))
        {
            highScore = PlayerPrefs.GetInt("highscore");
        }
        if (PlayerPrefs.HasKey("score"))
        {
            score = PlayerPrefs.GetInt("score");
        }
        UpdateUIElements();
        if (printDebugOfWinnableStatus)
        {
            FigureOutHowManyEnemiesExist();
        }
    }

    /// <summary>
    /// Description:
    /// Searches the level for all spawners and static enemies.
    /// Only produces debug messages / warnings if the game is set to be winnable
    /// If there are any infinite spawners a debug message will say so,
    /// If there are more enemies than the number of enemies to defeat to win
    /// then a debug message will say so
    /// If there are too few enemies to defeat to win then a debug warning will say so
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void FigureOutHowManyEnemiesExist()
    {
        List<EnemySpawner> enemySpawners = FindObjectsOfType<EnemySpawner>().ToList();
        List<Enemy> staticEnemies = FindObjectsOfType<Enemy>().ToList();

        int numberOfInfiniteSpawners = 0;
        int enemiesFromSpawners = 0;
        int enemiesFromStatic = staticEnemies.Count;
        foreach(EnemySpawner enemySpawner in enemySpawners)
        {
            if (enemySpawner.spawnInfinite)
            {
                numberOfInfiniteSpawners += 1;
            }
            else
            {
                enemiesFromSpawners += enemySpawner.maxSpawn;
            }
        }
        numberOfEnemiesFoundAtStart = enemiesFromSpawners + enemiesFromStatic;

        if (gameIsWinnable)
        {
            if (numberOfInfiniteSpawners > 0)
            {
                Debug.Log("There are " + numberOfInfiniteSpawners + " infinite spawners " + " so the level will always be winnable, "
                    + "\nhowever you sshould still playtest for timely completion");
            }
            else if (enemiesToDefeat > numberOfEnemiesFoundAtStart)
            {
                Debug.LogWarning("There are " + enemiesToDefeat + " enemies to defeat but only " + numberOfEnemiesFoundAtStart + 
                    " enemies found at start \nThe level can not be completed!");
            }
            else
            {
                Debug.Log("There are " + enemiesToDefeat + " enemies to defeat and " + numberOfEnemiesFoundAtStart +
                    " enemies found at start \nThe level can completed");
            }
        }
    }

    /// <summary>
    /// Description:
    /// Increments the number of enemies defeated by 1
    /// Input:
    /// none
    /// Return:
    /// void (no returned value)
    /// </summary>
    public void IncrementEnemiesDefeated()
    {
        enemiesDefeated++;
        if (enemiesDefeated >= enemiesToDefeat && gameIsWinnable)
        {
            LevelCleared();
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function that gets called when the application (or playmode) ends
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveHighScore();
        ResetScore();
    }

    /// <summary>
    /// Description:
    /// Adds a number to the player's score stored in the gameManager
    /// Input: 
    /// int scoreAmount
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="scoreAmount">The amount to add to the score</param>
    public static void AddScore(int scoreAmount)
    {
        score += scoreAmount;
        int currentHighScore = instance != null ? instance.highScore : PlayerPrefs.GetInt("highscore", 0);
        if (score > currentHighScore)
        {
            SaveHighScore();
        }
        UpdateUIElements();
    }
    
    /// <summary>
    /// Description:
    /// Resets the current player score
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public static void ResetScore()
    {
        PlayerPrefs.SetInt("score", 0);
        if (instance != null)
        {
            instance.gameManagerScore = 0;
        }
        UpdateUIElements();
    }

    /// <summary>
    /// Description:
    /// Saves the player's highscore
    /// Input: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public static void SaveHighScore()
    {
        int currentScore = score;
        int currentHighScore = instance != null ? instance.highScore : PlayerPrefs.GetInt("highscore", 0);

        if (currentScore > currentHighScore)
        {
            PlayerPrefs.SetInt("highscore", currentScore);
            if (instance != null)
            {
                instance.highScore = currentScore;
            }
        }
        UpdateUIElements();
    }

    /// <summary>
    /// Description:
    /// Resets the high score in player preferences
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public static void ResetHighScore()
    {
        PlayerPrefs.SetInt("highscore", 0);
        if (instance != null)
        {
            instance.highScore = 0;
        }
        UpdateUIElements();
    }

    /// <summary>
    /// Description:
    /// Sends out a message to UI elements to update
    /// Input: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public static void UpdateUIElements()
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateUI();
        }
    }

    /// <summary>
    /// Description:
    /// Ends the level, meant to be called when the level is complete (enough enemies have been defeated)
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void LevelCleared()
    {
        gameIsOver = true;
        PlayerPrefs.SetInt("score", score);
        SetHudMessage("Status: Clear!", 10f);
        RestoreNormalFireRate();
        SetImprovedHudVisible(false);
        if (UIManager.instance != null)
        {
            if (player != null)
            {
                player.SetActive(false);
            }
            UIManager.instance.allowPause = false;
            UIManager.instance.GoToPage(gameVictoryPageIndex);
            if (victoryEffect != null)
            {
                Instantiate(victoryEffect, transform.position, transform.rotation, null);
            }
        }     
    }

    [Header("Game Over Settings:")]
    [Tooltip("The index in the UI manager of the game over page")]
    public int gameOverPageIndex = 0;
    [Tooltip("The game over effect to create when the game is lost")]
    public GameObject gameOverEffect;

    // Whether or not the game is over
    [HideInInspector]
    public bool gameIsOver = false;

    /// <summary>
    /// Description:
    /// Displays game over screen
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public void GameOver()
    {
        gameIsOver = true;
        SetHudMessage("Status: Game Over", 10f);
        RestoreNormalFireRate();
        SetImprovedHudVisible(false);
        if (gameOverEffect != null)
        {
            Instantiate(gameOverEffect, transform.position, transform.rotation, null);
        }
        if (UIManager.instance != null)
        {
            UIManager.instance.allowPause = false;
            UIManager.instance.GoToPage(gameOverPageIndex);
        }
    }

    /// <summary>
    /// Description:
    /// Shows or hides the improved gameplay HUD.
    /// Inputs:
    /// bool visible
    /// Returns:
    /// void (no return)
    /// </summary>
    private void SetImprovedHudVisible(bool visible)
    {
        SetHudTextVisible(livesHudText, visible);
        SetHudTextVisible(objectiveHudText, visible);
        SetHudTextVisible(powerUpHudText, visible);
    }

    /// <summary>
    /// Description:
    /// Safely changes visibility for one added HUD text element.
    /// Inputs:
    /// TextMeshProUGUI textElement, bool visible
    /// Returns:
    /// void (no return)
    /// </summary>
    private void SetHudTextVisible(TextMeshProUGUI textElement, bool visible)
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Description:
    /// Returns true when this scene is a playable level with an active player reference.
    /// Inputs:
    /// none
    /// Returns:
    /// bool
    /// </summary>
    private bool IsGameplayScene()
    {
        return SceneManager.GetActiveScene().name != "MainMenu" && player != null;
    }

    /// <summary>
    /// Description:
    /// Creates a small runtime HUD so the player can always see score, lives, goal progress, and power-up state.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void SetUpImprovedHud()
    {
        if (!createImprovedHud || !IsGameplayScene() || objectiveHudText != null)
        {
            return;
        }

        TextMeshProUGUI scoreText = FindHudTextByName("Score text");
        TextMeshProUGUI highScoreText = FindHudTextByName("High Score");
        livesHudText = FindHudTextByName("Lives Text");
        objectiveHudText = FindHudTextByName("Objective Text");
        powerUpHudText = FindHudTextByName("Power-Up Text");
        if (powerUpHudText == null)
        {
            powerUpHudText = FindHudTextByName("Bonus Text");
        }

        if (livesHudText != null && objectiveHudText != null && powerUpHudText != null)
        {
            improvedHudText = powerUpHudText;
            UpdateImprovedHud();
            return;
        }

        TextMeshProUGUI templateText = highScoreText != null ? highScoreText : scoreText;

        if (templateText == null)
        {
            Debug.LogWarning("Improved HUD could not find the existing score text to copy its style.");
            return;
        }

        Transform hudParent = templateText.transform.parent;
        RectTransform baseTransform = highScoreText != null
            ? highScoreText.GetComponent<RectTransform>()
            : templateText.GetComponent<RectTransform>();

        Vector2 basePosition = baseTransform.anchoredPosition;
        float lineHeight = 45f;

        livesHudText = CreateMatchingHudText("Lives Text", templateText, hudParent, baseTransform, basePosition + new Vector2(0, -lineHeight));
        objectiveHudText = CreateMatchingHudText("Objective Text", templateText, hudParent, baseTransform, basePosition + new Vector2(0, -lineHeight * 2f));
        powerUpHudText = CreateMatchingHudText("Power-Up Text", templateText, hudParent, baseTransform, basePosition + new Vector2(0, -lineHeight * 3f));
        improvedHudText = powerUpHudText;

        UpdateImprovedHud();
    }

    /// <summary>
    /// Description:
    /// Updates the runtime HUD text with the newest score, lives, objective, and bonus status.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void UpdateImprovedHud()
    {
        if (livesHudText == null || objectiveHudText == null || powerUpHudText == null)
        {
            return;
        }

        string objectiveText = gameIsWinnable
            ? "Goal: " + enemiesDefeated + "/" + enemiesToDefeat + " kills"
            : "Goal: Survive";

        livesHudText.text = GetPlayerSurvivalText();
        objectiveHudText.text = objectiveText;
        powerUpHudText.text = GetPowerUpHudText();
    }

    /// <summary>
    /// Description:
    /// Finds an existing HUD text by name, including scene objects created from prefabs.
    /// Inputs:
    /// string textObjectName
    /// Returns:
    /// TextMeshProUGUI
    /// </summary>
    private TextMeshProUGUI FindHudTextByName(string textObjectName)
    {
        foreach (TextMeshProUGUI textElement in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (textElement.name == textObjectName && textElement.gameObject.scene.IsValid())
            {
                return textElement;
            }
        }

        return null;
    }

    /// <summary>
    /// Description:
    /// Creates a HUD text element that copies the existing score text style.
    /// Inputs:
    /// string objectName, TextMeshProUGUI templateText, Transform parent, RectTransform templateTransform, Vector2 anchoredPosition
    /// Returns:
    /// TextMeshProUGUI
    /// </summary>
    private TextMeshProUGUI CreateMatchingHudText(string objectName, TextMeshProUGUI templateText, Transform parent, RectTransform templateTransform, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.layer = templateText.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = templateTransform.anchorMin;
        textTransform.anchorMax = templateTransform.anchorMax;
        textTransform.pivot = templateTransform.pivot;
        textTransform.anchoredPosition = anchoredPosition;
        textTransform.sizeDelta = new Vector2(260, templateTransform.sizeDelta.y);
        textTransform.localScale = templateTransform.localScale;

        TextMeshProUGUI newText = textObject.AddComponent<TextMeshProUGUI>();
        newText.font = templateText.font;
        newText.fontSharedMaterial = templateText.fontSharedMaterial;
        newText.fontSize = templateText.fontSize;
        newText.fontStyle = templateText.fontStyle;
        newText.color = templateText.color;
        newText.alignment = templateText.alignment;
        newText.enableWordWrapping = false;
        newText.raycastTarget = false;

        return newText;
    }

    /// <summary>
    /// Description:
    /// Gets a readable lives or health line for the improved HUD.
    /// Inputs:
    /// none
    /// Returns:
    /// string
    /// </summary>
    private string GetPlayerSurvivalText()
    {
        if (player == null)
        {
            return "Lives: 0";
        }

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth == null)
        {
            return "Lives: ?";
        }

        if (playerHealth.useLives)
        {
            return "Lives: " + Mathf.Max(playerHealth.currentLives, 0);
        }

        return "HP: " + Mathf.Max(playerHealth.currentHealth, 0) + "/" + playerHealth.maximumHealth;
    }

    /// <summary>
    /// Description:
    /// Gets a readable power-up line for the improved HUD.
    /// Inputs:
    /// none
    /// Returns:
    /// string
    /// </summary>
    private string GetPowerUpHudText()
    {
        if (rapidFireActive)
        {
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(rapidFireEndsAt - Time.time, 0f));
            return "Power-Up: Rapid " + secondsLeft + "s";
        }

        if (Time.time < hudMessageUntil)
        {
            return hudMessage;
        }

        return "Power-Up: Waiting";
    }

    /// <summary>
    /// Description:
    /// Shows a short HUD message without interrupting gameplay.
    /// Inputs:
    /// string message, float seconds
    /// Returns:
    /// void (no return)
    /// </summary>
    private void SetHudMessage(string message, float seconds)
    {
        hudMessage = message;
        hudMessageUntil = Time.time + seconds;
        UpdateImprovedHud();
    }

    /// <summary>
    /// Description:
    /// Sets the next rapid fire pickup spawn time.
    /// Inputs:
    /// float delay
    /// Returns:
    /// void (no return)
    /// </summary>
    private void ScheduleNextPowerUp(float delay)
    {
        if (enableRapidFirePowerUps && IsGameplayScene())
        {
            nextPowerUpSpawnTime = Time.time + delay;
        }
    }

    /// <summary>
    /// Description:
    /// Spawns the rapid fire pickup when the timer is ready.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void TrySpawnRapidFirePowerUp()
    {
        if (!enableRapidFirePowerUps || gameIsOver || Time.timeScale <= 0 || Time.time < nextPowerUpSpawnTime)
        {
            return;
        }

        SpawnRapidFirePowerUp();
        ScheduleNextPowerUp(powerUpSpawnInterval);
    }

    /// <summary>
    /// Description:
    /// Creates a rapid fire pickup near the player.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void SpawnRapidFirePowerUp()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        float randomDistance = Random.Range(2.5f, powerUpSpawnRadius);
        Vector3 spawnPosition = player.transform.position + (Vector3)(randomDirection * randomDistance);
        spawnPosition.z = 0f;

        GameObject powerUpObject = CreateRapidFirePowerUpObject(spawnPosition);
        RapidFirePowerUpPickup pickup = powerUpObject.GetComponent<RapidFirePowerUpPickup>();
        if (pickup == null)
        {
            pickup = powerUpObject.AddComponent<RapidFirePowerUpPickup>();
        }
        pickup.SetUp(this, rapidFireDuration);

        Destroy(powerUpObject, powerUpLifetime);
        SetHudMessage("Power-Up: Pick up!", 3f);
    }

    /// <summary>
    /// Description:
    /// Creates the rapid fire pickup from a visible Resources prefab if it exists.
    /// Falls back to code creation if the prefab has not been generated yet.
    /// Inputs:
    /// Vector3 spawnPosition
    /// Returns:
    /// GameObject
    /// </summary>
    private GameObject CreateRapidFirePowerUpObject(Vector3 spawnPosition)
    {
        if (rapidFirePowerUpPrefab == null)
        {
            rapidFirePowerUpPrefab = Resources.Load<GameObject>("PowerUps/RapidFirePowerUp");
        }

        if (rapidFirePowerUpPrefab != null)
        {
            return Instantiate(rapidFirePowerUpPrefab, spawnPosition, Quaternion.identity, null);
        }

        GameObject powerUpObject = new GameObject("Rapid Fire Power-Up");
        powerUpObject.transform.position = spawnPosition;

        SpriteRenderer spriteRenderer = powerUpObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetRapidFireSprite();
        spriteRenderer.sortingOrder = 50;

        CircleCollider2D pickupCollider = powerUpObject.AddComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        pickupCollider.radius = 0.55f;

        powerUpObject.AddComponent<RapidFirePowerUpPickup>();
        return powerUpObject;
    }

    /// <summary>
    /// Description:
    /// Generates the rapid fire pickup icon at runtime so no external art asset is needed.
    /// Inputs:
    /// none
    /// Returns:
    /// Sprite
    /// </summary>
    private Sprite GetRapidFireSprite()
    {
        if (rapidFireSprite != null)
        {
            return rapidFireSprite;
        }

        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outer = new Color(1f, 0.75f, 0.1f, 1f);
        Color inner = new Color(0.3f, 0.95f, 1f, 1f);
        Color bolt = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float centeredX = (x - size * 0.5f) / (size * 0.5f);
                float centeredY = (y - size * 0.5f) / (size * 0.5f);
                float distance = Mathf.Sqrt(centeredX * centeredX + centeredY * centeredY);

                Color pixelColor = clear;
                if (distance <= 0.92f)
                {
                    pixelColor = Color.Lerp(inner, outer, distance);
                }

                bool inBolt =
                    centeredY > -0.78f &&
                    centeredY < 0.78f &&
                    centeredX > -0.12f - centeredY * 0.22f &&
                    centeredX < 0.18f - centeredY * 0.22f;

                bool inLowerBolt =
                    centeredY < 0.05f &&
                    centeredY > -0.7f &&
                    centeredX > -0.28f - centeredY * 0.45f &&
                    centeredX < -0.02f - centeredY * 0.45f;

                if (inBolt || inLowerBolt)
                {
                    pixelColor = bolt;
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        rapidFireSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return rapidFireSprite;
    }

    /// <summary>
    /// Description:
    /// Applies the rapid fire effect to the player's shooting controller.
    /// Inputs:
    /// float duration
    /// Returns:
    /// void (no return)
    /// </summary>
    public void ActivateRapidFirePowerUp(float duration)
    {
        if (playerShootingController == null && player != null)
        {
            playerShootingController = player.GetComponentInChildren<ShootingController>();
        }

        if (playerShootingController == null)
        {
            Debug.LogWarning("Rapid Fire was collected, but no ShootingController was found on the player.");
            return;
        }

        if (!rapidFireActive)
        {
            normalPlayerFireRate = playerShootingController.fireRate;
            rapidFireActive = true;
        }

        rapidFireEndsAt = Mathf.Max(rapidFireEndsAt, Time.time) + duration;
        playerShootingController.fireRate = Mathf.Max(0.01f, normalPlayerFireRate * rapidFireRateMultiplier);
        SetHudMessage("Power-Up: Rapid!", 2.5f);
    }

    /// <summary>
    /// Description:
    /// Ends rapid fire when the timer expires.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void UpdateRapidFirePowerUp()
    {
        if (rapidFireActive && Time.time >= rapidFireEndsAt)
        {
            RestoreNormalFireRate();
            SetHudMessage("Power-Up: Ended", 2f);
        }
    }

    /// <summary>
    /// Description:
    /// Restores the player's original shooting cooldown.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void RestoreNormalFireRate()
    {
        if (rapidFireActive && playerShootingController != null)
        {
            playerShootingController.fireRate = normalPlayerFireRate;
        }

        rapidFireActive = false;
        rapidFireEndsAt = 0f;
    }
}
