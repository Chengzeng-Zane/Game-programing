using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Handles enemy attack timing, hitbox checks, and player death calls.
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

        /// <summary>
        /// True after this enemy has killed the player.
        /// </summary>
        public bool HasKilledPlayer { get; private set; }

        /// <summary>
        /// True while the attack hitbox is active.
        /// </summary>
        public bool AttackHitboxActive { get; private set; }

        private bool IsBusy => attackRoutine != null ||
            (animationController != null && animationController.IsAttackAnimating);

        /// <summary>
        /// Initializes attack behavior from the serialized SimpleEnemy settings.
        /// </summary>
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
        /// Checks a trigger collider and starts an attack against the player when valid.
        /// </summary>
        /// <param name="other">Collider that entered or stayed in the enemy trigger.</param>
        public void TryAttackCollider(Collider2D other)
        {
            if (shouldPause())
            {
                pauseAfterPlayerDeath(true);
                return;
            }

            PlayerController2D player = targeting.GetPlayer(other);
            if (player == null)
            {
                return;
            }

            TryStartAttack(player.transform);
        }

        /// <summary>
        /// Attempts to start a new enemy attack.
        /// </summary>
        /// <param name="target">Player transform to attack.</param>
        /// <returns>True when an attack was started.</returns>
        public bool TryStartAttack(Transform target)
        {
            if (shouldPause())
            {
                pauseAfterPlayerDeath(true);
                return false;
            }

            if (IsBusy || target == null || !CanStartAttack(target) || Time.time < nextAttackTime)
            {
                return false;
            }

            updateFacing(target.position.x - transform.position.x);
            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine(target));
            return true;
        }

        /// <summary>
        /// Advances attack animation while an attack is active or finishing.
        /// </summary>
        /// <returns>True when attack logic is occupying the enemy this frame.</returns>
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
        /// Stops active attack behavior.
        /// </summary>
        /// <param name="stopRoutine">True to stop the coroutine immediately.</param>
        public void StopAttack(bool stopRoutine = true)
        {
            AttackHitboxActive = false;
            if (stopRoutine && attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }

        private IEnumerator AttackRoutine(Transform target)
        {
            animationController.PlayAttack(attackActiveDelay + attackActiveDuration);

            if (attackActiveDelay > 0f)
            {
                yield return new WaitForSeconds(attackActiveDelay);
            }

            if (shouldPause())
            {
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

        private bool TryHitPlayerInAttackBox(Transform target, out bool playerIsInFront, out bool playerInsideHitbox)
        {
            playerIsInFront = target != null && isInFacingDirection(target.position);
            playerInsideHitbox = false;

            Collider2D[] hits = Physics2D.OverlapBoxAll(AttackBoxCenter(), attackBoxSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController2D player = targeting.GetPlayer(hits[i]);
                if (player == null)
                {
                    continue;
                }

                playerInsideHitbox = true;
                playerIsInFront = isInFacingDirection(player.transform.position);
                if (!playerIsInFront)
                {
                    continue;
                }

                KillPlayer();
                HasKilledPlayer = true;
                pauseAfterPlayerDeath(false);
                return true;
            }

            return false;
        }

        private bool CanStartAttack(Transform target)
        {
            float effectiveAttackRange = Mathf.Max(0f, attackRange);
            float attackRangeSquared = effectiveAttackRange * effectiveAttackRange;
            if (((Vector2)transform.position - (Vector2)target.position).sqrMagnitude > attackRangeSquared)
            {
                return false;
            }

            float effectiveLeashDistance = Mathf.Max(0f, leashDistance);
            float leashDistanceSquared = effectiveLeashDistance * effectiveLeashDistance;
            return ((Vector2)startPosition - (Vector2)target.position).sqrMagnitude <= leashDistanceSquared;
        }

        private Vector2 AttackBoxCenter()
        {
            bool facingRight = getFacingRight();
            float direction = facingRight ? 1f : -1f;
            Vector2 offset = new Vector2(Mathf.Abs(attackBoxOffset.x) * direction, attackBoxOffset.y);
            return (Vector2)transform.position + offset;
        }

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
                EchoEscapeGameManager.Instance.KillPlayer(deathReason);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("Cursed Ghost attacked the player, but no EchoEscapeGameManager was found.");
            }
        }

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
