using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：敌人生命组件。它处理玩家攻击命中后的扣血、死亡动画和禁用敌人碰撞。
    /// 玩法逻辑：当前敌人一般只有 1 点血，玩家攻击命中后会调用 TakeDamage；血量归零后停止攻击、关掉 Collider，避免死亡中的敌人继续伤害玩家。
    /// 协作关系：PlayerAttack 命中敌人后最终调用它；EnemyAnimationController 播放死亡；EnemyAttack 会被停止。
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        private int currentHealth;
        private bool debugLogs;
        private EnemyAnimationController animationController;
        private EnemyAttack enemyAttack;
        private float deathTimer;
        public bool IsDefeated { get; private set; }
        public bool IsPlayingDeathAnimation { get; private set; }
        /// <summary>
        /// 接收外部脚本传入的参数，把当前组件配置成这个场景或这个敌人需要的状态。
        /// </summary>
        /// <param name="maxHealth">maxHealth 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="logs">logs 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="enemyAnimation">enemyAnimation 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="attack">attack 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void Configure(int maxHealth, bool logs, EnemyAnimationController enemyAnimation, EnemyAttack attack)
        {
            currentHealth = Mathf.Max(1, maxHealth);
            debugLogs = logs;
            animationController = enemyAnimation;
            enemyAttack = attack;
        }
        /// <summary>
        /// 接收伤害并扣除生命值。生命值归零后进入死亡或击败流程。
        /// </summary>
        /// <param name="damage">本次受到的伤害值。</param>
        public void TakeDamage(int damage)
        {
            if (IsDefeated)
            {
                // 已经死亡的敌人不再重复扣血，避免死亡动画/禁用 Collider 被执行多次。
                return;
            }

            // 至少扣 1 点血，防止传入 0 或负数导致攻击没有效果。
            currentHealth -= Mathf.Max(1, damage);
            if (currentHealth > 0)
            {
                // 还有血就继续存活，暂时不播放死亡动画。
                return;
            }

            Defeat();
        }
        /// <summary>
        /// 推进敌人死亡动画。如果正在播放死亡动画返回 true，让 SimpleEnemy 本帧停止其他行为。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        public bool TickDeathAnimation()
        {
            if (!IsPlayingDeathAnimation)
            {
                return false;
            }

            // 死亡动画期间敌人不再移动/攻击，只推进死亡帧。
            animationController.TickDeath();
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                // 动画播完后禁用整个敌人对象，避免尸体 Collider 或脚本继续参与游戏。
                gameObject.SetActive(false);
            }

            return true;
        }
        /// <summary>
        /// 处理敌人被击败后的收尾，包括停止攻击、禁用碰撞和播放死亡动画。
        /// </summary>
        private void Defeat()
        {
            IsDefeated = true;
            // 被玩家击败时立刻停止攻击，避免死亡瞬间攻击框还杀死玩家。
            enemyAttack.StopAttack();

            if (debugLogs)
            {
                Debug.Log("Enemy defeated.");
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                // 禁用所有 Collider，死亡敌人不会再挡路或触发攻击检测。
                colliders[i].enabled = false;
            }

            if (!animationController.HasDeathAnimation)
            {
                // 没有死亡动画素材时直接隐藏敌人，保证击败逻辑仍然完成。
                gameObject.SetActive(false);
                return;
            }

            // 有死亡动画时给它完整播放时间，再由 TickDeathAnimation 关闭对象。
            IsPlayingDeathAnimation = true;
            deathTimer = animationController.DeathDuration;
            animationController.PlayDeath();
        }
    }
}
