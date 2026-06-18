using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Anti-gravity dedicated death zone. Ordinary pits and rivers can be used directly HazardZone, but this cannot be done in anti-gravity areas, as players passing through these triggers with normal gravity should not die.
/// Gameplay logic: After the player flips to the upper platform, if he falls out of the playable range from the left, right or upper side of the platform, this trigger will check whether the player is really in the Gravity Flip state; only in the anti-gravity state can the death process be carried out.
/// Collaboration: reads GravityFlipController. IsFlipped; neglect EchoReplayController; Final reuse HazardZone/EchoEscapeGameManager Unified death process, so death animation, You Died UI、loot Both lost and reloaded levels remain consistent.
    /// </summary>
    public class GravityFlipVoidKillZone : MonoBehaviour
    {
        [SerializeField]
        private string deathReason = "fell out during gravity flip";

        [SerializeField]
        private bool debugLogs;
        /// <summary>
/// Check if the player should die the first time they enter the anti-gravity death trigger.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            TryKillFlippedPlayer(other);
        }
        /// <summary>
/// Continue checking when the player remains inside the anti-gravity death trigger to avoid missing it when moving at high speeds Enter situation.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerStay2D(Collider2D other)
        {
            TryKillFlippedPlayer(other);
        }
        /// <summary>
/// Confirm that the entering object is a real player or not Echo, and GravityFlipController. IsFlipped for true Finally, call the unified death process. Normal gravity entry will be ignored directly.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void TryKillFlippedPlayer(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null)
            {
// Echo You can through through these areas, but it is not the player's body and cannot trigger the player's death.
                return;
            }

            PlayerController2D player = other.GetComponent<PlayerController2D>() ??
                other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
// Not a player Collider, such as mechanisms, props, and enemies, are ignored.
                return;
            }

            if (debugLogs)
            {
                Debug.Log("[GravityFlipDeath] player entered zone");
            }

            GravityFlipController gravityFlip = player.GetComponent<GravityFlipController>();
            bool isFlipped = gravityFlip != null && gravityFlip.IsFlipped;
            if (debugLogs)
            {
                Debug.Log($"[GravityFlipDeath] isFlipped = {isFlipped}");
            }

            if (!isFlipped)
            {
// Passing through these under normal gravity Trigger Shouldn't die; these areas only compensate for falling out of bounds due to gravity.
                return;
            }

            if (debugLogs)
            {
                Debug.Log("[GravityFlipDeath] calling death flow");
            }

// Reuse here HazardZone Public death entrance to ensure death animation, You Died、loot Lost and reloaded levels are exactly the same.
            HazardZone.KillPlayerUsingExistingDeathFlow(this, deathReason, name, debugLogs);
        }
    }
}
