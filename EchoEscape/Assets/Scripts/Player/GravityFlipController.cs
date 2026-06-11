using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Lets the player flip gravity when a standable platform exists in the requested direction.
    /// </summary>
    /// <remarks>
    /// Up Arrow flips toward a platform above.
    /// Down Arrow restores normal gravity when ground exists below.
    /// </remarks>
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

        /// <summary>
        /// True while the player's gravity scale is negative.
        /// </summary>
        public bool IsFlipped => isFlipped;

        /// <summary>
        /// Clears gravity flip state when another system resets the player.
        /// </summary>
        public void ResetGravityState()
        {
            SetGravity(false);
        }

        /// <summary>
        /// Description:
        /// Called when the component is created.
        /// It caches player physics components and starts with normal gravity.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<Collider2D>();
            playerController = GetComponent<PlayerController2D>();

            if (Mathf.Abs(body.gravityScale) > 0.01f)
            {
                gravityScale = Mathf.Abs(body.gravityScale);
            }

            SetGravity(false);
        }

        /// <summary>
        /// Description:
        /// Called every frame by Unity.
        /// It reads Up Arrow and Down Arrow to request gravity flip changes.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                TrySetGravity(flipped: true, Vector2.up);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                TrySetGravity(flipped: false, Vector2.down);
            }
        }

        /// <summary>
        /// Description:
        /// Tries to flip or restore gravity only if the player is grounded and a platform exists in that direction.
        /// Inputs:
        /// flipped - true to flip upward, false to restore normal gravity
        /// checkDirection - direction used to search for a standable platform
        /// Returns:
        /// void (no return)
        /// </summary>
        private void TrySetGravity(bool flipped, Vector2 checkDirection)
        {
            if (isFlipped == flipped || !playerController.IsGrounded())
            {
                return;
            }

            if (!TryFindStandablePlatform(checkDirection, out RaycastHit2D platformHit))
            {
                return;
            }

            SetGravity(flipped);
            SnapToPlatform(platformHit, checkDirection);
            LogDebug(flipped ? "Gravity flipped." : "Gravity restored.");
        }

        /// <summary>
        /// Description:
        /// Applies normal or flipped gravity to the Rigidbody2D.
        /// Inputs:
        /// flipped - true for upside-down gravity, false for normal gravity
        /// Returns:
        /// void (no return)
        /// </summary>
        private void SetGravity(bool flipped)
        {
            isFlipped = flipped;
            body.gravityScale = flipped ? -gravityScale : gravityScale;
            body.velocity = new Vector2(body.velocity.x, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, flipped ? 180f : 0f);
        }

        /// <summary>
        /// Description:
        /// Raycasts in one direction to find the nearest valid platform.
        /// Inputs:
        /// direction - up or down direction to check
        /// platformHit - output hit information for the platform
        /// Returns:
        /// bool - true if a standable platform was found
        /// </summary>
        private bool TryFindStandablePlatform(Vector2 direction, out RaycastHit2D platformHit)
        {
            platformHit = default;

            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, flipCheckDistance, groundLayer);
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || !IsStandablePlatform(hit.collider))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    platformHit = hit;
                }
            }

            return platformHit.collider != null;
        }

        /// <summary>
        /// Description:
        /// Checks whether a collider can be used as a gravity flip landing surface.
        /// Inputs:
        /// hitCollider - collider found by the raycast
        /// Returns:
        /// bool - true if the collider is a solid ground or platform object
        /// </summary>
        private bool IsStandablePlatform(Collider2D hitCollider)
        {
            if (hitCollider == null || hitCollider.isTrigger)
            {
                return false;
            }

            if (hitCollider.attachedRigidbody == body || hitCollider.transform.IsChildOf(transform))
            {
                return false;
            }

            string objectName = hitCollider.gameObject.name;
            string objectTag = hitCollider.gameObject.tag;
            return objectName.Contains("Ground")
                || objectName.Contains("Platform")
                || objectTag == "Ground"
                || objectTag == "Platform";
        }

        /// <summary>
        /// Description:
        /// Moves the player onto the target platform after gravity changes.
        /// Inputs:
        /// platformHit - raycast hit for the target platform
        /// direction - direction the player moved during the flip
        /// Returns:
        /// void (no return)
        /// </summary>
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
                float topOffset = bounds.max.y - transform.position.y;
                targetPosition.y = platformHit.point.y - topOffset - SnapSkin;
            }
            else
            {
                float bottomOffset = transform.position.y - bounds.min.y;
                targetPosition.y = platformHit.point.y + bottomOffset + SnapSkin;
            }

            body.position = targetPosition;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }

        /// <summary>
        /// Description:
        /// Writes optional gravity flip debug messages to the Console.
        /// Inputs:
        /// message - text to print
        /// Returns:
        /// void (no return)
        /// </summary>
        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
