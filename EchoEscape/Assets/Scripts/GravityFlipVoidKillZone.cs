using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Kills the real player only after they leave the safe area while gravity is flipped.
    /// </summary>
    /// <remarks>
    /// Use this for Gravity Flip void triggers. Normal gravity movement and Echo replay are ignored.
    /// </remarks>
    public class GravityFlipVoidKillZone : MonoBehaviour
    {
        [SerializeField]
        private string deathReason = "fell out during gravity flip";

        [SerializeField]
        private bool debugLogs;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryKillFlippedPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryKillFlippedPlayer(other);
        }

        private void TryKillFlippedPlayer(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null)
            {
                return;
            }

            PlayerController2D player = other.GetComponent<PlayerController2D>() ??
                other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
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
                return;
            }

            if (debugLogs)
            {
                Debug.Log("[GravityFlipDeath] calling death flow");
            }

            HazardZone.KillPlayerUsingExistingDeathFlow(this, deathReason, name, debugLogs);
        }
    }
}
