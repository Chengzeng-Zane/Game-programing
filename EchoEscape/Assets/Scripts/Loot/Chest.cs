using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Represents a loot chest that can be opened once by the player.
    /// </summary>
    /// <remarks>
    /// Attach this script to chest objects spawned by EchoEscapeGameManager.
    /// PlayerController2D calls this chest once when F is pressed nearby, then the manager rolls
    /// loot, marks it as pending, and shows the loot feedback UI.
    /// </remarks>
    public class Chest : MonoBehaviour
    {
        private const float InteractionTriggerWorldWidth = 1.05f;
        private const float InteractionTriggerHeight = 0.9f;

        /// <summary>
        /// Spawn marker that created this chest.
        /// </summary>
        public ChestSpawnPoint spawnPoint;

        [SerializeField]
        private bool debugLogs = false;

        [SerializeField]
        private ChestAnimationController visual;

        /// <summary>
        /// True after this chest has already rewarded loot.
        /// </summary>
        public bool IsOpened => isOpened;

        /// <summary>
        /// True while the opening animation is still playing.
        /// </summary>
        public bool IsOpening => isOpening;

        private bool playerInRange;
        private bool isOpening;
        private bool isOpened;

        /// <summary>
        /// Unity event method called when the chest is created.
        /// </summary>
        /// <remarks>
        /// Ensures the chest has a trigger collider large enough for the player to stand beside it and press F.
        /// </remarks>
        private void Awake()
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (Collider2D collider2D in colliders)
            {
                collider2D.isTrigger = true;
            }

            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider2D>();
            }

            trigger.isTrigger = true;
            trigger.size = GetLocalTriggerSize();
            trigger.offset = Vector2.zero;

            if (visual == null)
            {
                visual = GetComponentInChildren<ChestAnimationController>();
            }

            visual?.ShowClosed();
        }

        /// <summary>
        /// Unity physics event called when another 2D collider enters this chest trigger.
        /// </summary>
        /// <param name="other">The collider that entered the chest range.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (playerInRange)
            {
                return;
            }

            playerInRange = true;
            LogDebug("Player entered chest range.");
        }

        /// <summary>
        /// Unity physics event called when another 2D collider leaves this chest trigger.
        /// </summary>
        /// <param name="other">The collider that left the chest range.</param>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (!playerInRange)
            {
                return;
            }

            playerInRange = false;
            LogDebug("Player left chest range.");
        }

        /// <summary>
        /// Opens the chest, rolls loot, and adds the result to pending loot.
        /// </summary>
        /// <remarks>
        /// Called by this chest when the player presses F in range. PlayerController2D can also call it as a fallback.
        /// </remarks>
        public void Open()
        {
            OpenChest();
        }

        /// <summary>
        /// Opens this chest and applies its loot reward once.
        /// </summary>
        private void OpenChest()
        {
            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager != null && manager.HasClaimedChestInCurrentScene())
            {
                isOpened = true;
                manager.UpdateStatus("This level's chest has already been claimed this run.");
                LogDebug("Scene chest reward already claimed.");
                return;
            }

            if (isOpened || isOpening)
            {
                LogDebug("Chest already opening or opened.");
                return;
            }

            isOpening = true;
            isOpened = true;
            bool hasVisual = visual != null && visual.HasVisual;
            if (hasVisual)
            {
                visual.PlayOpenAnimation(FinishOpening);
            }

            LootDefinition loot = manager != null ? manager.RollLoot() : RollFallbackLoot();

            if (manager == null)
            {
                Debug.LogWarning("No EchoEscapeGameManager found. Loot is being logged but not added to pending loot.");
            }
            else
            {
                manager.MarkChestClaimedInCurrentScene();
                manager.AddPendingLoot(loot);
                manager.AudioService?.PlayChest();
            }

            LogDebug("Chest opened.");
            LogDebug($"Collectible found: {loot.itemName}");
            if (!hasVisual)
            {
                PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));
                FinishOpening();
            }

            TutorialDirector tutorial = manager != null ? manager.Tutorial : null;
            if (tutorial != null)
            {
                tutorial.NotifyChestOpened();
            }
        }

        /// <summary>
        /// Description:
        /// Called when the chest opening animation finishes.
        /// It lets other scripts know the chest is no longer in its opening state.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void FinishOpening()
        {
            isOpening = false;
        }

        /// <summary>
        /// Checks whether a collider belongs to the controllable player.
        /// </summary>
        /// <param name="other">Collider entering or leaving the chest trigger.</param>
        /// <returns>True if the collider belongs to the player.</returns>
        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                return true;
            }

            return other.GetComponentInParent<PlayerController2D>() != null;
        }

        /// <summary>
        /// Converts the requested world-sized trigger into local collider size.
        /// </summary>
        /// <returns>Collider size that produces a compact non-blocking interaction trigger.</returns>
        private Vector2 GetLocalTriggerSize()
        {
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(scale.x));
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(scale.y));
            return new Vector2(InteractionTriggerWorldWidth / scaleX, InteractionTriggerHeight / scaleY);
        }

        /// <summary>
        /// Rolls loot when the chest is tested without a scene Game Manager.
        /// </summary>
        /// <returns>A weighted fallback loot entry.</returns>
        private static LootDefinition RollFallbackLoot()
        {
            IReadOnlyList<CollectibleItem> collectibles = CollectibleDatabase.GetAllCollectibles();
            if (collectibles.Count > 0)
            {
                return CollectibleDatabase.GetRandomCollectible().ToLootDefinition();
            }

            return new LootDefinition("Old Coin", "Common", 60);
        }

        /// <summary>
        /// Writes optional interaction diagnostics to the Console.
        /// </summary>
        /// <param name="message">Debug message to print.</param>
        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
