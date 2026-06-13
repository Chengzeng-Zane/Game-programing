using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：敌人目标识别工具。它的核心任务是找到真正的 Player，并明确忽略 Echo。
    /// 玩法逻辑：Echo 是玩家录制出来的影子，可以压机关，但不应该让敌人把它当作真正玩家来攻击或触发死亡。这个脚本通过 tag、EchoReplayController 和 PlayerController2D 做过滤。
    /// 协作关系：EnemyMovement 用它追踪玩家；EnemyAttack 用它判断触发器和攻击框里是不是玩家。
    /// </summary>
    public class EnemyTargeting : MonoBehaviour
    {
        private Transform playerTarget;
        /// <summary>
        /// 返回当前真正玩家的 Transform。敌人移动组件用它判断追击方向和距离。
        /// </summary>
        /// <returns>返回找到的 Transform；找不到时可能返回 null。</returns>
        public Transform GetPlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                // 缓存玩家 Transform，避免每帧 FindObjectOfType；玩家仍存在时直接复用。
                return playerTarget;
            }

            // 玩家死亡重载或场景切换后缓存可能失效，这时重新查找。
            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            playerTarget = player != null ? player.transform : null;
            return playerTarget;
        }
        /// <summary>
        /// 从一个 Collider 中找真正玩家。Echo 或非玩家物体会返回 null。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <returns>返回真正玩家控制器；如果传入对象不是玩家则返回 null。</returns>
        public PlayerController2D GetPlayer(Collider2D other)
        {
            if (other == null ||
                HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null)
            {
                // Echo 可以压按钮，但不能被敌人当成玩家攻击，否则谜题会误触发死亡。
                return null;
            }

            // 玩家 Collider 可能在根物体或子物体上，所以同时检查当前对象和父级。
            return other.GetComponent<PlayerController2D>() ?? other.GetComponentInParent<PlayerController2D>();
        }
        /// <summary>
        /// 安全检查 Collider 或根对象是否有指定 Tag。Tag 缺失时不会让游戏报错。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <param name="tagName">tagName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private static bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
                // 如果项目没有配置对应 Tag，CompareTag 会抛异常；这里返回 false 保持游戏继续运行。
                return false;
            }
        }
    }
}
