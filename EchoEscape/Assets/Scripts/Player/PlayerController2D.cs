using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Handles player movement, jumping, and gameplay input for the 2D prototype.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Player object with Rigidbody2D, CapsuleCollider2D, and ActionRecorder.
    /// It reads movement input, calls ActionRecorder for Q/E Echo controls, and interacts with nearby chests.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        /// <summary>
        /// Horizontal movement speed applied to the player's Rigidbody2D.
        /// </summary>
        public float moveSpeed = 6f;

        /// <summary>
        /// Velocity applied opposite the current gravity direction when the player jumps.
        /// </summary>
        public float jumpForce = 10f;

        /// <summary>
        /// Radius used when searching for nearby unopened chests.
        /// </summary>
        public float interactRadius = 1.1f;

        /// <summary>
        /// True when the latest horizontal input indicates the player is facing right.
        /// </summary>
        public bool FacingRight { get; private set; } = true;

        private Rigidbody2D body;
        private ActionRecorder recorder;
        private float moveInput;
        private float groundedUntil;

        /// <summary>
        /// Unity event method called when the player object is created.
        /// </summary>
        /// <remarks>
        /// Caches Rigidbody2D and ActionRecorder references before input is processed.
        /// </remarks>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            recorder = GetComponent<ActionRecorder>();
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Reads keyboard input for movement, jumping, recording, replaying Echo, and opening chests.
        /// </remarks>
        private void Update()
        {
            moveInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
            {
                Vector2 jumpVelocity = -GravityDirection * jumpForce;
                body.velocity = new Vector2(body.velocity.x, jumpVelocity.y);
                EchoEscapeGameManager.Instance?.AudioService?.PlayJump();
            }

            if (Input.GetKeyDown(KeyCode.Q) && recorder != null)
            {
                recorder.ToggleRecording();
            }

            if (Input.GetKeyDown(KeyCode.E) && recorder != null)
            {
                recorder.PlayEcho();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                TryOpenChest();
            }
        }

        /// <summary>
        /// Unity physics event method called at a fixed timestep.
        /// </summary>
        /// <remarks>
        /// Applies horizontal velocity and updates the facing direction used by RecordingFrame.
        /// </remarks>
        private void FixedUpdate()
        {
            body.velocity = new Vector2(moveInput * moveSpeed, body.velocity.y);

            if (moveInput > 0.05f)
            {
                FacingRight = true;
            }
            else if (moveInput < -0.05f)
            {
                FacingRight = false;
            }
        }

        /// <summary>
        /// Unity physics event called while the player is colliding with another 2D collider.
        /// </summary>
        /// <param name="collision">Collision information used to decide whether the player is grounded.</param>
        private void OnCollisionStay2D(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector2.Dot(collision.GetContact(i).normal, -GravityDirection) > 0.45f)
                {
                    groundedUntil = Time.time + 0.12f;
                    return;
                }
            }
        }

        /// <summary>
        /// Moves the player back to a spawn position and clears current velocity.
        /// </summary>
        /// <param name="position">The world position where the player should respawn.</param>
        public void Respawn(Vector3 position)
        {
            transform.position = position;
            body.velocity = Vector2.zero;
            GravityFlipController gravityFlip = GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
                gravityFlip.ResetGravityState();
            }
            else
            {
                body.gravityScale = Mathf.Abs(body.gravityScale);
                transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Direction gravity currently pulls the player.
        /// </summary>
        public Vector2 GravityDirection => body != null && body.gravityScale < 0f ? Vector2.up : Vector2.down;

        /// <summary>
        /// Checks whether the player was recently standing on a floor-like collision normal.
        /// </summary>
        /// <returns>True if the player can currently jump; otherwise false.</returns>
        public bool IsGrounded()
        {
            return Time.time <= groundedUntil;
        }

        /// <summary>
        /// Searches nearby colliders for the closest unopened chest and opens it.
        /// </summary>
        /// <remarks>
        /// Called when the player presses F.
        /// </remarks>
        private void TryOpenChest()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Chest nearestChest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                Chest chest = hit.GetComponent<Chest>();
                if (chest == null || chest.IsOpened)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, chest.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestChest = chest;
                }
            }

            if (nearestChest != null)
            {
                nearestChest.Open();
            }
            else if (EchoEscapeGameManager.Instance != null)
            {
                EchoEscapeGameManager.Instance.UpdateStatus("No unopened chest close enough. Stand beside it and press F.");
            }
        }
    }
}
