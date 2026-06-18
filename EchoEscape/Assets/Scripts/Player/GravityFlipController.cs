using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Player gravity flips the controller. It allows players to flip from the ground to the platform above, forming Echo Escape The core space puzzle-solving mechanism.
/// Gameplay logic: When the player presses the up direction key, the script first checks upward to see if there is a platform on which to stand; only after finding the platform Rigidbody2D. gravityScale becomes a negative number and rotates the character upside down. Returns normal gravity when pressing the d-pad. In order to avoid getting stuck in the wall, players will be attracted to the surface of the platform after flipping.
/// Collaborates with: PlayerController2D Continue to be responsible for horizontal movement and jumping; PlayerAnimationController read gravityScale Display animation parameters; GravityFlipVoidKillZone read IsFlipped Dealing with anti-gravity falling to death.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerController2D))]
    public class GravityFlipController : MonoBehaviour
    {
        [SerializeField] private float gravityScale = 2.4f;
        [SerializeField] private float flipCheckDistance = 3.25f;
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private bool debugLogs = true;

        private const float SnapSkin = 0.03f;

        private Rigidbody2D body;
        private Collider2D playerCollider;
        private PlayerController2D playerController;
        private bool isFlipped;
        public bool IsFlipped => isFlipped;
        /// <summary>
/// Return to normal gravity. Called during death, respawn or initialization to prevent players from retaining upside down rotation and negative gravityScale。
        /// </summary>
        public void ResetGravityState()
        {
            SetGravity(false);
        }
        /// <summary>
/// cache Rigidbody2D, player Collider and PlayerController2D, and read the current gravityScale as base gravity strength. Forces a return to normal gravity during initialization.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<Collider2D>();
            playerController = GetComponent<PlayerController2D>();

            if (Mathf.Abs(body.gravityScale) > 0.01f)
            {
// In usage scenarios Rigidbody Original gravity strength to avoid script default value overwriting Inspector Well-adjusted feel.
                gravityScale = Mathf.Abs(body.gravityScale);
            }

// Force normal gravity at the start to prevent the remaining upside-down state in the editor from affecting birth.
            SetGravity(false);
        }
        /// <summary>
/// Monitor the up and down arrow keys. Press the up button to try to flip to the upper platform, press the button to try to return to the lower platform; can it really be flipped by TrySetGravity Decide.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
// The up arrow key only means "try to flip to the upper platform". Whether you can actually flip depends on whether there is a platform above you to stand on.
                TrySetGravity(flipped: true, Vector2.up);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
// The down arrow key returns to normal gravity, which also requires a platform below to prevent players from switching at will in the air.
                TrySetGravity(flipped: false, Vector2.down);
            }
        }
        /// <summary>
/// Try switching the gravity direction. It must first confirm that the player is currently on the ground and that there is a platform on which he can stand in the target direction; otherwise, it will not flip to prevent the player from flipping randomly in the air or turning to a place without a platform.
        /// </summary>
/// <param name="flipped">Whether to switch to anti-gravity state. true means hanging upside down, false Indicates a return to normal gravity. </param>
/// <param name="checkDirection">Detect the direction of the platform. Up means to find the ceiling platform, downward means to find the ground platform. </param>
        private void TrySetGravity(bool flipped, Vector2 checkDirection)
        {
            if (isFlipped == flipped || !playerController.IsGrounded())
            {
// Flips are not allowed when the player is already in the target state, or when the player is not standing still, to avoid continuous flips in the air from destroying the level design.
                return;
            }

            if (!TryFindStandablePlatform(checkDirection, out RaycastHit2D platformHit))
            {
// No target direction Ground/Platform It does not flip when it happens, preventing players from flipping into the void.
                return;
            }

            SetGravity(flipped);
// Immediately stick to the surface of the target platform after flipping, otherwise Collider May get stuck in the platform or hang in the air.
            SnapToPlatform(platformHit, checkDirection);
            LogDebug(flipped ? "Gravity flipped." : "Gravity restored.");
        }
        /// <summary>
/// Really modify the gravity state. it updates internal isFlipped, changes Rigidbody2D. gravityScale, clears the vertical speed, and rotates the player to normal or upside down facing direction.
        /// </summary>
