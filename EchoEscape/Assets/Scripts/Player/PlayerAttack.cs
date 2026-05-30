using UnityEngine;

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
        private Vector2 attackBoxSize = new Vector2(1.1f, 0.9f); // Width and height of the attack hit area.

        [SerializeField]
        private Vector2 attackOffset = new Vector2(0.75f, -0.05f); // Offset from the player center toward the facing direction.

        [SerializeField]
        private float attackCooldown = 0.35f; // Minimum time between attacks.

        [SerializeField]
        private bool debugLogs = true; // Prints simple combat messages for playtesting.

        private PlayerController2D playerController;
        private PlayerAnimationController animationController;
        private float nextAttackTime;

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
            if (Input.GetKeyDown(attackKey) && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
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
            animationController?.PlayAttack();

            Vector2 center = AttackCenter();
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f);
            bool defeatedEnemy = false;

            foreach (Collider2D hit in hits)
            {
                SimpleEnemy enemy = hit.GetComponent<SimpleEnemy>();
                if (enemy == null)
                {
                    enemy = hit.GetComponentInParent<SimpleEnemy>();
                }

                if (enemy == null)
                {
                    continue;
                }

                enemy.Die();
                defeatedEnemy = true;
            }

            if (debugLogs && !defeatedEnemy)
            {
                Debug.Log("Player attacked.");
            }
        }

        /// <summary>
        /// Calculates the world-space center of the attack hit area.
        /// </summary>
        /// <returns>The center point used by Physics2D.OverlapBoxAll.</returns>
        private Vector2 AttackCenter()
        {
            bool facingRight = playerController == null || playerController.FacingRight;
            float direction = facingRight ? 1f : -1f;
            Vector2 offset = new Vector2(attackOffset.x * direction, attackOffset.y);
            return (Vector2)transform.position + offset;
        }

        /// <summary>
        /// Unity editor event used to draw the attack box while the player object is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.45f);
            Gizmos.DrawWireCube(AttackCenter(), attackBoxSize);
        }
    }
}
