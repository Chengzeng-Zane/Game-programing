using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Finds real player targets for enemies while ignoring Echo replay objects.
    /// </summary>
    public class EnemyTargeting : MonoBehaviour
    {
        private Transform playerTarget;

        /// <summary>
        /// Finds and caches the player's transform.
        /// </summary>
        /// <returns>The active player transform, or null.</returns>
        public Transform GetPlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                return playerTarget;
            }

            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            playerTarget = player != null ? player.transform : null;
            return playerTarget;
        }

        /// <summary>
        /// Gets the real player controller from a collider.
        /// </summary>
        /// <param name="other">Collider to inspect.</param>
        /// <returns>The player controller, or null.</returns>
        public PlayerController2D GetPlayer(Collider2D other)
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

        private static bool HasTag(Collider2D other, string tagName)
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
