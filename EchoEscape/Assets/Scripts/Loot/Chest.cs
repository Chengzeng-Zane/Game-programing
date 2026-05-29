using UnityEngine;

namespace EchoEscape
{
    // 这个脚本负责宝箱交互，玩家靠近后只能打开一次并获得奖励。
    /// <summary>
    /// Represents a loot chest that can be opened once by the player.
    /// </summary>
    /// <remarks>
    /// Attach this script to chest objects spawned by EchoEscapeGameManager.
    /// The chest listens for F while the player is in range, then the manager rolls loot,
    /// marks it as pending, and shows the loot feedback UI.
    /// </remarks>
    public class Chest : MonoBehaviour
    {
        private const float InteractionTriggerWorldSize = 1.2f;

        // 这个变量记录生成当前宝箱的 ChestSpawnPoint 标记。
        /// <summary>
        /// Spawn marker that created this chest.
        /// </summary>
        public ChestSpawnPoint spawnPoint;

        [SerializeField]
        private bool debugLogs = true;

        [SerializeField]
        private bool logOutOfRangeInput = false;

        // 这个属性表示宝箱是否已经打开并发过奖励。
        /// <summary>
        /// True after this chest has already rewarded loot.
        /// </summary>
        public bool IsOpened => isOpened;

        private bool playerInRange;
        private bool isOpened;

        // 这个函数在宝箱创建时运行，用来准备宝箱图片、颜色和 Trigger 碰撞范围。
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
        }

        // 这个函数每一帧运行一次，用来检查输入或更新当前对象状态。
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
                LogDebug("F key pressed while gameplay is paused.");
                return;
            }

            if (!playerInRange)
            {
                if (logOutOfRangeInput)
                {
                    LogDebug("F pressed, but player is not in chest range.");
                }

                return;
            }

            LogDebug("F key pressed.");

            if (isOpened)
            {
                LogDebug("Chest already opened.");
                return;
            }

            OpenChest();
        }

        // 这个函数在其他 2D 碰撞体进入宝箱触发范围时运行，用来判断玩家是否靠近宝箱。
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

            playerInRange = true;
            LogDebug("Player entered chest range.");
        }

        // 这个函数在其他 2D 碰撞体离开宝箱触发范围时运行，用来判断玩家是否离开宝箱。
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

            playerInRange = false;
            LogDebug("Player left chest range.");
        }

        // 这个函数打开宝箱、随机抽奖励，并把抽到的物品加入待保存奖励列表。
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

        // 这个函数真正执行开宝箱流程：防止重复领取、抽取奖励、更新 UI，并把宝箱变成已打开状态。
        /// <summary>
        /// Opens this chest and applies its loot reward once.
        /// </summary>
        private void OpenChest()
        {
            if (isOpened)
            {
                LogDebug("Chest already opened.");
                return;
            }

            isOpened = true;
            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            LootDefinition loot = manager != null ? manager.RollLoot() : RollFallbackLoot();

            if (manager == null)
            {
                Debug.LogWarning("No EchoEscapeGameManager found. Loot is being logged but not added to pending loot.");
            }
            else
            {
                manager.AddPendingLoot(loot);
                manager.AudioService?.PlayChest();
            }

            Debug.Log("Chest opened.");
            Debug.Log($"Loot found: {loot.itemName} [{loot.rarity}]");
            PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));

            TutorialDirector tutorial = manager != null ? manager.Tutorial : null;
            if (tutorial != null)
            {
                tutorial.NotifyChestOpened();
            }
        }

        // 这个函数判断碰到宝箱的碰撞体是不是可操控的玩家。
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

        // 这个函数把想要的世界坐标触发范围换算成 BoxCollider2D 需要的本地大小。
        /// <summary>
        /// Converts the requested world-sized trigger into local collider size.
        /// </summary>
        /// <returns>Collider size that produces a roughly 1.2 by 1.2 world-space trigger.</returns>
        private Vector2 GetLocalTriggerSize()
        {
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(scale.x));
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(scale.y));
            return new Vector2(InteractionTriggerWorldSize / scaleX, InteractionTriggerWorldSize / scaleY);
        }

        // 这个函数在场景里没有 GameManager 时使用备用奖励表给宝箱抽奖。
        /// <summary>
        /// Rolls loot when the chest is tested without a scene Game Manager.
        /// </summary>
        /// <returns>A weighted fallback loot entry.</returns>
        private static LootDefinition RollFallbackLoot()
        {
            int roll = Random.Range(0, 100);
            if (roll < 60)
            {
                return new LootDefinition("Old Coin", "Common", 60);
            }

            if (roll < 90)
            {
                return new LootDefinition("Blue Gem", "Rare", 30);
            }

            return new LootDefinition("Golden Relic", "Epic", 10);
        }

        // 这个函数根据调试开关把宝箱交互过程输出到 Console。
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
