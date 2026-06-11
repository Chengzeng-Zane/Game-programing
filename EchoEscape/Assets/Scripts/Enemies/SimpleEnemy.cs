using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// Coordinates a simple tutorial enemy from smaller movement, attack, health, targeting, and animation components.
    /// </summary>
    /// <remarks>
    /// Keep this component on existing scene enemies. It preserves serialized scene data while delegating behavior.
    /// </remarks>
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
        /// Unity event method called when the enemy is created.
        /// </summary>
        private void Awake()
        {
            startPosition = transform.position;

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            enemyTargeting = GetOrAdd<EnemyTargeting>();
            animationController = GetOrAdd<EnemyAnimationController>();
            enemyMovement = GetOrAdd<EnemyMovement>();
            enemyAttack = GetOrAdd<EnemyAttack>();
            enemyHealth = GetOrAdd<EnemyHealth>();

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

            enemyHealth.Configure(maxHealth, debugLogs, animationController, enemyAttack);
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        private void Update()
        {
            if (enemyHealth.TickDeathAnimation())
            {
                return;
            }

            if (enemyHealth.IsDefeated)
            {
                return;
            }

            if (ShouldPauseAfterPlayerDeath())
            {
                PauseAfterPlayerDeath();
                return;
            }

            if (enemyAttack.TickAttackAnimation())
            {
                return;
            }

            Transform target = enemyTargeting.GetPlayerTarget();
            bool wantsAttack = enemyMovement.Tick(target) == EnemyMovement.Decision.Attack;
            if (wantsAttack && enemyAttack.TryStartAttack(target))
            {
                return;
            }

            animationController.TickIdle();
        }

        /// <summary>
        /// Unity physics event called when another 2D collider enters the enemy trigger.
        /// </summary>
        /// <param name="other">The collider that touched the enemy.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            enemyAttack.TryAttackCollider(other);
        }

        /// <summary>
        /// Unity physics event called while another collider stays inside the enemy trigger.
        /// </summary>
        /// <param name="other">The collider currently inside the trigger.</param>
        private void OnTriggerStay2D(Collider2D other)
        {
            enemyAttack.TryAttackCollider(other);
        }

        /// <summary>
        /// Defeats the enemy and removes it from active gameplay.
        /// </summary>
        public void Die()
        {
            TakeDamage(1);
        }

        /// <summary>
        /// Applies damage from the player attack.
        /// </summary>
        /// <param name="damage">Amount of damage dealt by the player.</param>
        public void TakeDamage(int damage)
        {
            enemyHealth.TakeDamage(damage);
        }

        /// <summary>
        /// Stops enemy behavior after the player is already dying.
        /// </summary>
        /// <param name="stopAttackRoutine">True to stop the active attack coroutine.</param>
        private void PauseAfterPlayerDeath(bool stopAttackRoutine = true)
        {
            if (pausedAfterPlayerDeath)
            {
                return;
            }

            pausedAfterPlayerDeath = true;
            enemyAttack.StopAttack(stopAttackRoutine);
            animationController.PlayIdle();

            if (debugLogs)
            {
                Debug.Log($"{name} paused after triggering player death.");
            }
        }

        /// <summary>
        /// Checks whether enemy behavior should stop because the player is already dying.
        /// </summary>
        /// <returns>True if enemy AI should pause.</returns>
        private bool ShouldPauseAfterPlayerDeath()
        {
            return enemyAttack.HasKilledPlayer ||
                (EchoEscapeGameManager.Instance != null && EchoEscapeGameManager.Instance.IsPlayerDeadOrDying);
        }

        /// <summary>
        /// Updates which horizontal direction the enemy is facing.
        /// </summary>
        /// <param name="horizontalDirection">Positive for right, negative for left.</param>
        private void UpdateFacing(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) <= 0.05f)
            {
                return;
            }

            facingRight = horizontalDirection > 0f;
            animationController.SetFacing(facingRight);
        }

        /// <summary>
        /// Returns true if the enemy is facing right.
        /// </summary>
        /// <returns>True when facing right.</returns>
        private bool IsFacingRight()
        {
            return facingRight;
        }

        /// <summary>
        /// Checks whether a target position is in front of the enemy.
        /// </summary>
        /// <param name="targetPosition">World position to test.</param>
        /// <returns>True if the position is in front of the enemy.</returns>
        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = facingRight ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }

        /// <summary>
        /// Calculates the center of the enemy attack hitbox.
        /// </summary>
        /// <returns>The world-space attack hitbox center.</returns>
        private Vector2 AttackBoxCenter()
        {
            float direction = facingRight ? 1f : -1f;
            Vector2 offset = new Vector2(Mathf.Abs(enemyAttackOffset.x) * direction, enemyAttackOffset.y);
            return (Vector2)transform.position + offset;
        }

        /// <summary>
        /// Unity editor event used to draw enemy detection and attack ranges.
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

        /// <summary>
        /// Gets an existing component or adds it when missing.
        /// </summary>
        /// <typeparam name="T">Component type to fetch.</typeparam>
        /// <returns>The existing or newly added component.</returns>
        private T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
