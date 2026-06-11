using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Marks a possible location where the game manager can spawn a random chest.
    /// </summary>
    /// <remarks>
    /// Attach this empty marker script to scene objects placed in optional reward routes.
    /// EchoEscapeGameManager searches for these markers at scene start and chooses some of them for chest placement.
    /// </remarks>
    public class ChestSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private bool hideMarkerVisualsOnPlay = true;

        /// <summary>
        /// Description:
        /// Called when the marker object is created.
        /// It hides marker visuals in Play Mode so only the real chest is seen.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Awake()
        {
            if (hideMarkerVisualsOnPlay && Application.isPlaying)
            {
                HideMarkerVisuals();
            }
        }

        /// <summary>
        /// Hides legacy visible marker geometry so it does not cover the spawned chest.
        /// </summary>
        public void HideMarkerVisuals()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer markerRenderer in renderers)
            {
                markerRenderer.enabled = false;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D markerCollider in colliders)
            {
                markerCollider.enabled = false;
            }
        }
    }
}
