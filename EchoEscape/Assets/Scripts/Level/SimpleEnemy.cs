using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// Represents a simple tutorial enemy that can hurt the player or be defeated by an attack.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Enemy object in Level2_LootTutorial.
    /// The enemy can stay still or patrol a short distance. Touching it kills the player, while PlayerAttack defeats it.
    /// </remarks>
    public class SimpleEnemy : MonoBehaviour
    {
        [SerializeField]
        private bool patrol; // True if this enemy should move left and right slowly.

        [SerializeField]
        private float patrolSpeed = 1f; // Speed multiplier for optional patrol movement.

        [SerializeField]
        private float patrolDistance = 1.25f; // Maximum distance from the start position while patrolling.

        [SerializeField]
        private string deathReason = "touched a slime"; // Message used when the player touches this enemy.

        [SerializeField]
        private bool debugLogs = true; // Prints simple enemy messages for playtesting.

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
        private Sprite[] idleFrames = Array.Empty<Sprite>();
        private Sprite[] attackFrames = Array.Empty<Sprite>();
        private Sprite[] deathFrames = Array.Empty<Sprite>();
        private Sprite[] currentFrames = Array.Empty<Sprite>();
        private int currentFrameIndex;
        private float frameTimer;
        private float deathTimer;
        private float attackAnimationTimer;
        private float nextAttackTime;
        private Transform playerTarget;
        private Coroutine attackRoutine;
        private int currentHealth;
        private bool defeated;
        private bool playingDeathAnimation;
        private bool attackHitboxActive;
        private bool hasKilledPlayer;
        private bool pausedAfterPlayerDeath;
        private EnemyVisualState visualState = EnemyVisualState.Idle;

        private enum EnemyVisualState
        {
            Idle,
            Attack,
            Death
        }

        /// <summary>
        /// Unity event method called when the enemy is created.
        /// </summary>
        /// <remarks>
        /// Stores the start position used by optional patrol movement.
        /// </remarks>
        private void Awake()
        {
            startPosition = transform.position;
            currentHealth = Mathf.Max(1, maxHealth);

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            idleFrames = LoadFrames(idleFramesPath);
            if (idleFrames.Length == 0)
            {
                idleFrames = LoadFrames(fallbackIdleFramesPath);
            }

            deathFrames = LoadFrames(deathFramesPath);
            attackFrames = LoadFrames(attackFramesPath);
            ApplyFacingToVisual();
            SetVisualState(EnemyVisualState.Idle);
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Moves the enemy in a small patrol pattern when patrol is enabled.
        /// </remarks>
        private void Update()
        {
            if (playingDeathAnimation)
            {
                AdvanceFrame(deathFramesPerSecond, false);
                deathTimer -= Time.deltaTime;
                if (deathTimer <= 0f)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (defeated)
            {
                return;
            }

            if (ShouldPauseAfterPlayerDeath())
            {
                PauseAfterPlayerDeath();
                return;
            }

            if (attackRoutine != null || attackAnimationTimer > 0f)
            {
                attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);
                SetVisualState(EnemyVisualState.Attack);
                AdvanceFrame(attackFramesPerSecond, false);
                return;
            }

            SetVisualState(EnemyVisualState.Idle);
            UpdateEnemyMovement();
            AdvanceFrame(idleFramesPerSecond, true);
        }

        private void UpdateEnemyMovement()
        {
            Transform target = GetPlayerTarget();
            if (chasePlayer && target != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, target.position);
                bool withinLeash = Vector2.Distance(startPosition, target.position) <= leashDistance;

                if (distanceToPlayer <= detectionRange && withinLeash)
                {
                    FaceTarget(target);

                    if (distanceToPlayer <= attackRange)
                    {
                        StartAttack(target);
                        return;
                    }

                    ChasePlayer(target);
                    return;
                }
            }

            if (patrol)
            {
                Patrol();
            }
            else
            {
                ReturnTowardStart();
            }
        }

        private void Patrol()
        {
            float offset = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
            transform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
        }

        private void ChasePlayer(Transform target)
        {
            Vector3 toPlayer = target.position - transform.position;
            Vector3 direction = new Vector3(toPlayer.x, toPlayer.y * verticalChaseStrength, 0f);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.position += direction.normalized * chaseSpeed * Time.deltaTime;
            UpdateFacing(direction.x);
        }

        private void ReturnTowardStart()
        {
            if (Vector2.Distance(transform.position, startPosition) <= 0.03f)
            {
                transform.position = startPosition;
                return;
            }

            Vector3 direction = startPosition - transform.position;
            transform.position += direction.normalized * returnSpeed * Time.deltaTime;
            UpdateFacing(direction.x);
        }

        /// <summary>
        /// Unity physics event called when another 2D collider enters the enemy trigger.
        /// </summary>
        /// <param name="other">The collider that touched the enemy.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAttackCollider(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryAttackCollider(other);
        }

        private void TryAttackCollider(Collider2D other)
        {
            if (defeated)
            {
                return;
            }

            if (ShouldPauseAfterPlayerDeath())
            {
                PauseAfterPlayerDeath();
                return;
            }

            PlayerController2D player = GetPlayer(other);
            if (player == null)
            {
                return;
            }

            StartAttack(player.transform);
        }

        private void StartAttack(Transform target)
        {
            if (ShouldPauseAfterPlayerDeath())
            {
                PauseAfterPlayerDeath();
                return;
            }

            if (attackRoutine != null || attackAnimationTimer > 0f)
            {
                return;
            }

            FaceTarget(target);

            if (!CanStartAttack(target))
            {
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine(target));
        }

        private IEnumerator AttackRoutine(Transform target)
        {
            PlayAttackAnimation(enemyAttackActiveDelay + enemyAttackActiveDuration);

            if (enemyAttackActiveDelay > 0f)
            {
                yield return new WaitForSeconds(enemyAttackActiveDelay);
            }

            if (defeated || playingDeathAnimation || ShouldPauseAfterPlayerDeath())
            {
                attackRoutine = null;
                yield break;
            }

            attackHitboxActive = true;
            bool hitPlayer = false;
            bool playerIsInFront = target != null && IsInFacingDirection(target.position);
            bool playerInsideHitbox = false;
            float endTime = Time.time + Mathf.Max(0.01f, enemyAttackActiveDuration);
            while (Time.time < endTime && !defeated && !playingDeathAnimation && !ShouldPauseAfterPlayerDeath())
            {
                if (!hitPlayer && TryHitPlayerInAttackBox(target, out playerIsInFront, out playerInsideHitbox))
                {
                    hitPlayer = true;
                }

                yield return null;
            }

            attackHitboxActive = false;
            attackRoutine = null;
            LogEnemyAttackCheck(target, playerIsInFront, playerInsideHitbox, hitPlayer);
        }

        private bool TryHitPlayerInAttackBox(Transform target, out bool playerIsInFront, out bool playerInsideHitbox)
        {
            playerIsInFront = target != null && IsInFacingDirection(target.position);
            playerInsideHitbox = false;

            Collider2D[] hits = Physics2D.OverlapBoxAll(AttackBoxCenter(), enemyAttackBoxSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController2D player = GetPlayer(hits[i]);
                if (player == null)
                {
                    continue;
                }

                playerInsideHitbox = true;
                playerIsInFront = IsInFacingDirection(player.transform.position);
                if (!playerIsInFront)
                {
                    continue;
                }

                KillPlayer();
                hasKilledPlayer = true;
                PauseAfterPlayerDeath(false);
                return true;
            }

            return false;
        }

        private void KillPlayer()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
                EchoEscapeGameManager.Instance.KillPlayer(deathReason);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("Cursed Ghost attacked the player, but no EchoEscapeGameManager was found.");
            }
        }

        private bool ShouldPauseAfterPlayerDeath()
        {
            return hasKilledPlayer ||
                (EchoEscapeGameManager.Instance != null && EchoEscapeGameManager.Instance.IsPlayerDeadOrDying);
        }

        private void PauseAfterPlayerDeath(bool stopAttackRoutine = true)
        {
            if (pausedAfterPlayerDeath)
            {
                return;
            }

            pausedAfterPlayerDeath = true;
            attackHitboxActive = false;
            attackAnimationTimer = 0f;

            if (stopAttackRoutine && attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            SetVisualState(EnemyVisualState.Idle);

            if (debugLogs)
            {
                Debug.Log($"{name} paused after triggering player death.");
            }
        }

        private bool CanStartAttack(Transform target)
        {
            if (target == null)
            {
                return false;
            }

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

        /// <summary>
        /// Defeats the enemy and removes it from active gameplay.
        /// </summary>
        /// <remarks>
        /// Called by PlayerAttack when the enemy is inside the attack box.
        /// </remarks>
        public void Die()
        {
            TakeDamage(1);
        }

        /// <summary>
        /// Applies damage from the player attack and starts the simple death feedback.
        /// </summary>
        /// <param name="damage">Amount of damage dealt by the player.</param>
        public void TakeDamage(int damage)
        {
            if (defeated)
            {
                return;
            }

            currentHealth -= Mathf.Max(1, damage);
            if (currentHealth > 0)
            {
                return;
            }

            defeated = true;
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            attackHitboxActive = false;
            attackAnimationTimer = 0f;

            if (debugLogs)
            {
                Debug.Log("Enemy defeated.");
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            if (deathFrames.Length == 0 || visualRenderer == null)
            {
                gameObject.SetActive(false);
                return;
            }

            playingDeathAnimation = true;
            deathTimer = deathFrames.Length / Mathf.Max(1f, deathFramesPerSecond);
            SetVisualState(EnemyVisualState.Death);
        }

        private PlayerController2D GetPlayer(Collider2D other)
        {
            if (other == null ||
                HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null)
            {
                return null;
            }

            return other.GetComponent<PlayerController2D>() ?? other.GetComponentInParent<PlayerController2D>();
        }

        private void PlayAttackAnimation(float minimumDuration)
        {
            if (attackFrames.Length == 0)
            {
                return;
            }

            float frameDuration = attackFrames.Length / Mathf.Max(1f, attackFramesPerSecond);
            attackAnimationTimer = Mathf.Max(attackAnimationDuration, Mathf.Max(minimumDuration, frameDuration));
            SetVisualState(EnemyVisualState.Attack);
        }

        private void SetVisualState(EnemyVisualState nextState)
        {
            if (visualState == nextState && currentFrames != null && currentFrames.Length > 0)
            {
                return;
            }

            visualState = nextState;
            currentFrames = nextState switch
            {
                EnemyVisualState.Attack => attackFrames.Length > 0 ? attackFrames : idleFrames,
                EnemyVisualState.Death => deathFrames.Length > 0 ? deathFrames : idleFrames,
                _ => idleFrames
            };

            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }

        private void AdvanceFrame(float framesPerSecond, bool loop)
        {
            if (visualRenderer == null || currentFrames == null || currentFrames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }

        private void ApplyCurrentFrame()
        {
            if (visualRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                visualRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }

        private Transform GetPlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                return playerTarget;
            }

            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            playerTarget = player != null ? player.transform : null;
            return playerTarget;
        }

        private void UpdateFacing(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) <= 0.05f)
            {
                return;
            }

            facingRight = horizontalDirection > 0f;
            ApplyFacingToVisual();
        }

        private void FaceTarget(Transform target)
        {
            if (target != null)
            {
                UpdateFacing(target.position.x - transform.position.x);
            }
        }

        private Vector2 AttackBoxCenter()
        {
            float direction = facingRight ? 1f : -1f;
            Vector2 offset = new Vector2(Mathf.Abs(enemyAttackOffset.x) * direction, enemyAttackOffset.y);
            return (Vector2)transform.position + offset;
        }

        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = facingRight ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }

        private void ApplyFacingToVisual()
        {
            if (visualRenderer != null)
            {
                visualRenderer.flipX = spriteDefaultFacesRight ? !facingRight : facingRight;
            }
        }

        private void LogEnemyAttackCheck(Transform target, bool playerIsInFront, bool playerInsideHitbox, bool hitPlayer)
        {
            if (!debugLogs)
            {
                return;
            }

            string playerPosition = target != null ? target.position.ToString("F2") : "none";
            bool flipX = visualRenderer != null && visualRenderer.flipX;
            Debug.Log(
                $"[EnemyAttackCheck] enemy={name}, enemyPos={transform.position.ToString("F2")}, playerPos={playerPosition}, " +
                $"facingRight={facingRight}, flipX={flipX}, spriteDefaultFacesRight={spriteDefaultFacesRight}, " +
                $"playerIsInFront={playerIsInFront}, attackBoxCenter={AttackBoxCenter().ToString("F2")}, " +
                $"attackBoxSize={enemyAttackBoxSize.ToString("F2")}, playerInsideHitbox={playerInsideHitbox}, final={(hitPlayer ? "hit" : "miss")}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, detectionRange));

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, attackRange));

            Vector2 center = AttackBoxCenter();
            Gizmos.color = attackHitboxActive
                ? new Color(1f, 0f, 0f, 0.32f)
                : new Color(1f, 0.45f, 0.05f, 0.18f);
            Gizmos.DrawCube(center, enemyAttackBoxSize);
            Gizmos.color = attackHitboxActive
                ? new Color(1f, 0f, 0f, 0.95f)
                : new Color(1f, 0.45f, 0.05f, 0.85f);
            Gizmos.DrawWireCube(center, enemyAttackBoxSize);
        }

        private bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private static Sprite[] LoadFrames(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return Array.Empty<Sprite>();
            }

            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
