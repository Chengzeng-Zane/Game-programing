using UnityEngine;

namespace EchoEscape
{
    public class Chest : MonoBehaviour
    {
        public ChestSpawnPoint spawnPoint;
        public bool IsOpened { get; private set; }

        public void Open()
        {
            if (IsOpened)
            {
                return;
            }

            IsOpened = true;
            LootDefinition loot = EchoEscapeGameManager.Instance.RollLoot();
            EchoEscapeGameManager.Instance.AddPendingLoot(loot);
            EchoEscapeGameManager.Instance.AudioService?.PlayChest();
            PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));

            TutorialDirector tutorial = EchoEscapeGameManager.Instance.Tutorial;
            if (tutorial != null)
            {
                tutorial.NotifyChestOpened();
            }
        }
    }
}
