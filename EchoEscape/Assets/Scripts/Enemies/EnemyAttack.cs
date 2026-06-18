using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Enemy Attack Component. It is responsible for determining whether the player has entered the attack range, playing the attack pre-roll, opening the attack determination box, and killing the player after a hit.
/// Gameplay logic: The enemy will not kill the player immediately if it touches the player, but first checks the distance, cooldown, direction and attack box; if the real player is in front of the enemy during the activation of the attack box, call GameManager unified death process. Echo will be filtered and will not trigger player death.
/// Collaborates with: EnemyController Responsible for configuring parameters; EnemyTargeting Responsible for finding players; EnemyAnimationController play attack; EchoEscapeGameManager dealing with death, UI and overloading.
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
/// Receive the parameters passed in by the external script and configure the current component to the state required by this scene or this enemy.
        /// </summary>
/// <param name="enemyTargeting">enemyTargeting Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="enemyAnimation">enemyAnimation Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="boxSize">boxSize Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="boxOffset">boxOffset Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="activeDelay">activeDelay Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="activeDuration">activeDuration Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="cooldown">cooldown Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="range">range Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="leash">leash Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="homePosition">homePosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="reason">cause of death or event, used for death UI, status prompts and debugging logs. </param>
/// <param name="logs">logs Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingRightGetter">facingRightGetter Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingUpdater">facingUpdater Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingCheck">facingCheck Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="pauseCheck">pauseCheck Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="pauseCallback">pauseCallback Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
/// Attempts to perform an operation that may fail; if the conditions are not met, exit safely or return failure.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        public void TryAttackCollider(Collider2D other)
        {
            if (shouldPause())
            {
// When the player has entered the death process, the enemy attack system stops to avoid duplication. KillPlayer。
                pauseAfterPlayerDeath(true);
                return;
            }

            PlayerController2D player = targeting.GetPlayer(other);
            if (player == null)
            {
// EnemyTargeting Will filter Echo and non-players Collider。
                return;
            }

// Touching an enemy does not kill them directly, but instead attempts to initiate an attack with a forward pan and an attack frame.
            TryStartAttack(player.transform);
        }
        /// <summary>
/// Try to start an enemy attack. It checks whether it is already attacking, whether the target is valid, and whether it is within attack range/Within the towing range, and whether the cooling is over.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        public bool TryStartAttack(Transform target)
        {
            if (shouldPause())
            {
// No new attacks will be launched during the death process to keep player death feedback clean.
                pauseAfterPlayerDeath(true);
                return false;
            }

            if (IsBusy || target == null || !CanStartAttack(target) || Time.time < nextAttackTime)
            {
// You cannot start attacking if you are busy, have no target, are at inappropriate distance, or are on cooldown.
                return false;
            }

// Face the player before attacking so that the attack box appears on the correct side.
            updateFacing(target.position.x - transform.position.x);
            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine(target));
            return true;
        }
        /// <summary>
/// Advance the enemy's attack animation. EnemyController It is called every frame and if it returns true, indicating that the attack system is taking over the enemy's state in this frame.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
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
/// Stops current enemy attack. Called when the player dies or the enemy is defeated, ensuring that the attack box closes immediately.
        /// </summary>
/// <param name="stopRoutine">stopRoutine Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
/// A complete enemy attack process: play the forward shaking animation, briefly open the attack box, and trigger a unified death process after hitting the player.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator AttackRoutine(Transform target)
        {
// Play the attack animation first and shake forward+The effective time is told to the animation system to ensure visual coverage of the entire attack process.
            animationController.PlayAttack(attackActiveDelay + attackActiveDuration);

            if (attackActiveDelay > 0f)
            {
// During the forward shaking phase, only the animation is played and no damage is caused, allowing the player some reaction time.
                yield return new WaitForSeconds(attackActiveDelay);
            }

            if (shouldPause())
            {
// If the player dies or the enemy is paused during the forward roll, the attack is canceled.
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
// The attack frame is checked frame by frame while the attack frame is valid to avoid only detecting one frame and missing a judgment when the player is moving at high speed.
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
/// Find the real player in the enemy's attack box and confirm that the player is in front of the enemy; called after hitting KillPlayer。
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <param name="playerIsInFront">playerIsInFront Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="playerInsideHitbox">playerInsideHitbox Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool TryHitPlayerInAttackBox(Transform target, out bool playerIsInFront, out bool playerInsideHitbox)
        {
            playerIsInFront = target != null && isInFacingDirection(target.position);
            playerInsideHitbox = false;

// The enemy attack box is a OverlapBox, size and offset from EnemyController of Inspector configuration.
            Collider2D[] hits = Physics2D.OverlapBoxAll(AttackBoxCenter(), attackBoxSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController2D player = targeting.GetPlayer(hits[i]);
                if (player == null)
                {
// Echo, non-player objects, and the enemy's own Collider They will all be filtered here.
                    continue;
                }

                playerInsideHitbox = true;
                playerIsInFront = isInFacingDirection(player.transform.position);
                if (!playerIsInFront)
                {
// Even if the player is at the edge of the attack box, if they are not in front of the enemy, it will not count as a hit.
                    continue;
                }

// After hitting the target, go to the unified death entrance and do not show it in the enemy script. UI Or reload the scene.
                KillPlayer();
                HasKilledPlayer = true;
                pauseAfterPlayerDeath(false);
                return true;
            }

            return false;
        }
        /// <summary>
/// determines whether the current conditions allow an action to be performed, such as opening a box, attacking, pressing a button, or switching states.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool CanStartAttack(Transform target)
        {
            float effectiveAttackRange = Mathf.Max(0f, attackRange);
            float attackRangeSquared = effectiveAttackRange * effectiveAttackRange;
            if (((Vector2)transform.position - (Vector2)target.position).sqrMagnitude > attackRangeSquared)
            {
// Use square distance to avoid square root every frame, and the performance is more stable.
                return false;
            }

            float effectiveLeashDistance = Mathf.Max(0f, leashDistance);
            float leashDistanceSquared = effectiveLeashDistance * effectiveLeashDistance;
// leash Limit enemies from being too far away from the spawn point to prevent players from drawing enemies out of the design area.
            return ((Vector2)startPosition - (Vector2)target.position).sqrMagnitude <= leashDistanceSquared;
        }
        /// <summary>
/// According to the enemy's position, facing direction and attackBoxOffset Calculate the attack box center point.
        /// </summary>
/// <returns>Returns 2D coordinates or dimensions. </returns>
        private Vector2 AttackBoxCenter()
        {
            bool facingRight = getFacingRight();
            float direction = facingRight ? 1f : -1f;
// x Offset flips based on enemy facing direction, so the same attackBoxOffset Can support left and right attacks at the same time.
            Vector2 offset = new Vector2(Mathf.Abs(attackBoxOffset.x) * direction, attackBoxOffset.y);
            return (Vector2)transform.position + offset;
        }
        /// <summary>
/// Trigger player death or related death process. In this game death should be done as soon as possible GameManager unified process.
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
// GameManager Responsible for the complete death process: animation, UI、pending loot Lose or reload the current level.
                EchoEscapeGameManager.Instance.KillPlayer(deathReason);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("Cursed Ghost attacked the player, but no EchoEscapeGameManager was found.");
            }
        }
        /// <summary>
/// Output debugging logs to help confirm whether the process is executed as expected during testing.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <param name="playerIsInFront">playerIsInFront Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="playerInsideHitbox">playerInsideHitbox Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="hitPlayer">hitPlayer Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
