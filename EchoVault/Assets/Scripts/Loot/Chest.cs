using UnityEngine;

namespace EchoVault
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
            LootDefinition loot = EchoVaultGameManager.Instance.RollLoot();
            EchoVaultGameManager.Instance.AddPendingLoot(loot);
            EchoVaultGameManager.Instance.AudioService?.PlayChest();
            PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));

            TutorialDirector tutorial = EchoVaultGameManager.Instance.Tutorial;
            if (tutorial != null)
            {
                tutorial.NotifyChestOpened();
            }
        }
    }
}
