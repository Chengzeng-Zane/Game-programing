using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// Handles the player's simple prototype melee attack.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Player object with PlayerController2D.
    /// It reads the J key, checks a small box in front of the player, and defeats any SimpleEnemy inside it.
    /// This is intentionally lightweight for the Level2_LootTutorial combat lesson.
    /// </remarks>
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField]
        private KeyCode attackKey = KeyCode.J; // Keyboard key used to perform the prototype attack.

        [SerializeField]
        private Vector2 attackBoxSize = new Vector2(0.65f, 0.5f); // Width and height of the attack hit area.

        [SerializeField]
        [FormerlySerializedAs("attackOffset")]
        private Vector2 attackBoxOffset = new Vector2(0.55f, 0f); // Offset from the player center toward the facing direction.

        [SerializeField]
        private int attackDamage = 1;

        [SerializeField]
        private LayerMask enemyLayers = ~0; // Optional layer filter; SimpleEnemy is still required.

        [SerializeField]
        private float attackActiveDelay = 0.1f; // Startup time before the sword hitbox becomes active.

        [SerializeField]
        private float attackActiveDuration = 0.12f; // Short window where the sword hitbox can deal damage.

        [SerializeField]
        private float attackCooldown = 0.4f; // Minimum time between attacks.

        [SerializeField]
        private bool debugLogs = true; // Prints simple combat messages for playtesting.

        private PlayerController2D playerController;
        private PlayerAnimationController animationController;
        private Coroutine attackRoutine;
        private float nextAttackTime;
        private bool attackHitboxActive;

        /// <summary>
        /// Unity event method called when the attack component is created.
        /// </summary>
        /// <remarks>
        /// Caches the PlayerController2D so the attack can use the player's facing direction.
        /// </remarks>
        private void Awake()
        {
            playerController = GetComponent<PlayerController2D>();
            animationController = GetComponentInChildren<PlayerAnimationController>();
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Reads the attack key and starts an attack when the cooldown has finished.
        /// </remarks>
        private void Update()
        {
            if (Input.GetKeyDown(attackKey))
            {
                Attack();
            }
        }

        /// <summary>
        /// Checks the attack area for SimpleEnemy components and defeats them.
        /// </summary>
        /// <remarks>
        /// Called when the player presses J. The attack is intentionally one-hit for the tutorial.
        /// </remarks>
        public void Attack()
        {
            if (attackRoutine != null || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine());
        }

        /// <summary>
        /// Description:
        /// Runs the delayed attack timing and active hitbox window.
        /// Inputs:
        /// none
        /// Returns:
        /// IEnumerator - Unity coroutine steps for one attack
        /// </summary>
        private IEnumerator AttackRoutine()
        {
            animationController?.PlayAttack();

            if (attackActiveDelay > 0f)
            {
                yield return new WaitForSeconds(attackActiveDelay);
            }

            attackHitboxActive = true;
            HashSet<SimpleEnemy> damagedEnemies = new HashSet<SimpleEnemy>();
            bool defeatedEnemy = false;
            float endTime = Time.time + Mathf.Max(0.01f, attackActiveDuration);

            while (Time.time < endTime)
            {
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
        /// Description:
        /// Checks the attack box and damages each enemy only once during this attack.
        /// Inputs:
        /// damagedEnemies - enemies already hit by this attack
        /// Returns:
        /// bool - true if at least one enemy was defeated or damaged
        /// </summary>
        private bool CheckAttackHits(HashSet<SimpleEnemy> damagedEnemies)
        {
            Vector2 center = AttackCenter();
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyLayers);
            bool defeatedEnemy = false;

            foreach (Collider2D hit in hits)
            {
                SimpleEnemy enemy = hit.GetComponent<SimpleEnemy>();
                if (enemy == null)
                {
                    enemy = hit.GetComponentInParent<SimpleEnemy>();
                }

                if (enemy == null ||
                    !IsInFacingDirection(enemy.transform.position) ||
                    !damagedEnemies.Add(enemy))
                {
                    continue;
                }

                enemy.TakeDamage(attackDamage);
                defeatedEnemy = true;
            }

            return defeatedEnemy;
        }

        /// <summary>
        /// Calculates the world-space center of the attack hit area.
        /// </summary>
        /// <returns>The center point used by Physics2D.OverlapBoxAll.</returns>
        private Vector2 AttackCenter()
        {
            bool facingRight = IsFacingRight();
            float direction = facingRight ? 1f : -1f;
            Vector2 offset = new Vector2(Mathf.Abs(attackBoxOffset.x) * direction, attackBoxOffset.y);
            return (Vector2)transform.position + offset;
        }

        /// <summary>
        /// Description:
        /// Checks whether a target is in front of the player.
        /// Inputs:
        /// targetPosition - world position of the target
        /// Returns:
        /// bool - true if the target is in the facing direction
        /// </summary>
        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = IsFacingRight() ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }

        /// <summary>
        /// Description:
        /// Reads the player's facing direction from PlayerController2D.
        /// Inputs:
        /// none
        /// Returns:
        /// bool - true if the player is facing right
        /// </summary>
        private bool IsFacingRight()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController2D>();
            }

            return playerController == null || playerController.FacingRight;
        }

        /// <summary>
        /// Unity editor event used to draw the attack box while the player object is selected.
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
