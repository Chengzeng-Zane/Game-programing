using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Enemy Target Identification Tool. Its core mission is to find the true Player, and explicitly ignore Echo。
/// Gameplay logic: Echo It is a shadow recorded by the player, which can suppress the machine, but the enemy should not treat it as a real player to attack or trigger death. This script passes tag、EchoReplayController and PlayerController2D Do the filtering.
/// Collaborates with: EnemyMovement Use it to track players; EnemyAttack Use it to determine whether there is a player in the trigger and attack box.
    /// </summary>
    public class EnemyTargeting : MonoBehaviour
    {
        private Transform playerTarget;
        /// <summary>
/// Returns the current real player's Transform. The enemy movement component uses it to determine the pursuit direction and distance.
        /// </summary>
/// <returns>Return found Transform; may return if not found null。</returns>
        public Transform GetPlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
// Caching players Transform, to avoid every frame FindObjectOfType; Directly reuse while the player still exists.
                return playerTarget;
            }

// The cache may become invalid after the player dies and is reloaded or the scene is switched. In this case, search again.
            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            playerTarget = player != null ? player.transform : null;
            return playerTarget;
        }
        /// <summary>
/// from a Collider Find real players. Echo or non-player objects will return null。
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <returns>Returns the real player controller; returns if the incoming object is not a player null。</returns>
        public PlayerController2D GetPlayer(Collider2D other)
        {
            if (other == null ||
                HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null)
            {
// Echo You can press the button, but you cannot be attacked by the enemy as a player, otherwise the puzzle will trigger death by mistake.
                return null;
            }

// player Collider May be on the root object or a child object, so both the current object and the parent are checked.
            return other.GetComponent<PlayerController2D>() ?? other.GetComponentInParent<PlayerController2D>();
        }
        /// <summary>
/// security check Collider Or whether the root object is specified Tag。Tag The game will not report an error when missing.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <param name="tagName">tagName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private static bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
// If the project does not have a corresponding configuration Tag，CompareTag An exception will be thrown; return here false Keep the game running.
                return false;
            }
        }
    }
}
