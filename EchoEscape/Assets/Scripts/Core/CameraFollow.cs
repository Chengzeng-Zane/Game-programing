using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Camera follows the script. It allows the main camera to follow the movement of the player, ensuring that the player is always visible in the horizontal version of the level.
/// Gameplay logic: The camera does not teleport directly to the player, but uses SmoothDamp Smoothly approach the target position so the screen won't be too shaky when moving and jumping.
/// Collaboration: hanging on Main Camera superior, target point to Player; Only changes the camera position and does not affect player speed, collision or input.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 1.25f, -10f);
        public float followSpeed = 6f;
        /// <summary>
/// Unity exist Update Called afterwards. This is often used for camera or visual synchronization to ensure that the final state of this frame is read.
        /// </summary>
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
