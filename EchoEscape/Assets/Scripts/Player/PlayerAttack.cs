using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Player Attack Script. It is responsible for button attacks, attack animations, attack forward swings, attack check boxes, and enemy damage.
/// Gameplay logic: After the player presses the attack button, the attack animation will be played first and then wait. attackActiveDelay Then briefly open the attack box; the attack box uses OverlapBoxAll Detect enemies and only hit enemies in the direction the player is facing to avoid accidental damage to enemies behind them.
/// Collaborates with: PlayerAnimationController Play attack vision; EnemyController/EnemyHealth receive harm; OnDrawGizmosSelected Helping you Scene Look at the size of the attack box.
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField]
private KeyCode attackKey = KeyCode.J; // Player attack key.

        [SerializeField]
private Vector2 attackBoxSize = new Vector2(0.85f, 0.45f); // The width and height of the attack box.

        [SerializeField]
        [FormerlySerializedAs("attackOffset")]
private Vector2 attackBoxOffset = new Vector2(0.62f, -0.28f); // The offset of the attack determination box relative to the center of the player will flip left and right depending on the direction.

        [SerializeField]
        private int attackDamage = 1;

        [SerializeField]
private LayerMask enemyLayers = ~0; // Optional enemy layer filtering; enemy scripts are still checked eventually.

        [SerializeField]
private float attackActiveDelay = 0.1f; // After pressing attack, the forward swing time before the judgment box actually takes effect.

        [SerializeField]
private float attackActiveDuration = 0.12f; // The duration of damage the attack box can cause.

        [SerializeField]
private float attackCooldown = 0.4f; // The minimum interval between attacks.

        [SerializeField]
private bool debugLogs = true; // Whether to output attack debugging logs to facilitate testing of attack frames.

        private PlayerController2D playerController;
        private PlayerAnimationController animationController;
        private Coroutine attackRoutine;
        private float nextAttackTime;
        private bool attackHitboxActive;
        /// <summary>
/// Caching player controllers and player animation controllers. The attack needs to know the direction it is facing, and it also needs to tell the animation script to play the attack frame.
        /// </summary>
        private void Awake()
        {
            playerController = GetComponent<PlayerController2D>();
            animationController = GetComponentInChildren<PlayerAnimationController>();
        }
        /// <summary>
/// Attack keys and cooldowns are checked every frame. A new attack will only be started when the game is not paused, no attack coroutine is executing, and the cooldown is over.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(attackKey))
            {
                Attack();
            }
        }
        /// <summary>
/// Starts a player attack. It records the cooldown, plays the attack animation, and then starts AttackRoutine Controls forward pan and decision box.
        /// </summary>
        public void Attack()
        {
            if (attackRoutine != null || Time.time < nextAttackTime)
            {
// It will not accept new attacks when it is attacking or the cooldown has not ended, preventing multiple button presses from causing multiple damage.
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine());
        }
        /// <summary>
/// Attack coroutine. wait first attackActiveDelay Indicates that the weapon is swung forward, and then attackActiveDuration Repeatedly check the attack box hit, and finally turn off the attack state.
        /// </summary>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator AttackRoutine()
        {
// The attack action is played visually first, and the attack frame that actually causes damage is opened later, forming a "swinging sword forward".
            animationController?.PlayAttack();

            if (attackActiveDelay > 0f)
            {
                yield return new WaitForSeconds(attackActiveDelay);
            }

            attackHitboxActive = true;
// The same enemy can only be wounded once during an attack, even if the attack box lasts for multiple frames detecting it.
            HashSet<EnemyController> damagedEnemies = new HashSet<EnemyController>();
            bool defeatedEnemy = false;
            float endTime = Time.time + Mathf.Max(0.01f, attackActiveDuration);

            while (Time.time < endTime)
            {
// Detect frame by frame in a short effective time to improve hit stability when moving at high speed.
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
/// use Physics2D. OverlapBoxAll Detect objects within the attack box. It will filter out already wounded enemies and check if the enemy is in the direction the player is facing, calling enemy damage after a hit.
        /// </summary>
/// <param name="damagedEnemies">damagedEnemies Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool CheckAttackHits(HashSet<EnemyController> damagedEnemies)
        {
            Vector2 center = AttackCenter();
// OverlapBoxAll It is the player attack box: the size is given by attackBoxSize controlled, centered by attackBoxOffset and facing direction decisions.
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, enemyLayers);
            bool defeatedEnemy = false;

            foreach (Collider2D hit in hits)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy == null)
                {
// some enemies Collider It is on the child object, so if it cannot be found, it will continue to search the parent.
                    enemy = hit.GetComponentInParent<EnemyController>();
                }

                if (enemy == null ||
                    !IsInFacingDirection(enemy.transform.position) ||
                    !damagedEnemies.Add(enemy))
                {
// Filter out non-enemies, enemies behind you, and enemies already hit by this attack.
                    continue;
                }

// After the enemy is injured, EnemyHealth To determine death, the player attack script does not directly disable the enemy.
                enemy.TakeDamage(attackDamage);
                defeatedEnemy = true;
            }

            return defeatedEnemy;
        }
        /// <summary>
/// Calculate the attack box center point. The attack box is based on player position and attackBoxOffset, and flip left and right depending on the player's facing direction.
        /// </summary>
/// <returns>Returns 2D coordinates or dimensions. </returns>
        private Vector2 AttackCenter()
        {
            bool facingRight = IsFacingRight();
            float direction = facingRight ? 1f : -1f;
// x The offset takes the absolute value and then multiplies the direction to ensure Inspector Enter a positive number and it will automatically flip left and right.
            Vector2 offset = new Vector2(Mathf.Abs(attackBoxOffset.x) * direction, attackBoxOffset.y);
            return (Vector2)transform.position + offset;
        }
        /// <summary>
/// Determines whether the target point is on the side the player is facing. This restriction makes attacks more visually appealing and prevents players from hitting enemies behind them.
        /// </summary>
/// <param name="targetPosition">Target world coordinates, used to determine distance or facing direction. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsInFacingDirection(Vector2 targetPosition)
        {
            float direction = IsFacingRight() ? 1f : -1f;
            return (targetPosition.x - transform.position.x) * direction > 0.01f;
        }
        /// <summary>
/// Reads whether the player is currently facing right. if PlayerController2D If it exists, use it FacingRight; Otherwise, it defaults to the right.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsFacingRight()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController2D>();
            }

            return playerController == null || playerController.FacingRight;
        }
        /// <summary>
/// exist Scene The view draws the attack box range. When adjusting the attack distance, width, height and offset, you can directly see whether the frame covers the enemy.
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
