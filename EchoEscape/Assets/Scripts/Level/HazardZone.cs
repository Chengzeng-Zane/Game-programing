using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Detects when the player enters a dangerous trigger area.
    /// </summary>
    /// <remarks>
    /// Attach this script to hazard or death-zone objects with Collider2D set as Trigger.
    /// It notifies EchoEscapeGameManager so the player respawns and pending loot is lost.
    /// </remarks>
    public class HazardZone : MonoBehaviour
    {
        /// <summary>
        /// Message shown in the status text when the player dies in this hazard.
        /// </summary>
        public string deathReason = "hit a hazard";

        /// <summary>
        /// Unity physics event called when another 2D collider enters this hazard trigger.
        /// </summary>
        /// <param name="other">The collider that entered the hazard area.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                EchoEscapeGameManager.Instance?.KillPlayer(deathReason);
            }
        }
    }
}
