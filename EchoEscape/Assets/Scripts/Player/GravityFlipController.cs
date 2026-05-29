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

        private void SetGravity(bool flipped)
        {
            isFlipped = flipped;
            body.gravityScale = flipped ? -gravityScale : gravityScale;
            body.velocity = new Vector2(body.velocity.x, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, flipped ? 180f : 0f);
        }

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

        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
