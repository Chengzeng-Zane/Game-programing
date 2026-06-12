using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// Coordinates the main prototype game state for Echo Escape.
    /// </summary>
    /// <remarks>
    /// Attach this script to a scene-level Game Manager object.
    /// It connects Player, ActionRecorder, TutorialDirector, audio, visual skinning, loot, death, and win state.
    /// It is the central place other scripts call when the player dies, finds loot, or reaches the exit.
    /// </remarks>
    public class EchoEscapeGameManager : MonoBehaviour
    {
        /// <summary>
        /// Global reference to the active game manager in the scene.
        /// </summary>
        public static EchoEscapeGameManager Instance { get; private set; }

        [Header("Scene References")]
        /// <summary>
        /// Player controller currently used in the scene.
        /// </summary>
        public PlayerController2D player;

        /// <summary>
        /// Recorder attached to the player for Q/E Echo playback.
        /// </summary>
        public ActionRecorder recorder;

        /// <summary>
        /// Transform used when respawning the player after death.
        /// </summary>
        public Transform playerSpawn;

        [Header("Loot")]
        /// <summary>
        /// Number of random chests to create from available ChestSpawnPoint markers.
        /// </summary>
        public int chestsPerRun = 2;

        /// <summary>
        /// Weighted loot entries used when a chest is opened.
        /// </summary>
        public LootDefinition[] lootTable;

        [Header("Tutorial / HUD")]
        /// <summary>
        /// If true, the older objective tracker is used by PrototypeHud.
        /// </summary>
        public bool useTutorialDirector = true;

        /// <summary>
        /// If true, prototype blocks are automatically replaced with pixel-art sprites at runtime.
        /// </summary>
        public bool usePrototypeVisualSkinner = true;

        /// <summary>
        /// First status message shown by the HUD when the scene starts.
        /// </summary>
        public string startingStatusMessage = "Tutorial started. Learn record, replay, loot risk, and extraction.";

        /// <summary>
        /// If true, a Canvas-based loot pickup popup and loot summary are shown.
        /// </summary>
        public bool useLootFeedbackUi = true;

        [Header("Death")]
        [SerializeField]
        [FormerlySerializedAs("deathHurtDelay")]
        private float deathAnimationFallbackDelay = 0.8f;

        [SerializeField]
        private float deathReloadDelay = 1f;

        [SerializeField]
        private bool debugDeathFlow;

        /// <summary>
        /// Current short message shown by prototype HUD or status systems.
        /// </summary>
        public string StatusMessage { get; private set; } = "Reach the exit. Use your echo to hold the plate.";

        /// <summary>
        /// Number of player deaths during this run.
        /// </summary>
        public int DeathCount { get; private set; }

        /// <summary>
        /// True after the player has reached the exit.
        /// </summary>
        public bool HasWon { get; private set; }

        /// <summary>
        /// True while the player death sequence is active and the current scene is waiting to reload.
        /// </summary>
        public bool IsPlayerDeadOrDying => deathInProgress;

        /// <summary>
        /// Tutorial state machine attached to the Game Manager.
        /// </summary>
        public TutorialDirector Tutorial { get; private set; }

        /// <summary>
        /// Audio helper used by gameplay scripts.
        /// </summary>
        public PrototypeAudio AudioService { get; private set; }

        /// <summary>
        /// Visual skinning helper used to replace placeholder blocks with pixel art.
        /// </summary>
        public PrototypeVisualSkinner VisualSkinner { get; private set; }

        /// <summary>
        /// Canvas-based loot feedback UI created for prototype scenes.
        /// </summary>
        public LootFeedbackUI LootFeedback { get; private set; }

        /// <summary>
        /// Loot found but not yet secured at the exit.
        /// </summary>
        public IReadOnlyList<LootDefinition> PendingLoot => pendingLoot;

        /// <summary>
        /// Loot successfully banked by reaching the exit.
        /// </summary>
        public IReadOnlyList<LootDefinition> SecuredLoot => securedLoot;

        /// <summary>
        /// Number of currently unbanked loot items.
        /// </summary>
        public int PendingLootCount => pendingLoot.Count;

        /// <summary>
        /// Number of secured loot items.
        /// </summary>
        public int SecuredLootCount => securedLoot.Count;

        private readonly List<LootDefinition> pendingLoot = new List<LootDefinition>();
        private static readonly List<LootDefinition> securedLoot = new List<LootDefinition>();
        private static readonly HashSet<string> scenesWithClaimedChest = new HashSet<string>();
        private const float ChestVisualScale = 0.55f;
        private const float ChestVisualYOffset = -0.4f;
        private const int ChestSortingOrder = 5;
        private static Sprite chestSprite;
        private bool deathInProgress;

        /// <summary>
        /// Unity event method called when the Game Manager is created.
        /// </summary>
        /// <remarks>
        /// Sets the singleton reference, ensures audio/visual helper components exist, and creates fallback loot data.
        /// </remarks>
        private void Awake()
        {
            Instance = this;
            EnsureAudioListener();
            EnsurePresentationServices();

            if (lootTable == null || lootTable.Length == 0)
            {
                lootTable = new[]
                {
                    new LootDefinition("Old Coin", "Common", 60),
                    new LootDefinition("Blue Gem", "Rare", 30),
                    new LootDefinition("Golden Relic", "Epic", 10)
                };
            }
        }

        /// <summary>
        /// Unity event method called before the first frame update.
        /// </summary>
        /// <remarks>
        /// Finds scene references, starts the tutorial, spawns chests, and applies visual skinning.
        /// </remarks>
        private void Start()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController2D>();
            }

            if (recorder == null && player != null)
            {
                recorder = player.GetComponent<ActionRecorder>();
            }

            if (useTutorialDirector)
            {
                Tutorial = GetComponent<TutorialDirector>();
                if (Tutorial == null)
                {
                    Tutorial = gameObject.AddComponent<TutorialDirector>();
                }
            }
            else
            {
                TutorialDirector existingTutorial = GetComponent<TutorialDirector>();
                if (existingTutorial != null)
                {
                    existingTutorial.enabled = false;
                }

                Tutorial = null;
            }

            SpawnRandomChests();
            if (usePrototypeVisualSkinner)
            {
                VisualSkinner?.SkinAll();
            }

            UpdateStatus(startingStatusMessage);
            RefreshLootFeedback();
        }

        /// <summary>
        /// Updates the short gameplay status message displayed by HUD systems.
        /// </summary>
        /// <param name="message">Message to store as the current status.</param>
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

        /// <summary>
        /// Adds newly opened chest loot to the pending loot list.
        /// </summary>
        /// <param name="loot">Loot definition rolled by a chest.</param>
        public void AddPendingLoot(LootDefinition loot)
        {
            pendingLoot.Add(loot);
            UpdateStatus($"Found: {loot.itemName}. Escape safely or lose it on death.");
            RefreshLootFeedback();
            LootFeedback?.ShowLootFound(loot);
            Debug.Log($"Pending loot updated: {loot.itemName}.");
        }

        /// <summary>
        /// Selects one loot entry using the weighted loot table.
        /// </summary>
        /// <returns>The selected LootDefinition.</returns>
        public LootDefinition RollLoot()
        {
            IReadOnlyList<CollectibleItem> collectibles = CollectibleDatabase.GetAllCollectibles();
            if (collectibles.Count > 0)
            {
                return CollectibleDatabase.GetRandomCollectible().ToLootDefinition();
            }

            int totalWeight = 0;
            foreach (LootDefinition loot in lootTable)
            {
                totalWeight += Mathf.Max(1, loot.weight);
            }

            int roll = Random.Range(0, totalWeight);
            foreach (LootDefinition loot in lootTable)
            {
                roll -= Mathf.Max(1, loot.weight);
                if (roll < 0)
                {
                    return loot;
                }
            }

            return lootTable[0];
        }

        /// <summary>
        /// Checks whether the current scene already gave its chest reward this run.
        /// </summary>
        /// <returns>True if a chest reward was already claimed in this scene.</returns>
        public bool HasClaimedChestInCurrentScene()
        {
            return scenesWithClaimedChest.Contains(GetCurrentSceneChestKey());
        }

        /// <summary>
        /// Marks the current scene chest reward as claimed for this run.
        /// </summary>
        public void MarkChestClaimedInCurrentScene()
        {
            scenesWithClaimedChest.Add(GetCurrentSceneChestKey());
        }

        /// <summary>
        /// Clears chest claim tracking when a new run starts.
        /// </summary>
        public static void ResetChestClaimsForNewRun()
        {
            scenesWithClaimedChest.Clear();
        }

        /// <summary>
        /// Moves pending loot into secured loot before an exit or scene transition.
        /// </summary>
        /// <returns>Number of loot items secured.</returns>
        public int SecurePendingLoot()
        {
            if (pendingLoot.Count == 0)
            {
                RefreshLootFeedback();
                return 0;
            }

            List<LootDefinition> securedNow = new List<LootDefinition>(pendingLoot);
            securedLoot.AddRange(securedNow);
            pendingLoot.Clear();

            UpdateStatus($"Loot secured: {FormatLoot(securedNow)}.");
            RefreshLootFeedback();
            LootFeedback?.ShowLootSecured(securedNow);
            Debug.Log($"Loot secured: {FormatLoot(securedNow)}.");
            return securedNow.Count;
        }

        /// <summary>
        /// Handles player death, clears pending loot, removes active Echo, and reloads the current scene.
        /// </summary>
        /// <param name="reason">Short text explaining why the player died.</param>
        public void KillPlayer(string reason)
        {
            if (debugDeathFlow)
            {
                Debug.Log("[DeathFlow] KillPlayer called");
                Debug.Log($"[DeathFlow] reason = {reason}");
                Debug.Log($"[DeathFlow] will respawn/reload = {!HasWon && !deathInProgress}");
            }

            if (HasWon || deathInProgress)
            {
                return;
            }

            deathInProgress = true;
            DeathCount++;
            AudioService?.PlayHurt();
            List<LootDefinition> lostLoot = new List<LootDefinition>(pendingLoot);
            pendingLoot.Clear();

            if (recorder != null)
            {
                recorder.DestroyActiveEcho();
            }

            DisablePlayerForDeath();
            StartCoroutine(HandleDeathSequence(reason, lostLoot));
        }

        /// <summary>
        /// Description:
        /// Runs the death flow after KillPlayer is called.
        /// It waits for the player death animation, shows lost loot, then reloads the scene.
        /// Inputs:
        /// reason - short text explaining why the player died
        /// lostLoot - pending loot that was lost on death
        /// Returns:
        /// IEnumerator - Unity coroutine steps for the death sequence
        /// </summary>
        private IEnumerator HandleDeathSequence(string reason, List<LootDefinition> lostLoot)
        {
            float deathAnimationDuration = PlayPlayerDeathAnimation(reason);
            float deathAnimationWait = Mathf.Clamp(Mathf.Max(deathAnimationFallbackDelay, deathAnimationDuration), 0.6f, 1f);
            yield return new WaitForSecondsRealtime(deathAnimationWait);

            string lossText = lostLoot.Count > 0 ? $" Loot Lost: {FormatLoot(lostLoot)}" : string.Empty;
            UpdateStatus($"You died: {reason}.{lossText}");
            RefreshLootFeedback();
            LootFeedback?.ShowDeath(lostLoot);

            if (lostLoot.Count > 0)
            {
                Debug.Log("Player died. Pending loot lost.");
            }

            yield return ReloadCurrentSceneAfterDeath();
        }

        /// <summary>
        /// Completes the run and moves pending loot into secured loot.
        /// </summary>
        /// <remarks>
        /// Called by GoalZone when the player reaches the exit trigger.
        /// </remarks>
        public void Win()
        {
            if (HasWon || deathInProgress)
            {
                return;
            }

            HasWon = true;
            int securedThisRun = SecurePendingLoot();
            AudioService?.PlaySuccess();
            UpdateStatus(securedThisRun > 0
                ? $"Extraction complete. Secured {securedThisRun} collectible(s)."
                : "Extraction complete. No pending loot to secure.");
            RefreshLootFeedback();
            Debug.Log("Extraction successful. Loot secured.");
        }

        /// <summary>
        /// Ensures audio and visual helper components exist on the Game Manager.
        /// </summary>
        private void EnsurePresentationServices()
        {
            AudioService = GetComponent<PrototypeAudio>();
            if (AudioService == null)
            {
                AudioService = gameObject.AddComponent<PrototypeAudio>();
            }

            VisualSkinner = GetComponent<PrototypeVisualSkinner>();
            if (usePrototypeVisualSkinner && VisualSkinner == null)
            {
                VisualSkinner = gameObject.AddComponent<PrototypeVisualSkinner>();
            }
            else if (!usePrototypeVisualSkinner && VisualSkinner != null)
            {
                VisualSkinner.enabled = false;
                VisualSkinner = null;
            }

            LootFeedback = GetComponent<LootFeedbackUI>();
            if (useLootFeedbackUi)
            {
                if (LootFeedback == null)
                {
                    LootFeedback = gameObject.AddComponent<LootFeedbackUI>();
                }

                LootFeedback.enabled = true;
            }
            else if (LootFeedback != null)
            {
                LootFeedback.enabled = false;
            }
        }

        /// <summary>
        /// Ensures SFX can play in generated scenes that started without an AudioListener.
        /// </summary>
        private static void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>() != null)
            {
                return;
            }

            Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }

        /// <summary>
        /// Pushes the current loot lists into the Canvas feedback UI.
        /// </summary>
        private void RefreshLootFeedback()
        {
            if (useLootFeedbackUi)
            {
                LootFeedback?.RefreshLootState(pendingLoot, securedLoot);
            }
        }

        /// <summary>
        /// Description:
        /// Waits briefly, restores time scale, and reloads the current scene.
        /// Inputs:
        /// none
        /// Returns:
        /// IEnumerator - Unity coroutine steps for scene reload
        /// </summary>
        private IEnumerator ReloadCurrentSceneAfterDeath()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, deathReloadDelay));
            Time.timeScale = 1f;
            string sceneName = SceneManager.GetActiveScene().name;
            LevelIntroSequence.SkipNextIntroForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Description:
        /// Starts the player's death animation if a PlayerAnimationController exists.
        /// Inputs:
        /// reason - short text used for debug information
        /// Returns:
        /// float - animation duration, or 0 if no animation was played
        /// </summary>
        private float PlayPlayerDeathAnimation(string reason)
        {
            if (player == null)
            {
                return 0f;
            }

            PlayerAnimationController animationController = player.GetComponentInChildren<PlayerAnimationController>();
            return animationController != null ? animationController.PlayDeath(reason) : 0f;
        }

        /// <summary>
        /// Description:
        /// Stops player movement and disables player input during the death sequence.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void DisablePlayerForDeath()
        {
            if (player == null)
            {
                return;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            PlayerAttack attack = player.GetComponent<PlayerAttack>();
            if (attack != null)
            {
                attack.enabled = false;
            }

            GravityFlipController gravityFlip = player.GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
                gravityFlip.enabled = false;
            }

            if (recorder != null)
            {
                recorder.enabled = false;
            }

            player.enabled = false;
        }

        /// <summary>
        /// Description:
        /// Converts a loot list into readable text for the death message.
        /// Inputs:
        /// loot - list of loot items to display
        /// Returns:
        /// string - readable loot names and rarity labels
        /// </summary>
        private static string FormatLoot(IReadOnlyList<LootDefinition> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "none";
            }

            List<string> labels = new List<string>(loot.Count);
            for (int i = 0; i < loot.Count; i++)
            {
                labels.Add(GetLootLabel(loot[i]));
            }

            return string.Join(", ", labels);
        }

        private static string GetLootLabel(LootDefinition loot)
        {
            if (string.IsNullOrWhiteSpace(loot.rarity) || loot.rarity == "Collectible")
            {
                return loot.itemName;
            }

            return $"{loot.itemName} [{loot.rarity}]";
        }

        /// <summary>
        /// Spawns random chest objects at a subset of available ChestSpawnPoint markers.
        /// </summary>
        /// <remarks>
        /// Existing chests are removed first so restarting or rebuilding does not duplicate them.
        /// </remarks>
        private void SpawnRandomChests()
        {
            Chest[] existingChests = FindObjectsOfType<Chest>();
            foreach (Chest chest in existingChests)
            {
                Destroy(chest.gameObject);
            }

            if (HasClaimedChestInCurrentScene())
            {
                UpdateStatus("This level's chest has already been claimed this run.");
                return;
            }

            ChestSpawnPoint[] points = FindObjectsOfType<ChestSpawnPoint>();
            if (points.Length == 0)
            {
                return;
            }

            List<ChestSpawnPoint> available = new List<ChestSpawnPoint>(points);
            int spawnCount = Mathf.Min(chestsPerRun, available.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                int index = Random.Range(0, available.Count);
                ChestSpawnPoint point = available[index];
                available.RemoveAt(index);
                point.HideMarkerVisuals();

                GameObject chestObject = CreateChestBlock(point.transform.position, i, spawnCount);
                Chest chest = chestObject.AddComponent<Chest>();
                chest.spawnPoint = point;
            }
        }

        /// <summary>
        /// Creates the visible, non-blocking chest object spawned from a chest marker.
        /// </summary>
        /// <param name="position">World position for the chest.</param>
        /// <param name="index">Index of this chest in the current spawn pass.</param>
        /// <param name="total">Total number of chests spawned this pass.</param>
        /// <returns>The generated chest GameObject.</returns>
        private static GameObject CreateChestBlock(Vector3 position, int index, int total)
        {
            string chestName = total == 1 ? "Chest_Block" : $"Chest_Block_{index + 1}";
            GameObject chestObject = new GameObject(chestName);
            chestObject.transform.position = position;

            GameObject visualObject = new GameObject("ChestVisual");
            visualObject.transform.SetParent(chestObject.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, ChestVisualYOffset, 0f);
            visualObject.transform.localScale = new Vector3(ChestVisualScale, ChestVisualScale, 1f);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetChestSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = ChestSortingOrder;
            visualObject.AddComponent<ChestAnimationController>();

            BoxCollider2D trigger = chestObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            return chestObject;
        }

        /// <summary>
        /// Creates a simple runtime square sprite for generated chests.
        /// </summary>
        /// <returns>A shared square sprite.</returns>
        private static Sprite GetChestSprite()
        {
            if (chestSprite != null)
            {
                return chestSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            float pixelsPerUnit = Mathf.Max(texture.width, texture.height);
            chestSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            chestSprite.name = "RuntimeChestSquare";
            return chestSprite;
        }

        private static string GetCurrentSceneChestKey()
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetRunChestState()
        {
            ResetChestClaimsForNewRun();
        }
    }

    /// <summary>
    /// Defines one possible loot item and its weighted random chance.
    /// </summary>
    /// <remarks>
    /// EchoEscapeGameManager uses this serializable struct as entries in the loot table.
    /// </remarks>
    [System.Serializable]
    public struct LootDefinition
    {
        /// <summary>
        /// Display name of the loot item.
        /// </summary>
        public string itemName;

        /// <summary>
        /// Stable collectible identifier.
        /// </summary>
        public string id;

        /// <summary>
        /// Rarity label shown in status messages.
        /// </summary>
        public string rarity;

        /// <summary>
        /// Short collectible description shown in UI.
        /// </summary>
        public string description;

        /// <summary>
        /// Optional collectible icon shown in UI.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Relative random weight used by RollLoot.
        /// </summary>
        public int weight;

        /// <summary>
        /// Creates a loot table entry.
        /// </summary>
        /// <param name="itemName">Display name of the item.</param>
        /// <param name="rarity">Rarity label for the item.</param>
        /// <param name="weight">Relative random selection weight.</param>
        public LootDefinition(string itemName, string rarity, int weight)
        {
            id = itemName;
            this.itemName = itemName;
            this.rarity = rarity;
            description = string.Empty;
            icon = null;
            this.weight = weight;
        }

        /// <summary>
        /// Creates a collectible loot entry.
        /// </summary>
        /// <param name="id">Stable collectible identifier.</param>
        /// <param name="itemName">Display name of the collectible.</param>
        /// <param name="description">Short collectible description.</param>
        /// <param name="icon">Optional collectible icon.</param>
        /// <param name="weight">Relative random selection weight.</param>
        public LootDefinition(string id, string itemName, string description, Sprite icon, int weight)
        {
            this.id = id;
            this.itemName = itemName;
            rarity = "Collectible";
            this.description = description;
            this.icon = icon;
            this.weight = weight;
        }
    }
}
