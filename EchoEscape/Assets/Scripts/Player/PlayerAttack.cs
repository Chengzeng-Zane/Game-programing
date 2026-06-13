using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：玩家攻击脚本。它负责按键攻击、攻击动画、攻击前摇、攻击判定框和敌人受伤。
    /// 玩法逻辑：玩家按攻击键后先播放攻击动画，等待 attackActiveDelay 后短暂打开攻击框；攻击框用 OverlapBoxAll 检测敌人，并且只打中玩家面朝方向的敌人，避免背后敌人被误伤。
    /// 协作关系：PlayerAnimationController 播放攻击视觉；SimpleEnemy/EnemyHealth 接收伤害；OnDrawGizmosSelected 帮你在 Scene 里看攻击框大小。
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField]
        private KeyCode attackKey = KeyCode.J; // 玩家攻击键。

        [SerializeField]
        private Vector2 attackBoxSize = new Vector2(0.65f, 0.5f); // 攻击判定框的宽度和高度。

        [SerializeField]
        [FormerlySerializedAs("attackOffset")]
        private Vector2 attackBoxOffset = new Vector2(0.55f, 0f); // 攻击判定框相对玩家中心的偏移，会根据朝向左右翻转。

        [SerializeField]
        private int attackDamage = 1;

        [SerializeField]
        private LayerMask enemyLayers = ~0; // 可选的敌人层过滤；最终仍会检查敌人脚本。

        [SerializeField]
        private float attackActiveDelay = 0.1f; // 按下攻击后，判定框真正生效前的前摇时间。

        [SerializeField]
        private float attackActiveDuration = 0.12f; // 攻击判定框可以造成伤害的持续时间。

        [SerializeField]
        private float attackCooldown = 0.4f; // 两次攻击之间的最短间隔。

        [SerializeField]
        private bool debugLogs = true; // 是否输出攻击调试日志，方便测试攻击框。

        private PlayerController2D playerController;
        private PlayerAnimationController animationController;
        private Coroutine attackRoutine;
        private float nextAttackTime;
        private bool attackHitboxActive;
        /// <summary>
        /// 缓存玩家控制器和玩家动画控制器。攻击需要知道面朝方向，也需要通知动画脚本播放攻击帧。
        /// </summary>
        private void Awake()
        {
            playerController = GetComponent<PlayerController2D>();
            animationController = GetComponentInChildren<PlayerAnimationController>();
        }
        /// <summary>
        /// 每帧检查攻击键和冷却时间。只有游戏没有暂停、没有攻击协程正在执行、冷却结束时，才会启动新攻击。
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(attackKey))
            {
                Attack();
            }
        }
        /// <summary>
        /// 开始一次玩家攻击。它记录冷却时间、播放攻击动画，然后启动 AttackRoutine 控制前摇和判定框。
        /// </summary>
        public void Attack()
        {
            if (attackRoutine != null || Time.time < nextAttackTime)
            {
                // 正在攻击或冷却没结束时不接受新攻击，防止按键连发造成多次伤害。
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine());
        }
        /// <summary>
        /// 攻击协程。先等待 attackActiveDelay 表示武器挥出前摇，然后在 attackActiveDuration 内反复检查攻击框命中，最后关闭攻击状态。
        /// </summary>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator AttackRoutine()
        {
            // 视觉先播放攻击动作，真正造成伤害的攻击框稍后才打开，形成“挥剑前摇”。
            animationController?.PlayAttack();

            if (attackActiveDelay > 0f)
            {
                yield return new WaitForSeconds(attackActiveDelay);
            }

            attackHitboxActive = true;
            // 一次攻击期间同一个敌人只能受伤一次，即使攻击框持续多帧检测到它。
            HashSet<SimpleEnemy> damagedEnemies = new HashSet<SimpleEnemy>();
            bool defeatedEnemy = false;
            float endTime = Time.time + Mathf.Max(0.01f, attackActiveDuration);

            while (Time.time < endTime)
            {
                // 在短暂有效时间内逐帧检测，提高高速移动时的命中稳定性。
                defeatedEnemy |= CheckAttackHits(damagedEnemies);
                yield return null;
            }

            attackHitboxActive = false;
            attackRoutine = null;

            if (debugLogs && !defeatedEnemy)
            {
                Debug.Log("Player attacked.");
            }
        }
        /// <summary>
        /// 用 Physics2D.OverlapBoxAll 检测攻击框内的对象。它会过滤掉已经受伤的敌人，并检查敌人是否在玩家面朝方向，命中后调用敌人受伤。
        /// </summary>
        /// <param name="damagedEnemies">damagedEnemies 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool CheckAttackHits(HashSet<SimpleEnemy> damagedEnemies)
        {
            Vector2 center = AttackCenter();
            // OverlapBoxAll 就是玩家攻击框：大小由 attackBoxSize 控制，中心由 attackBoxOffset 和朝向决定。
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyLayers);
            bool defeatedEnemy = false;

            foreach (Collider2D hit in hits)
            {
                SimpleEnemy enemy = hit.GetComponent<SimpleEnemy>();
                if (enemy == null)
                {
                    // 有些敌人 Collider 在子物体上，所以找不到时继续查父级。
                    enemy = hit.GetComponentInParent<SimpleEnemy>();
                }

                if (enemy == null ||
                    !IsInFacingDirection(enemy.transform.position) ||
                    !damagedEnemies.Add(enemy))
                {
                    // 过滤非敌人、背后的敌人、以及这次攻击已经打中过的敌人。
                    continue;
                }

                // 敌人受伤后由 EnemyHealth 判断是否死亡，玩家攻击脚本不直接禁用敌人。
                enemy.TakeDamage(attackDamage);
                defeatedEnemy = true;
            }

            return defeatedEnemy;
        }
        /// <summary>
        /// 计算攻击框中心点。攻击框基于玩家位置和 attackBoxOffset，并根据玩家朝向左右翻转。
        /// </summary>
        /// <returns>返回二维坐标或尺寸。</returns>
        private Vector2 AttackCenter()
        {
            bool facingRight = IsFacingRight();
            float direction = facingRight ? 1f : -1f;
            // x 偏移取绝对值再乘方向，保证 Inspector 里填正数就能自动左右翻转。
            Vector2 offset = new Vector2(Mathf.Abs(attackBoxOffset.x) * direction, attackBoxOffset.y);
            return (Vector2)transform.position + offset;
        }
        /// <summary>
        /// 判断目标点是否在玩家面朝的一侧。这个限制让攻击更符合视觉表现，玩家不能打到背后的敌人。
        /// </summary>
        /// <param name="targetPosition">目标世界坐标，用来判断距离或朝向。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = IsFacingRight() ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }
        /// <summary>
        /// 读取玩家当前是否朝右。如果 PlayerController2D 存在，就用它的 FacingRight；否则默认朝右。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsFacingRight()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController2D>();
            }

            return playerController == null || playerController.FacingRight;
        }
        /// <summary>
        /// 在 Scene 视图画出攻击框范围。调攻击距离、宽高和偏移时可以直接看这个框是否覆盖到敌人。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector2 center = AttackCenter();
            Gizmos.color = attackHitboxActive
                ? new Color(1f, 0.25f, 0.1f, 0.32f)
                : new Color(1f, 0.85f, 0.2f, 0.18f);
            Gizmos.DrawCube(center, attackBoxSize);
            Gizmos.color = attackHitboxActive
                ? new Color(1f, 0.1f, 0.05f, 0.9f)
                : new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(center, attackBoxSize);
        }
    }
}
