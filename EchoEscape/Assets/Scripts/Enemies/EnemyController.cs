using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Enemy Master Control Script, too Inspector Entrance to configure enemy parameters. It combines several sub-components of movement, enemy seeking, attack, health and animation into a complete enemy.
/// Gameplay logic: Awake Automatically complete and configure EnemyTargeting、EnemyMovement、EnemyAttack、EnemyHealth、EnemyAnimationController；Update determines whether the current enemy should die, pause, attack, pursue, patrol or standby.
/// Collaborates with: PlayerAttack will call Die/TakeDamage；EnemyAttack Called after hitting the player GameManager die; EnemyHealth Control the enemy to be defeated.
    /// </summary>
    public class EnemyController : MonoBehaviour
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
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            startPosition = transform.position;

            if (visualRenderer == null)
            {
// if Inspector No delay SpriteRenderer, automatically find the enemy's display layer from the sub-object.
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

// EnemyController It only does overall control; these sub-components are responsible for enemy hunting, animation, movement, attack and health.
            enemyTargeting = GetOrAdd<EnemyTargeting>();
            animationController = GetOrAdd<EnemyAnimationController>();
            enemyMovement = GetOrAdd<EnemyMovement>();
            enemyAttack = GetOrAdd<EnemyAttack>();
            enemyHealth = GetOrAdd<EnemyHealth>();

// Configure animation resources and facing direction rules. The specific playback frame is determined by EnemyAnimationController Tube.
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

// Configure movement parameters such as pursuit, patrol, and homecoming, EnemyController Each frame only reads its decisions.
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

// The attack component needs to know the enemy search, animation, attack box and death callback. It will go away after hitting the player. GameManager death process.
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

// The life component is responsible for blood deduction, death animation and disabling Collider, to avoid dead enemies from continuing to attack.
            enemyHealth.Configure(maxHealth, debugLogs, animationController, enemyAttack);
        }
        /// <summary>
/// Unity Called every frame. This handles input, timers, UI Real-time refresh of state or non-physics.
        /// </summary>
        private void Update()
        {
            if (enemyHealth.TickDeathAnimation())
            {
// While the death animation is playing, only the death frame is advanced and no longer moves or attacks.
                return;
            }

            if (enemyHealth.IsDefeated)
            {
// Stop logic directly when defeated but without animation.
                return;
            }

            if (ShouldPauseAfterPlayerDeath())
            {
// The enemy stops when the player dies to avoid further pursuit or repeated attacks during the death process.
                PauseAfterPlayerDeath();
                return;
            }

            if (enemyAttack.TickAttackAnimation())
            {
// attack animation/The attack frame period consists of EnemyAttack Take over and this frame no longer moves.
                return;
            }

            Transform target = enemyTargeting.GetPlayerTarget();
            bool wantsAttack = enemyMovement.Tick(target) == EnemyMovement.Decision.Attack;
            if (wantsAttack && enemyAttack.TryStartAttack(target))
            {
// After the mobile component determines that the distance is close enough, the attack component then checks the cooldown, facing direction and attack box.
                return;
            }

// Plays on standby when there is no attack or death. /Floating animation.
            animationController.TickIdle();
        }
        /// <summary>
/// 2D Trigger Called when first entering. Here, it is decided whether to trigger teaching, mechanism, treasure chest, death or clearance based on the entering object.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            enemyAttack.TryAttackCollider(other);
        }
        /// <summary>
/// 2D Trigger Continuously called during the stay. This is used to process trigger logic that requires continuous checking to avoid missed judgments during high-speed movement.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerStay2D(Collider2D other)
        {
            enemyAttack.TryAttackCollider(other);
        }
        /// <summary>
/// Puts the current object into a dead or defeated state. To the enemy it usually equates to taking enough damage.
        /// </summary>
        public void Die()
        {
            TakeDamage(1);
        }
        /// <summary>
/// Receive damage and deduct health points. After the health value reaches zero, the player enters the death or defeat process.
        /// </summary>
/// <param name="damage">The amount of damage received this time. </param>
        public void TakeDamage(int damage)
        {
            enemyHealth.TakeDamage(damage);
        }
        /// <summary>
/// Suspend enemy state after player death. It stops the attack coroutine and attack box and returns the enemy to standby vision to avoid killing the player repeatedly.
        /// </summary>
/// <param name="stopAttackRoutine">stopAttackRoutine Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private void PauseAfterPlayerDeath(bool stopAttackRoutine = true)
        {
            if (pausedAfterPlayerDeath)
            {
                return;
            }

            pausedAfterPlayerDeath = true;
// Stop the current attack coroutine and attack box to prevent the player from being hit repeatedly after death.
            enemyAttack.StopAttack(stopAttackRoutine);
            animationController.PlayIdle();

            if (debugLogs)
            {
                Debug.Log($"{name} paused after triggering player death.");
            }
        }
        /// <summary>
/// determines whether a certain process should be executed based on the current game state.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool ShouldPauseAfterPlayerDeath()
        {
            return enemyAttack.HasKilledPlayer ||
                (EchoEscapeGameManager.Instance != null && EchoEscapeGameManager.Instance.IsPlayerDeadOrDying);
        }
        /// <summary>
/// Update the enemy's facing direction according to the horizontal movement direction, and synchronize the enemy's animation components.
        /// </summary>
/// <param name="horizontalDirection">horizontalDirection Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private void UpdateFacing(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) <= 0.05f)
            {
// Small direction changes are ignored, preventing enemies from rapidly shaking left and right when approaching the player or returning home.
                return;
            }

            facingRight = horizontalDirection > 0f;
            animationController.SetFacing(facingRight);
        }
        /// <summary>
/// Returns whether the enemy is currently facing right. EnemyAttack Use this to calculate whether the attack box should appear on the left or right.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsFacingRight()
        {
            return facingRight;
        }
        /// <summary>
/// determines whether the target is in front of the enemy. Enemy attacks should only hit players in front of them, not behind them.
        /// </summary>
/// <param name="targetPosition">Target world coordinates, used to determine distance or facing direction. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = facingRight ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }
        /// <summary>
/// Calculate the center point of the enemy's attack frame. Scene view of Gizmos Use this to display attack range.
        /// </summary>
/// <returns>Returns 2D coordinates or dimensions. </returns>
        private Vector2 AttackBoxCenter()
        {
            float direction = facingRight ? 1f : -1f;
// attackOffset. x Just need to be in Inspector If you fill in a positive number, the code will automatically flip left and right according to the facing direction.
            Vector2 offset = new Vector2(Mathf.Abs(enemyAttackOffset.x) * direction, enemyAttackOffset.y);
            return (Vector2)transform.position + offset;
        }
        /// <summary>
/// Only in the editor Scene Draw auxiliary lines in the view. It helps debug attack frames, detection ranges, etc. , without affecting official game operation.
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
// If the split component is not manually hung in the scene, EnemyController It will be automatically added to avoid missing components in old scenarios.
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
