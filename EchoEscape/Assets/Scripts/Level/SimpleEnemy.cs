using System;
using UnityEngine;

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
        private float attackRange = 1f;

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
        private int currentHealth;
        private bool defeated;
        private bool playingDeathAnimation;
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

            if (attackAnimationTimer > 0f)
            {
                attackAnimationTimer -= Time.deltaTime;
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

                if (distanceToPlayer <= attackRange && withinLeash)
                {
                    AttackPlayer();
                    return;
                }

                if (distanceToPlayer <= detectionRange && withinLeash)
                {
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
            if (defeated || !IsPlayer(other))
            {
                return;
            }

            AttackPlayer();
        }

        private void AttackPlayer()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            PlayAttackAnimation();

            if (EchoEscapeGameManager.Instance != null)
            {
                EchoEscapeGameManager.Instance.KillPlayer(deathReason);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("Cursed Ghost attacked the player, but no EchoEscapeGameManager was found.");
            }
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

        /// <summary>
        /// Checks whether a collider belongs to the real player.
        /// </summary>
        /// <param name="other">Collider being checked.</param>
        /// <returns>True if the collider is on the Player or its child object; otherwise false.</returns>
        private bool IsPlayer(Collider2D other)
        {
            return HasTag(other, "Player") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null;
        }

        /// <summary>
        /// Safely checks a tag without throwing if the tag is missing from Project Settings.
        /// </summary>
        /// <param name="other">Collider whose object or root should be checked.</param>
        /// <param name="tagName">Tag name to compare.</param>
        /// <returns>True if the tag matches; otherwise false.</returns>
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

        private void PlayAttackAnimation()
        {
            if (attackFrames.Length == 0)
            {
                return;
            }

            attackAnimationTimer = Mathf.Max(attackAnimationDuration, attackFrames.Length / Mathf.Max(1f, attackFramesPerSecond));
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
            if (visualRenderer != null && Mathf.Abs(horizontalDirection) > 0.05f)
            {
                visualRenderer.flipX = horizontalDirection < 0f;
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
