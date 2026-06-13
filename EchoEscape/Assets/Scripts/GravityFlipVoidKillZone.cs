using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：反重力专用死亡区。普通坑和河流可以直接用 HazardZone，但反重力区域不能这么做，因为玩家普通重力经过这些触发器时不应该死亡。
    /// 玩法逻辑：玩家翻到上方平台后，如果从平台左侧、右侧或上方掉出可玩范围，这个触发器会检查玩家是否真的处于 Gravity Flip 状态；只有反重力状态才走死亡流程。
    /// 协作关系：读取 GravityFlipController.IsFlipped；忽略 EchoReplayController；最终复用 HazardZone/EchoEscapeGameManager 的统一死亡流程，所以死亡动画、You Died UI、loot 丢失和重载关卡都保持一致。
    /// </summary>
    public class GravityFlipVoidKillZone : MonoBehaviour
    {
        [SerializeField]
        private string deathReason = "fell out during gravity flip";

        [SerializeField]
        private bool debugLogs;
        /// <summary>
        /// 玩家第一次进入反重力死亡触发器时检查是否应该死亡。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            TryKillFlippedPlayer(other);
        }
        /// <summary>
        /// 玩家停留在反重力死亡触发器内时继续检查，避免高速移动错过 Enter 的情况。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerStay2D(Collider2D other)
        {
            TryKillFlippedPlayer(other);
        }
        /// <summary>
        /// 确认进入对象是真玩家、不是 Echo，并且 GravityFlipController.IsFlipped 为 true 后，调用统一死亡流程。普通重力进入会直接忽略。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void TryKillFlippedPlayer(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null)
            {
                // Echo 可以经过这些区域，但它不是玩家本体，不能触发玩家死亡。
                return;
            }

            PlayerController2D player = other.GetComponent<PlayerController2D>() ??
                other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                // 不是玩家 Collider，例如机关、道具、敌人，都忽略。
                return;
            }

            if (debugLogs)
            {
                Debug.Log("[GravityFlipDeath] player entered zone");
            }

            GravityFlipController gravityFlip = player.GetComponent<GravityFlipController>();
            bool isFlipped = gravityFlip != null && gravityFlip.IsFlipped;
            if (debugLogs)
            {
                Debug.Log($"[GravityFlipDeath] isFlipped = {isFlipped}");
            }

            if (!isFlipped)
            {
                // 普通重力状态下经过这些 Trigger 不应该死；这些区域只补反重力掉出边界的问题。
                return;
            }

            if (debugLogs)
            {
                Debug.Log("[GravityFlipDeath] calling death flow");
            }

            // 这里复用 HazardZone 的公共死亡入口，保证死亡动画、You Died、loot 丢失和重载关卡完全一致。
            HazardZone.KillPlayerUsingExistingDeathFlow(this, deathReason, name, debugLogs);
        }
    }
}
