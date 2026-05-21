using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Represents a simple tutorial enemy that can hurt the player or be defeated by an attack.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Enemy_Block object in Level2_LootTutorial.
    /// The enemy can stay still or patrol a short distance. Touching it kills the player, while PlayerAttack.Die defeats it.
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

        private Vector3 startPosition;
        private bool defeated;

        /// <summary>
        /// Unity event method called when the enemy is created.
        /// </summary>
        /// <remarks>
        /// Stores the start position used by optional patrol movement.
        /// </remarks>
        private void Awake()
        {
            startPosition = transform.position;
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Moves the enemy in a small patrol pattern when patrol is enabled.
        /// </remarks>
        private void Update()
        {
            if (!patrol || defeated)
            {
                return;
            }

            float offset = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
            transform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
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

            EchoEscapeGameManager.Instance?.KillPlayer(deathReason);
        }

        /// <summary>
        /// Defeats the enemy and removes it from active gameplay.
        /// </summary>
        /// <remarks>
        /// Called by PlayerAttack when the enemy is inside the attack box.
        /// </remarks>
        public void Die()
        {
            if (defeated)
            {
                return;
            }

            defeated = true;
            if (debugLogs)
            {
                Debug.Log("Enemy defeated.");
            }

            gameObject.SetActive(false);
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
    }
}
