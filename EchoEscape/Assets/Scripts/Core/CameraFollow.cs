using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Smoothly follows a target transform with an offset.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Main Camera.
    /// Level builders set the target to the Player so the camera follows the tutorial route.
    /// </remarks>
    public class CameraFollow : MonoBehaviour
    {
        /// <summary>
        /// Transform the camera should follow, usually the Player.
        /// </summary>
        public Transform target;

        /// <summary>
        /// Offset from the target position used to frame the level.
        /// </summary>
        public Vector3 offset = new Vector3(0f, 1.25f, -10f);

        /// <summary>
        /// Interpolation speed used when moving toward the desired position.
        /// </summary>
        public float followSpeed = 6f;

        /// <summary>
        /// Unity event method called after normal Update methods.
        /// </summary>
        /// <remarks>
        /// Moves the camera after the player has already updated for the frame, reducing visual jitter.
        /// </remarks>
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }
    }
}