/// <param name="flipped">Whether to switch to anti-gravity state. true means hanging upside down, false Indicates a return to normal gravity. </param>
        private void SetGravity(bool flipped)
        {
            isFlipped = flipped;
            body.gravityScale = flipped ? -gravityScale : gravityScale;
// The vertical speed is cleared instantly when switching to prevent players from crossing the new platform with their original falling speed.
            body.velocity = new Vector2(body.velocity.x, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, flipped ? 180f : 0f);
        }
        /// <summary>
/// Launch in the target direction Raycast, to find the nearest platform on which to stand. This function is responsible for converting the question "Is there a platform above" into a physical detection result.
        /// </summary>
/// <param name="direction">Direction vector, used for ray detection, movement or facing direction judgment. </param>
/// <param name="platformHit">Output parameters, return the detected standable platform information. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool TryFindStandablePlatform(Vector2 direction, out RaycastHit2D platformHit)
        {
            platformHit = default;

// RaycastAll May hit multiple Collider, so you need to choose the nearest platform you can stand on.
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, flipCheckDistance, groundLayer);
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || !IsStandablePlatform(hit.collider))
                {
// Triggers, the player themselves, danger zones, and non-platform objects cannot be used as flip points.
                    continue;
                }

                if (hit.distance < bestDistance)
                {
// Choose the closest platform so that the flip target matches the platform the player visually sees.
                    bestDistance = hit.distance;
                    platformHit = hit;
                }
            }

            return platformHit.collider != null;
        }
        /// <summary>
/// filter Raycast Hit object. Trigger, player's own Collider, danger zones or non-platform objects cannot be used as gravity flip landing points.
        /// </summary>
/// <param name="hitCollider">Ray or collision detection hits Collider, used to judge whether it can be used as a platform. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsStandablePlatform(Collider2D hitCollider)
        {
            if (hitCollider == null || hitCollider.isTrigger)
            {
// Trigger Usually a death zone, portal or prompt area, it cannot be used as the ground.
                return false;
            }

            if (hitCollider.attachedRigidbody == body || hitCollider.transform.IsChildOf(transform))
            {
// Can't put the player's own Collider As a platform.
                return false;
            }

// Here use the name and Tag Double insurance, because the platform objects in the level are not named exactly the same.
            string objectName = hitCollider.gameObject.name;
            string objectTag = hitCollider.gameObject.tag;
            return objectName.Contains("Ground")
                || objectName.Contains("Platform")
                || objectTag == "Ground"
                || objectTag == "Platform";
        }
        /// <summary>
/// After the flip is successful, stick the player to the surface of the platform. In this way, the character will not be Collider Size and ray distance errors get stuck in the platform or hang in the air.
        /// </summary>
/// <param name="platformHit">Output parameters, return the detected standable platform information. </param>
/// <param name="direction">Direction vector, used for ray detection, movement or facing direction judgment. </param>
        private void SnapToPlatform(RaycastHit2D platformHit, Vector2 direction)
        {
            if (playerCollider == null)
            {
                return;
            }

            Physics2D.SyncTransforms();
            Bounds bounds = playerCollider.bounds;
            Vector2 targetPosition = body.position;

            if (direction.y > 0f)
            {
// When flipping upward, the player's head should be close to the lower surface of the platform, so use Collider Top offset calculation target y。
                float topOffset = bounds.max.y - transform.position.y;
                targetPosition.y = platformHit.point.y - topOffset - SnapSkin;
            }
            else
            {
// When returning to normal gravity, the player's soles need to stand on the upper surface of the platform, so use Collider Bottom offset calculation target y。
                float bottomOffset = transform.position.y - bounds.min.y;
                targetPosition.y = platformHit.point.y + bottomOffset + SnapSkin;
            }

            body.position = targetPosition;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }
        /// <summary>
/// exist debugLogs When opened, logs related to gravity flips are output to facilitate testing why a certain flip succeeds or fails.
        /// </summary>
/// <param name="message">to be displayed to HUD Or the text written in the log. </param>
        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
