using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoVault
{
    public class EchoVaultGameManager : MonoBehaviour
    {
        public static EchoVaultGameManager Instance { get; private set; }

        [Header("Scene References")]
        public PlayerController2D player;
        public ActionRecorder recorder;
        public Transform playerSpawn;

        [Header("Loot")]
        public int chestsPerRun = 2;
        public LootDefinition[] lootTable;

        public string StatusMessage { get; private set; } = "Reach the exit. Use your echo to hold the plate.";
        public int DeathCount { get; private set; }
        public bool HasWon { get; private set; }
        public TutorialDirector Tutorial { get; private set; }
        public PrototypeAudio AudioService { get; private set; }
        public PrototypeVisualSkinner VisualSkinner { get; private set; }
        public IReadOnlyList<LootDefinition> PendingLoot => pendingLoot;
        public IReadOnlyList<LootDefinition> SecuredLoot => securedLoot;
        public int PendingLootCount => pendingLoot.Count;
        public int SecuredLootCount => securedLoot.Count;

        private readonly List<LootDefinition> pendingLoot = new List<LootDefinition>();
        private readonly List<LootDefinition> securedLoot = new List<LootDefinition>();

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

        public void AddPendingLoot(LootDefinition loot)
        {
            pendingLoot.Add(loot);
            UpdateStatus($"Found {loot.rarity} loot: {loot.itemName}. Escape safely or lose it on death.");
        }

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

    [System.Serializable]
    public struct LootDefinition
    {
        public string itemName;
        public string rarity;
        public int weight;

        public LootDefinition(string itemName, string rarity, int weight)
        {
            this.itemName = itemName;
            this.rarity = rarity;
            this.weight = weight;
        }
    }
}
