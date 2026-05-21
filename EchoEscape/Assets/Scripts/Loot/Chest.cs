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

        /// <summary>
        /// Opens the chest, rolls loot, and adds the result to pending loot.
        /// </summary>
        /// <remarks>
        /// Called by PlayerController2D when the player presses F near an unopened chest.
        /// </remarks>
        public void Open()
        {
            if (IsOpened)
            {
                return;
            }

            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("Chest cannot open because no EchoEscapeGameManager exists in the scene.");
                return;
            }

            IsOpened = true;
            LootDefinition loot = manager.RollLoot();
            manager.AddPendingLoot(loot);
            manager.AudioService?.PlayChest();
            Debug.Log($"Chest opened: {loot.itemName}.");
            PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));

            TutorialDirector tutorial = manager.Tutorial;
            if (tutorial != null)
            {
                tutorial.NotifyChestOpened();
            }
        }
    }
}
