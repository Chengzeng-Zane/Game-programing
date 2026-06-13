using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：敌人攻击组件。它负责判断玩家是否进入攻击范围、播放攻击前摇、打开攻击判定框，并在命中后杀死玩家。
    /// 玩法逻辑：敌人不会碰到玩家就立刻死亡玩家，而是先检查距离、冷却、朝向和攻击框；攻击框激活期间如果真正玩家在敌人面前，就调用 GameManager 的统一死亡流程。Echo 会被过滤，不会触发玩家死亡。
    /// 协作关系：SimpleEnemy 负责配置参数；EnemyTargeting 负责找玩家；EnemyAnimationController 播放攻击；EchoEscapeGameManager 处理死亡、UI 和重载。
    /// </summary>
    public class EnemyAttack : MonoBehaviour
    {
        private EnemyTargeting targeting;
        private EnemyAnimationController animationController;
        private Vector2 attackBoxSize;
        private Vector2 attackBoxOffset;
        private float attackActiveDelay;
        private float attackActiveDuration;
        private float attackCooldown;
        private float attackRange;
        private float leashDistance;
        private Vector3 startPosition;
        private string deathReason;
        private bool debugLogs;
        private Func<bool> getFacingRight;
        private Action<float> updateFacing;
        private Func<Vector2, bool> isInFacingDirection;
        private Func<bool> shouldPause;
        private Action<bool> pauseAfterPlayerDeath;
        private Coroutine attackRoutine;
        private float nextAttackTime;
        public bool HasKilledPlayer { get; private set; }
        public bool AttackHitboxActive { get; private set; }

        private bool IsBusy => attackRoutine != null ||
            (animationController != null && animationController.IsAttackAnimating);
        /// <summary>
        /// 接收外部脚本传入的参数，把当前组件配置成这个场景或这个敌人需要的状态。
        /// </summary>
        /// <param name="enemyTargeting">enemyTargeting 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="enemyAnimation">enemyAnimation 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="boxSize">boxSize 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="boxOffset">boxOffset 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="activeDelay">activeDelay 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="activeDuration">activeDuration 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="cooldown">cooldown 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="range">range 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="leash">leash 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="homePosition">homePosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="reason">死亡原因或事件原因，用于死亡 UI、状态提示和调试日志。</param>
        /// <param name="logs">logs 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingRightGetter">facingRightGetter 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingUpdater">facingUpdater 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingCheck">facingCheck 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="pauseCheck">pauseCheck 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="pauseCallback">pauseCallback 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void Configure(
            EnemyTargeting enemyTargeting,
            EnemyAnimationController enemyAnimation,
            Vector2 boxSize,
            Vector2 boxOffset,
            float activeDelay,
            float activeDuration,
            float cooldown,
            float range,
            float leash,
            Vector3 homePosition,
            string reason,
            bool logs,
            Func<bool> facingRightGetter,
            Action<float> facingUpdater,
            Func<Vector2, bool> facingCheck,
            Func<bool> pauseCheck,
            Action<bool> pauseCallback)
        {
            targeting = enemyTargeting;
            animationController = enemyAnimation;
            attackBoxSize = boxSize;
            attackBoxOffset = boxOffset;
            attackActiveDelay = activeDelay;
            attackActiveDuration = activeDuration;
            attackCooldown = cooldown;
            attackRange = range;
            leashDistance = leash;
            startPosition = homePosition;
            deathReason = reason;
            debugLogs = logs;
            getFacingRight = facingRightGetter;
            updateFacing = facingUpdater;
            isInFacingDirection = facingCheck;
            shouldPause = pauseCheck;
            pauseAfterPlayerDeath = pauseCallback;
        }
        /// <summary>
        /// 尝试执行一个可能失败的操作；如果条件不满足，会安全退出或返回失败。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        public void TryAttackCollider(Collider2D other)
        {
            if (shouldPause())
            {
                // 玩家已经进入死亡流程时，敌人攻击系统停止，避免重复 KillPlayer。
                pauseAfterPlayerDeath(true);
                return;
            }

            PlayerController2D player = targeting.GetPlayer(other);
            if (player == null)
            {
                // EnemyTargeting 会过滤 Echo 和非玩家 Collider。
                return;
            }

            // 触碰敌人并不直接死亡，而是尝试开始一次带前摇和攻击框的攻击。
            TryStartAttack(player.transform);
        }
        /// <summary>
        /// 尝试开始敌人攻击。它会检查是否已经在攻击、目标是否有效、是否在攻击距离/牵引范围内，以及冷却是否结束。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        public bool TryStartAttack(Transform target)
        {
            if (shouldPause())
            {
                // 死亡流程中不再开新攻击，保持玩家死亡反馈干净。
                pauseAfterPlayerDeath(true);
                return false;
            }

            if (IsBusy || target == null || !CanStartAttack(target) || Time.time < nextAttackTime)
            {
                // 忙、没目标、距离不合适或冷却中，都不能开始攻击。
                return false;
            }

            // 攻击前先面向玩家，让攻击框出现在正确一侧。
            updateFacing(target.position.x - transform.position.x);
            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine(target));
            return true;
        }
        /// <summary>
        /// 推进敌人的攻击动画。SimpleEnemy 每帧调用它，如果返回 true，说明这一帧攻击系统正在接管敌人状态。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        public bool TickAttackAnimation()
        {
            if (!IsBusy)
            {
                return false;
            }

            animationController.TickAttack();
            return true;
        }
        /// <summary>
        /// 停止当前敌人攻击。玩家死亡或敌人被击败时调用，确保攻击框立刻关闭。
        /// </summary>
        /// <param name="stopRoutine">stopRoutine 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void StopAttack(bool stopRoutine = true)
        {
            AttackHitboxActive = false;
            if (stopRoutine && attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }
        /// <summary>
        /// 敌人一次完整攻击流程：播放前摇动画，短暂开启攻击框，命中玩家后触发统一死亡流程。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator AttackRoutine(Transform target)
        {
            // 先播放攻击动画，并把前摇+有效时间告诉动画系统，保证视觉覆盖整个攻击过程。
            animationController.PlayAttack(attackActiveDelay + attackActiveDuration);

            if (attackActiveDelay > 0f)
            {
                // 前摇阶段只播放动画，不造成伤害，让玩家有一点反应时间。
                yield return new WaitForSeconds(attackActiveDelay);
            }

            if (shouldPause())
            {
                // 如果前摇期间玩家已经死亡或敌人被暂停，就取消本次攻击。
                attackRoutine = null;
                yield break;
            }

            AttackHitboxActive = true;
            bool hitPlayer = false;
            bool playerIsInFront = target != null && isInFacingDirection(target.position);
            bool playerInsideHitbox = false;
            float endTime = Time.time + Mathf.Max(0.01f, attackActiveDuration);
            while (Time.time < endTime && !shouldPause())
            {
                // 攻击框有效期间逐帧检查，避免玩家高速移动时只检测一帧漏判。
                if (!hitPlayer && TryHitPlayerInAttackBox(target, out playerIsInFront, out playerInsideHitbox))
                {
                    hitPlayer = true;
                }

                yield return null;
            }

            AttackHitboxActive = false;
            attackRoutine = null;
            LogAttackCheck(target, playerIsInFront, playerInsideHitbox, hitPlayer);
        }
        /// <summary>
        /// 在敌人攻击框内寻找真正玩家，并确认玩家在敌人面前；命中后调用 KillPlayer。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <param name="playerIsInFront">playerIsInFront 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="playerInsideHitbox">playerInsideHitbox 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool TryHitPlayerInAttackBox(Transform target, out bool playerIsInFront, out bool playerInsideHitbox)
        {
            playerIsInFront = target != null && isInFacingDirection(target.position);
            playerInsideHitbox = false;

            // 敌人攻击框是一个 OverlapBox，大小和偏移来自 SimpleEnemy 的 Inspector 配置。
            Collider2D[] hits = Physics2D.OverlapBoxAll(AttackBoxCenter(), attackBoxSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController2D player = targeting.GetPlayer(hits[i]);
                if (player == null)
                {
                    // Echo、非玩家物体和敌人自己的 Collider 都会在这里被过滤掉。
                    continue;
                }

                playerInsideHitbox = true;
                playerIsInFront = isInFacingDirection(player.transform.position);
                if (!playerIsInFront)
                {
                    // 即使玩家在攻击框边缘，如果不在敌人面前，也不算命中。
                    continue;
                }

                // 命中后走统一死亡入口，不在敌人脚本里自己显示 UI 或重载场景。
                KillPlayer();
                HasKilledPlayer = true;
                pauseAfterPlayerDeath(false);
                return true;
            }

            return false;
        }
        /// <summary>
        /// 判断当前条件是否允许执行某个动作，例如开箱、攻击、按按钮或切换状态。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool CanStartAttack(Transform target)
        {
            float effectiveAttackRange = Mathf.Max(0f, attackRange);
            float attackRangeSquared = effectiveAttackRange * effectiveAttackRange;
            if (((Vector2)transform.position - (Vector2)target.position).sqrMagnitude > attackRangeSquared)
            {
                // 用平方距离避免每帧开方，性能更稳定。
                return false;
            }

            float effectiveLeashDistance = Mathf.Max(0f, leashDistance);
            float leashDistanceSquared = effectiveLeashDistance * effectiveLeashDistance;
            // leash 限制敌人不能离出生点太远，防止玩家把敌人引出设计区域。
            return ((Vector2)startPosition - (Vector2)target.position).sqrMagnitude <= leashDistanceSquared;
        }
        /// <summary>
        /// 根据敌人位置、朝向和 attackBoxOffset 计算攻击框中心点。
        /// </summary>
        /// <returns>返回二维坐标或尺寸。</returns>
        private Vector2 AttackBoxCenter()
        {
            bool facingRight = getFacingRight();
            float direction = facingRight ? 1f : -1f;
            // x 偏移根据敌人朝向翻转，所以同一个 attackBoxOffset 可同时支持左右攻击。
            Vector2 offset = new Vector2(Mathf.Abs(attackBoxOffset.x) * direction, attackBoxOffset.y);
            return (Vector2)transform.position + offset;
        }
        /// <summary>
        /// 触发玩家死亡或相关死亡流程。这个游戏里死亡应尽量走 GameManager 的统一流程。
        /// </summary>
        private void KillPlayer()
        {
            if (debugLogs)
            {
                PlayerController2D debugPlayer = FindObjectOfType<PlayerController2D>();
                Debug.Log(
                    $"[DeathDebug] Enemy killed player. enemy={name}, reason={deathReason}, " +
                    $"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, time={Time.time}, " +
                    $"enemyPos={transform.position}, playerPos={(debugPlayer != null ? debugPlayer.transform.position.ToString() : "none")}");
            }

            if (EchoEscapeGameManager.Instance != null)
            {
                // GameManager 负责完整死亡流程：动画、UI、pending loot 丢失、重载当前关。
                EchoEscapeGameManager.Instance.KillPlayer(deathReason);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("Cursed Ghost attacked the player, but no EchoEscapeGameManager was found.");
            }
        }
        /// <summary>
        /// 输出调试日志，帮助测试时确认流程有没有按预期执行。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <param name="playerIsInFront">playerIsInFront 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="playerInsideHitbox">playerInsideHitbox 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="hitPlayer">hitPlayer 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void LogAttackCheck(Transform target, bool playerIsInFront, bool playerInsideHitbox, bool hitPlayer)
        {
            if (!debugLogs)
            {
                return;
            }

            string playerPosition = target != null ? target.position.ToString("F2") : "none";
            Debug.Log(
                $"[EnemyAttackCheck] enemy={name}, enemyPos={transform.position.ToString("F2")}, playerPos={playerPosition}, " +
                $"facingRight={getFacingRight()}, playerIsInFront={playerIsInFront}, " +
                $"attackBoxCenter={AttackBoxCenter().ToString("F2")}, attackBoxSize={attackBoxSize.ToString("F2")}, " +
                $"playerInsideHitbox={playerInsideHitbox}, final={(hitPlayer ? "hit" : "miss")}");
        }
    }
}
