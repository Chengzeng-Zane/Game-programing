using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：宝箱交互脚本。玩家靠近宝箱并按交互键后，宝箱打开、播放动画、抽取 loot，并把 loot 加入本关未结算列表。
    /// 玩法逻辑：宝箱有触发范围判断玩家是否靠近；打开时防止重复领取；真正奖励由 GameManager.RollLoot 按权重抽取；获得物品后进入 pendingLoot，只有到出口才变成 securedLoot，死亡会丢失。
    /// 协作关系：PlayerController2D 触发开箱；ChestAnimationController 播放宝箱动画；EchoEscapeGameManager 记录 loot；LootFeedbackUI 显示获得提示。
    /// </summary>
    public class Chest : MonoBehaviour
    {
        private const float InteractionTriggerWorldWidth = 1.05f;
        private const float InteractionTriggerHeight = 0.9f;
        public ChestSpawnPoint spawnPoint;

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
        /// 初始化宝箱触发范围。它把宝箱 Collider 设为 Trigger，保证玩家靠近时触发交互而不是被宝箱挡住，并准备关闭状态视觉。
        /// </summary>
        private void Awake()
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (Collider2D collider2D in colliders)
            {
                // 宝箱是交互目标，不应该挡住玩家移动，所以所有 Collider 都设为 Trigger。
                collider2D.isTrigger = true;
            }

            BoxCollider2D trigger = GetComponent<BoxCollider2D>();
            if (trigger == null)
            {
                // 如果美术物体没有 Collider，就运行时补一个固定大小的交互范围。
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
        /// 玩家进入宝箱触发范围时记录 playerInRange，之后玩家按 F 才允许开箱。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
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
        /// 玩家离开宝箱触发范围时取消 playerInRange，防止离开后还能远程开箱。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
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
        /// 宝箱对外开放的交互入口。PlayerController2D 找到可交互宝箱后会调用它。
        /// </summary>
        public void Open()
        {
            OpenChest();
        }
        /// <summary>
        /// 执行真正开箱流程：防重复领取、播放宝箱动画、抽取 loot、加入 pendingLoot，并播放音效和 UI 反馈。
        /// </summary>
        private void OpenChest()
        {
            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager != null && manager.HasClaimedChestInCurrentScene())
            {
                // 同一关同一局只允许领取一次奖励，防止重进触发范围或重复按 F 刷 loot。
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
                // 有动画时等动画回调 FinishOpening；奖励数据可以先结算，视觉再慢慢播放。
                visual.PlayOpenAnimation(FinishOpening);
            }

            // 优先使用 GameManager 的随机 loot；没有 GameManager 的测试场景才走备用奖励。
            LootDefinition loot = manager != null ? manager.RollLoot() : RollFallbackLoot();

            if (manager == null)
            {
                Debug.LogWarning("No EchoEscapeGameManager found. Loot is being logged but not added to pending loot.");
            }
            else
            {
                // 这里把奖励放进 pendingLoot：玩家拿到了，但必须到出口才会变成 securedLoot。
                manager.MarkChestClaimedInCurrentScene();
                manager.AddPendingLoot(loot);
                manager.AudioService?.PlayChest();
            }

            LogDebug("Chest opened.");
            LogDebug($"Collectible found: {loot.itemName}");
            if (!hasVisual)
            {
                // 没有正式宝箱动画的场景，用变灰作为最简单的“已打开”反馈。
                PrototypeFactory.Tint(gameObject, new Color(0.42f, 0.42f, 0.42f));
                FinishOpening();
            }

            TutorialDirector tutorial = manager != null ? manager.Tutorial : null;
            if (tutorial != null)
            {
                // 教程系统需要知道玩家已经学会开箱，才能推进目标提示。
                tutorial.NotifyChestOpened();
            }
        }
        /// <summary>
        /// 宝箱动画结束后的收尾函数。它保证宝箱显示为打开状态，并清除正在打开的标记。
        /// </summary>
        private void FinishOpening()
        {
            isOpening = false;
        }
        /// <summary>
        /// 判断触发宝箱范围的对象是否是真正玩家。Echo 或其他 Collider 不应该打开宝箱。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                // 优先用 Tag 判断，速度快，也符合场景里 Player 的标准设置。
                return true;
            }

            // 如果触发的是玩家子物体 Collider，也能通过父级 PlayerController2D 找回玩家。
            return other.GetComponentInParent<PlayerController2D>() != null;
        }
        /// <summary>
        /// 根据宝箱世界尺寸和本地缩放计算 Collider 大小，保证触发范围在不同缩放下仍合理。
        /// </summary>
        /// <returns>返回二维坐标或尺寸。</returns>
        private Vector2 GetLocalTriggerSize()
        {
            Vector3 scale = transform.lossyScale;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(scale.x));
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(scale.y));
            return new Vector2(InteractionTriggerWorldWidth / scaleX, InteractionTriggerHeight / scaleY);
        }
        /// <summary>
        /// 当场景里没有 GameManager 时返回备用 loot，避免宝箱打开后完全没有奖励数据。
        /// </summary>
        /// <returns>返回一个战利品定义，后续会进入 pending 或 secured loot 流程。</returns>
        private static LootDefinition RollFallbackLoot()
        {
            IReadOnlyList<CollectibleItem> collectibles = CollectibleDatabase.GetAllCollectibles();
            if (collectibles.Count > 0)
            {
                // 有收藏品数据库时沿用正式 collectible，保证测试场景奖励表现和正式关卡一致。
                return CollectibleDatabase.GetRandomCollectible().ToLootDefinition();
            }

            // 最后的保底数据，避免没有数据库时开箱报错。
            return new LootDefinition("Old Coin", "Common", 60);
        }
        /// <summary>
        /// debugLogs 开启时输出宝箱调试信息，方便检查玩家是否进范围、是否重复开箱。
        /// </summary>
        /// <param name="message">要显示到 HUD 或写入日志的文字。</param>
        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
