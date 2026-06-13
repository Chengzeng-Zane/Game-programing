using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：敌人总控脚本，也是 Inspector 上配置敌人参数的入口。它把移动、寻敌、攻击、血量和动画几个子组件组合成一个完整敌人。
    /// 玩法逻辑：Awake 自动补齐并配置 EnemyTargeting、EnemyMovement、EnemyAttack、EnemyHealth、EnemyAnimationController；Update 决定当前敌人应该死亡动画、暂停、攻击、追击、巡逻还是待机。
    /// 协作关系：PlayerAttack 会调用 Die/TakeDamage；EnemyAttack 命中玩家后调用 GameManager 死亡；EnemyHealth 控制敌人被击败。
    /// </summary>
    public class SimpleEnemy : MonoBehaviour
    {
        [SerializeField]
        private bool patrol;

        [SerializeField]
        private float patrolSpeed = 1f;

        [SerializeField]
        private float patrolDistance = 1.25f;

        [SerializeField]
        private string deathReason = "touched a slime";

        [SerializeField]
        private bool debugLogs = true;

        [SerializeField]
        private int maxHealth = 1;

        [Header("Player Detection")]
        [SerializeField]
        private bool chasePlayer = true;

        [SerializeField]
        private float detectionRange = 5f;

        [SerializeField]
        private float attackRange = 0.85f;

        [Header("Attack Hitbox")]
        [SerializeField]
        [FormerlySerializedAs("attackBoxSize")]
        private Vector2 enemyAttackBoxSize = new Vector2(0.65f, 0.6f);

        [SerializeField]
        [FormerlySerializedAs("attackBoxOffset")]
        private Vector2 enemyAttackOffset = new Vector2(0.45f, 0f);

        [SerializeField]
        [FormerlySerializedAs("attackActiveDelay")]
        private float enemyAttackActiveDelay = 0.15f;

        [SerializeField]
        [FormerlySerializedAs("attackActiveDuration")]
        private float enemyAttackActiveDuration = 0.12f;

        [SerializeField]
        private float chaseSpeed = 1.75f;

        [SerializeField]
        private float returnSpeed = 1.1f;

        [SerializeField]
        private float attackCooldown = 1f;

        [SerializeField]
        private float verticalChaseStrength = 0.35f;

        [SerializeField]
        private float leashDistance = 6f;

        [Header("Visuals")]
        [SerializeField]
        private SpriteRenderer visualRenderer;

        [SerializeField]
        private bool spriteDefaultFacesRight;

        [SerializeField]
        private bool facingRight;

        [SerializeField]
        private string idleFramesPath = "Ancient Forest 1.6/Enemies/Cursed Ghost/Float/Float-Sheet";

        [SerializeField]
        private string fallbackIdleFramesPath = "Ancient Forest 1.6/Enemies/Cursed Ghost/Idle/Idle-Sheet";

        [SerializeField]
        private string deathFramesPath = "Ancient Forest 1.6/Enemies/Cursed Ghost/Dead/Dead-Sheet";

        [SerializeField]
        private string attackFramesPath = "Ancient Forest 1.6/Enemies/Cursed Ghost/Attack/Blood Claw-Sheet";

        [SerializeField]
        private float idleFramesPerSecond = 8f;

        [SerializeField]
        private float deathFramesPerSecond = 10f;

        [SerializeField]
        private float attackFramesPerSecond = 12f;

        [SerializeField]
        private float attackAnimationDuration = 0.35f;

        private Vector3 startPosition;
        private EnemyAnimationController animationController;
        private EnemyAttack enemyAttack;
        private EnemyHealth enemyHealth;
        private EnemyMovement enemyMovement;
        private EnemyTargeting enemyTargeting;
        private bool pausedAfterPlayerDeath;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            startPosition = transform.position;

            if (visualRenderer == null)
            {
                // 如果 Inspector 没拖 SpriteRenderer，就从子物体里自动找敌人的显示层。
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // SimpleEnemy 只做总控；这些子组件分别负责寻敌、动画、移动、攻击和血量。
            enemyTargeting = GetOrAdd<EnemyTargeting>();
            animationController = GetOrAdd<EnemyAnimationController>();
            enemyMovement = GetOrAdd<EnemyMovement>();
            enemyAttack = GetOrAdd<EnemyAttack>();
            enemyHealth = GetOrAdd<EnemyHealth>();

            // 配置动画资源和朝向规则，具体播放帧由 EnemyAnimationController 管。
            animationController.Configure(
                visualRenderer,
                spriteDefaultFacesRight,
                facingRight,
                idleFramesPath,
                fallbackIdleFramesPath,
                attackFramesPath,
                deathFramesPath,
                idleFramesPerSecond,
                attackFramesPerSecond,
                deathFramesPerSecond,
                attackAnimationDuration);

            // 配置追击、巡逻、回家等移动参数，SimpleEnemy 每帧只读取它的决策。
            enemyMovement.Configure(
                patrol,
                patrolSpeed,
                patrolDistance,
                chasePlayer,
                detectionRange,
                attackRange,
                chaseSpeed,
                returnSpeed,
                verticalChaseStrength,
                leashDistance,
                startPosition,
                UpdateFacing);

            // 攻击组件需要知道寻敌、动画、攻击框和死亡回调，命中玩家后会走 GameManager 死亡流程。
            enemyAttack.Configure(
                enemyTargeting,
                animationController,
                enemyAttackBoxSize,
                enemyAttackOffset,
                enemyAttackActiveDelay,
                enemyAttackActiveDuration,
                attackCooldown,
                attackRange,
                leashDistance,
                startPosition,
                deathReason,
                debugLogs,
                IsFacingRight,
                UpdateFacing,
                IsInFacingDirection,
                ShouldPauseAfterPlayerDeath,
                PauseAfterPlayerDeath);

            // 生命组件负责扣血、死亡动画和禁用 Collider，避免死亡敌人继续攻击。
            enemyHealth.Configure(maxHealth, debugLogs, animationController, enemyAttack);
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
        /// </summary>
        private void Update()
        {
            if (enemyHealth.TickDeathAnimation())
            {
                // 正在播放死亡动画时只推进死亡帧，不再移动或攻击。
                return;
            }

            if (enemyHealth.IsDefeated)
            {
                // 已击败但没有动画时直接停止逻辑。
                return;
            }

            if (ShouldPauseAfterPlayerDeath())
            {
                // 玩家已经死亡时敌人停住，避免死亡流程中继续追击或重复攻击。
                PauseAfterPlayerDeath();
                return;
            }

            if (enemyAttack.TickAttackAnimation())
            {
                // 攻击动画/攻击框期间由 EnemyAttack 接管，这一帧不再移动。
                return;
            }

            Transform target = enemyTargeting.GetPlayerTarget();
            bool wantsAttack = enemyMovement.Tick(target) == EnemyMovement.Decision.Attack;
            if (wantsAttack && enemyAttack.TryStartAttack(target))
            {
                // 移动组件判断距离够近后，攻击组件再检查冷却、朝向和攻击框。
                return;
            }

            // 没有攻击也没有死亡时播放待机/漂浮动画。
            animationController.TickIdle();
        }
        /// <summary>
        /// 2D Trigger 刚进入时调用。这里根据进入对象决定是否触发教学、机关、宝箱、死亡或通关。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            enemyAttack.TryAttackCollider(other);
        }
        /// <summary>
        /// 2D Trigger 停留期间持续调用。这里用于处理需要连续检查的触发逻辑，避免高速移动漏判。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerStay2D(Collider2D other)
        {
            enemyAttack.TryAttackCollider(other);
        }
        /// <summary>
        /// 让当前对象进入死亡或被击败状态。对敌人来说通常等同于受到足够伤害。
        /// </summary>
        public void Die()
        {
            TakeDamage(1);
        }
        /// <summary>
        /// 接收伤害并扣除生命值。生命值归零后进入死亡或击败流程。
        /// </summary>
        /// <param name="damage">本次受到的伤害值。</param>
        public void TakeDamage(int damage)
        {
            enemyHealth.TakeDamage(damage);
        }
        /// <summary>
        /// 玩家死亡后暂停敌人状态。它会停止攻击协程和攻击框，并让敌人回到待机视觉，避免重复杀死玩家。
        /// </summary>
        /// <param name="stopAttackRoutine">stopAttackRoutine 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void PauseAfterPlayerDeath(bool stopAttackRoutine = true)
        {
            if (pausedAfterPlayerDeath)
            {
                return;
            }

            pausedAfterPlayerDeath = true;
            // 停止当前攻击协程和攻击框，防止玩家死亡后继续被重复命中。
            enemyAttack.StopAttack(stopAttackRoutine);
            animationController.PlayIdle();

            if (debugLogs)
            {
                Debug.Log($"{name} paused after triggering player death.");
            }
        }
        /// <summary>
        /// 根据当前游戏状态判断是否应该执行某个流程。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool ShouldPauseAfterPlayerDeath()
        {
            return enemyAttack.HasKilledPlayer ||
                (EchoEscapeGameManager.Instance != null && EchoEscapeGameManager.Instance.IsPlayerDeadOrDying);
        }
        /// <summary>
        /// 根据水平移动方向更新敌人朝向，并同步给敌人动画组件。
        /// </summary>
        /// <param name="horizontalDirection">horizontalDirection 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void UpdateFacing(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) <= 0.05f)
            {
                // 很小的方向变化忽略，避免敌人在接近玩家或回家时快速左右抖动。
                return;
            }

            facingRight = horizontalDirection > 0f;
            animationController.SetFacing(facingRight);
        }
        /// <summary>
        /// 返回敌人当前是否面朝右。EnemyAttack 用它来计算攻击框应该出现在左侧还是右侧。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsFacingRight()
        {
            return facingRight;
        }
        /// <summary>
        /// 判断目标是否位于敌人面前。敌人攻击只应该打到面前的玩家，不能打到背后。
        /// </summary>
        /// <param name="targetPosition">目标世界坐标，用来判断距离或朝向。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = facingRight ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }
        /// <summary>
        /// 计算敌人攻击框中心点。Scene 视图的 Gizmos 使用它显示攻击范围。
        /// </summary>
        /// <returns>返回二维坐标或尺寸。</returns>
        private Vector2 AttackBoxCenter()
        {
            float direction = facingRight ? 1f : -1f;
            // attackOffset.x 只需要在 Inspector 里填正数，代码会根据朝向自动左右翻转。
            Vector2 offset = new Vector2(Mathf.Abs(enemyAttackOffset.x) * direction, enemyAttackOffset.y);
            return (Vector2)transform.position + offset;
        }
        /// <summary>
        /// 只在编辑器 Scene 视图中绘制辅助线。它帮助调试攻击框、检测范围等，不影响正式游戏运行。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, detectionRange));

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, attackRange));

            Vector2 center = AttackBoxCenter();
            bool active = enemyAttack != null && enemyAttack.AttackHitboxActive;
            Gizmos.color = active
                ? new Color(1f, 0f, 0f, 0.32f)
                : new Color(1f, 0.45f, 0.05f, 0.18f);
            Gizmos.DrawCube(center, enemyAttackBoxSize);
            Gizmos.color = active
                ? new Color(1f, 0f, 0f, 0.95f)
                : new Color(1f, 0.45f, 0.05f, 0.85f);
            Gizmos.DrawWireCube(center, enemyAttackBoxSize);
        }
        private T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            // 拆分组件如果场景里没手动挂，SimpleEnemy 会自动补上，避免旧场景报缺组件。
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
