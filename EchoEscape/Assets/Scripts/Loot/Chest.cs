using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Treasure Chest Interaction Script. After the player approaches the treasure chest and presses the interact button, the treasure chest opens, plays animation, and extracts loot, and put loot Add to the unsettled list of this level.
/// Gameplay logic: The treasure chest has a trigger range to determine whether the player is close; it prevents repeated collection when opened; the real reward is given by GameManager. RollLoot Extract according to weight; enter after obtaining the item pendingLoot, only when it reaches the exit does it become securedLoot, death will be lost.
/// Collaborates with: PlayerController2D Trigger unboxing; ChestAnimationController Play treasure chest animation; EchoEscapeGameManager records loot；LootFeedbackUI Show getting tips.
    /// </summary>
    public class Chest : MonoBehaviour
    {
        private const float InteractionTriggerWorldWidth = 1.05f;
        private const float InteractionTriggerHeight = 0.9f;
        [SerializeField]
        private bool debugLogs = false;

        [SerializeField]
        private ChestAnimationController visual;
        public bool IsOpened => isOpened;
        public bool IsOpening => isOpening;

        private bool playerInRange;
        private bool isOpening;
        private bool isOpened;
        /// <summary>
/// Initialize the treasure chest trigger range. it puts the treasure chest Collider set to Trigger, to ensure that the interaction is triggered when the player approaches instead of being blocked by the treasure chest, and to prepare to turn off the status vision.
        /// </summary>
        private void Awake()
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (Collider2D collider2D in colliders)
            {
// Treasure chests are interactive targets and should not block player movement, so all Collider All set to Trigger。
                collider2D.isTrigger = true;
            }

            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            if (trigger == null)
            {
// If the art object does not have Collider, just add a fixed size interaction range at runtime.
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
/// Recorded when the player enters the trigger range of the treasure chest playerInRange, then the player presses F Only then allowed to open the box.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
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
/// Canceled when the player leaves the trigger range of the treasure chest playerInRange, preventing you from being able to open the box remotely after you leave.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
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
/// The interactive entrance to the treasure chest is open to the public. PlayerController2D It is called when an interactive treasure chest is found.
        /// </summary>
        public void Open()
        {
            OpenChest();
        }
        /// <summary>
/// Execute the real unboxing process: prevent duplicate collection, play chest animation, and extract loot, join in pendingLoot, and play sound effects and UI feedback.
        /// </summary>
        private void OpenChest()
        {
            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager != null && manager.HasClaimedChestInCurrentScene())
            {
// Only one reward is allowed in the same level and round to prevent re-entering the trigger range or pressing the button repeatedly. F brush loot。
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
// Wait for animation callback when there is animation FinishOpening; The reward data can be settled first and then played back slowly.
                visual.PlayOpenAnimation(FinishOpening);
            }

// priority use GameManager of random loot; No GameManager Alternate rewards are given only in test scenarios.
            LootDefinition loot = manager != null ? manager.RollLoot() : RollFallbackLoot();

            if (manager == null)
            {
                Debug.LogWarning("No EchoEscapeGameManager found. Loot is being logged but not added to pending loot.");
            }
            else
            {
// Put the reward here pendingLoot: The player has obtained it, but it must go to the exit to become securedLoot。
                manager.MarkChestClaimedInCurrentScene();
                manager.AddPendingLoot(loot);
                manager.AudioService?.PlayChest();
            }

            LogDebug("Chest opened.");
            LogDebug($"Collectible found: {loot.itemName}");
            if (!hasVisual)
            {
// In scenes without formal treasure chest animation, graying out is used as the simplest "opened" feedback.
                PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));
                FinishOpening();
            }
        }
        /// <summary>
/// The ending function after the treasure chest animation ends. It ensures that the chest appears open and clears the opening flag.
        /// </summary>
        private void FinishOpening()
        {
            isOpening = false;
        }
        /// <summary>
/// determines whether the object triggering the treasure chest range is a real player. Echo or other Collider Chests should not to to be opened.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
// priority Tag Judgment, fast, and consistent with the scene Player standard settings.
                return true;
            }

// If the trigger is a player child object Collider, can also through the parent PlayerController2D Retrieve player.
            return other.GetComponentInParent<PlayerController2D>() != null;
        }
        /// <summary>
/// Calculated based on chest world size and local scaling Collider size to ensure that the trigger range is still reasonable under different zooms.
        /// </summary>
/// <returns>Returns 2D coordinates or dimensions. </returns>
        private Vector2 GetLocalTriggerSize()
        {
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(scale.x));
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(scale.y));
            return new Vector2(InteractionTriggerWorldWidth / scaleX, InteractionTriggerHeight / scaleY);
        }
        /// <summary>
/// When there is no scene GameManager Return to standby loot, to avoid having no reward data at all after the treasure chest is opened.
        /// </summary>
/// <returns>Returns a loot definition, which will be entered later pending or secured loot process. </returns>
        private static LootDefinition RollFallbackLoot()
        {
            IReadOnlyList<CollectibleItem> collectibles = CollectibleDatabase.GetAllCollectibles();
            if (collectibles.Count > 0)
            {
// Use formal when there is a collection database collectible, to ensure that the test scene reward performance is consistent with the official level.
                return CollectibleDatabase.GetRandomCollectible().ToLootDefinition();
            }

// The final guaranteed data to avoid errors when unpacking without a database.
            return new LootDefinition("Old Coin", "Common", 60);
        }
        /// <summary>
/// debugLogs When turned on, treasure chest debugging information is output to facilitate checking whether the player has entered the range and whether to open the chest repeatedly.
        /// </summary>
/// <param name="message">to be displayed to HUD Or the text written in the log. </param>
        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
