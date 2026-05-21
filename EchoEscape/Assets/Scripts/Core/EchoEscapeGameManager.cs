using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private readonly List<LootDefinition> securedLoot = new List<LootDefinition>();

        /// <summary>
        /// Unity event method called when the Game Manager is created.
        /// </summary>
        /// <remarks>
        /// Sets the singleton reference, ensures audio/visual helper components exist, and creates fallback loot data.
        /// </remarks>
        private void Awake()
        {
            Instance = this;
            EnsurePresentationServices();

            if (lootTable == null || lootTable.Length == 0)
            {
                lootTable = new[]
                {
                    new LootDefinition("Scrap Token", "Common", 55),
                    new LootDefinition("Repair Kit", "Uncommon", 25),
                    new LootDefinition("Echo Shard", "Rare", 15),
                    new LootDefinition("Vault Core", "Legendary", 5)
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

            Tutorial = GetComponent<TutorialDirector>();
            if (Tutorial == null)
            {
                Tutorial = gameObject.AddComponent<TutorialDirector>();
            }

            SpawnRandomChests();
            VisualSkinner?.SkinAll();
            UpdateStatus("Tutorial started. Learn record, replay, loot risk, and extraction.");
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Handles the R key scene restart shortcut for quick playtesting.
        /// </remarks>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
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
            UpdateStatus($"Found {loot.rarity} loot: {loot.itemName}. Escape safely or lose it on death.");
        }

        /// <summary>
        /// Selects one loot entry using the weighted loot table.
        /// </summary>
        /// <returns>The selected LootDefinition.</returns>
        public LootDefinition RollLoot()
        {
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
        /// Handles player death, clears pending loot, removes active Echo, and respawns the player.
        /// </summary>
        /// <param name="reason">Short text explaining why the player died.</param>
        public void KillPlayer(string reason)
        {
            if (HasWon)
            {
                return;
            }

            DeathCount++;
            AudioService?.PlayHurt();
            int lostCount = pendingLoot.Count;
            pendingLoot.Clear();

            if (recorder != null)
            {
                recorder.DestroyActiveEcho();
            }

            if (player != null && playerSpawn != null)
            {
                player.Respawn(playerSpawn.position);
            }

            string lossText = lostCount > 0 ? $" Lost {lostCount} unbanked loot item(s)." : string.Empty;
            UpdateStatus($"You died: {reason}.{lossText}");
        }

        /// <summary>
        /// Completes the run and moves pending loot into secured loot.
        /// </summary>
        /// <remarks>
        /// Called by GoalZone when the player reaches the exit trigger.
        /// </remarks>
        public void Win()
        {
            if (HasWon)
            {
                return;
            }

            HasWon = true;
            securedLoot.AddRange(pendingLoot);
            int securedThisRun = pendingLoot.Count;
            pendingLoot.Clear();
            AudioService?.PlaySuccess();
            UpdateStatus($"Extraction complete. Secured {securedThisRun} loot item(s). Press R to replay.");
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
            if (VisualSkinner == null)
            {
                VisualSkinner = gameObject.AddComponent<PrototypeVisualSkinner>();
            }
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

                GameObject chestObject = PrototypeFactory.CreateBlock("Random Loot Chest", point.transform.position, new Vector2(0.75f, 0.55f), new Color(1f, 0.74f, 0.22f), false);
                Chest chest = chestObject.AddComponent<Chest>();
                chest.spawnPoint = point;
            }
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
        /// Rarity label shown in status messages.
        /// </summary>
        public string rarity;

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
            this.itemName = itemName;
            this.rarity = rarity;
            this.weight = weight;
        }
    }
}
