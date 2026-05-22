using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Represents a loot chest that can be opened once by the player.
    /// </summary>
    /// <remarks>
    /// Attach this script to chest objects spawned by EchoEscapeGameManager.
    /// PlayerController2D calls Open when the player presses F near the chest,
    /// then the manager rolls loot and marks it as pending until the player reaches the exit.
    /// </remarks>
    public class Chest : MonoBehaviour
    {
        /// <summary>
        /// Spawn marker that created this chest.
        /// </summary>
        public ChestSpawnPoint spawnPoint;

        /// <summary>
        /// True after this chest has already rewarded loot.
        /// </summary>
        public bool IsOpened { get; private set; }

        private bool playerInRange;
        private PlayerController2D nearbyPlayer;
        private int lastOpenFrame = -1;

        /// <summary>
        /// Unity event method called when the chest is created.
        /// </summary>
        /// <remarks>
        /// Ensures the chest has a trigger collider large enough for the player to stand beside it and press F.
        /// </remarks>
        private void Awake()
        {
            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider2D>();
            }

            trigger.isTrigger = true;
            trigger.size = new Vector2(1.5f, 1.1f);
            trigger.offset = Vector2.zero;
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Opens the chest when the real player is inside the trigger area and presses F.
        /// </remarks>
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F))
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (!playerInRange)
            {
                Debug.Log("F pressed, but player is not in chest range");
                return;
            }

            Debug.Log("F pressed near chest");
            Open();
        }

        /// <summary>
        /// Unity physics event called when another 2D collider enters this chest trigger.
        /// </summary>
        /// <param name="other">The collider that entered the chest range.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController2D player = GetPlayer(other);
            if (player == null)
            {
                return;
            }

            nearbyPlayer = player;
            playerInRange = true;
            Debug.Log("Player entered chest range");
        }

        /// <summary>
        /// Unity physics event called when another 2D collider leaves this chest trigger.
        /// </summary>
        /// <param name="other">The collider that left the chest range.</param>
        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController2D player = GetPlayer(other);
            if (player == null || player != nearbyPlayer)
            {
                return;
            }

            nearbyPlayer = null;
            playerInRange = false;
            Debug.Log("Player left chest range");
        }

        /// <summary>
        /// Opens the chest, rolls loot, and adds the result to pending loot.
        /// </summary>
        /// <remarks>
        /// Called by this chest when the player presses F in range. PlayerController2D can also call it as a fallback.
        /// </remarks>
        public void Open()
        {
            if (IsOpened)
            {
                if (lastOpenFrame == Time.frameCount)
                {
                    return;
                }

                Debug.Log("Chest already opened");
                return;
            }

            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("Chest cannot open because no EchoEscapeGameManager exists in the scene.");
                return;
            }

            IsOpened = true;
            lastOpenFrame = Time.frameCount;
            LootDefinition loot = manager.RollLoot();
            manager.AddPendingLoot(loot);
            manager.AudioService?.PlayChest();
            Debug.Log("Chest opened");
            Debug.Log($"Loot found: {loot.itemName} ({loot.rarity})");
            PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));

            TutorialDirector tutorial = manager.Tutorial;
            if (tutorial != null)
            {
                tutorial.NotifyChestOpened();
            }
        }

        /// <summary>
        /// Checks whether a collider belongs to the controllable player.
        /// </summary>
        /// <param name="other">Collider entering or leaving the chest trigger.</param>
        /// <returns>The PlayerController2D if the collider belongs to the player; otherwise null.</returns>
        private static PlayerController2D GetPlayer(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player != null)
            {
                return player;
            }

            if (other.CompareTag("Player"))
            {
                return other.GetComponent<PlayerController2D>();
            }

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player"))
            {
                return root.GetComponentInChildren<PlayerController2D>();
            }

            return null;
        }
    }
}
