using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Player Basic Control Script. It is responsible for left/right movement, jumping, facing direction, ground detection, respawn and chest opening interaction, and is the core of player control.
/// Gameplay logic: Update reads horizontal input, jump key, Q/E Echo Record/replay key and F chest-open key; FixedUpdate writes horizontal input Rigidbody2D. velocity；OnCollisionStay2D determines whether you are standing on the ground in the current gravity direction based on the collision normal.
/// Collaborates with: ActionRecorder records player position; GravityFlipController changes the direction of gravity; PlayerAnimationController reads landing and facing direction; CameraFollow follows the player; Chest through TryOpenChest to to be opened.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float jumpForce = 10f;
        public float interactRadius = 0.75f;

        [SerializeField]
        private float chestFacingTolerance = 0.18f;

        [SerializeField]
        private float chestVerticalTolerance = 0.75f;
        public bool FacingRight { get; private set; } = true;

        private Rigidbody2D body;
        private ActionRecorder recorder;
        private float moveInput;
        private float groundedUntil;
        /// <summary>
/// Initialize the player movement component. Cache here Rigidbody2D, let the follow-up FixedUpdate You can change the speed directly; cache at the same time ActionRecorder, let Q/E The record playback input can call the recording system.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            recorder = GetComponent<ActionRecorder>();
        }
        /// <summary>
/// reads the player's operation input for this frame. Horizontal Used to move left and right, Space Used to jump according to the current gravity direction, Q/E Separate control Echo record and playback, F Used to open nearby treasure chests. There is no direct horizontal physical movement here to avoid conflicts with FixedUpdate The physical synchronization is not synchronized.
        /// </summary>
        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
// Plot introduction, tutorial pop-up or death UI The time will be paused; clear the input here to prevent the player from continuing to slide in the direction of the previous frame when the game is resumed.
                moveInput = 0f;
                return;
            }

// Horizontal from Unity input shaft, A/D You can control this value with the left and right arrow keys.
            moveInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
            {
// The jumping direction is opposite to the current gravity, so jumping upward under normal gravity will jump toward the platform below under anti-gravity.
                Vector2 jumpVelocity = -GravityDirection * jumpForce;
                body.velocity = new Vector2(body.velocity.x, jumpVelocity.y);
                EchoEscapeGameManager.Instance?.AudioService?.PlayJump();
            }

            if (Input.GetKeyDown(KeyCode.Q) && recorder != null)
            {
// Recording logic is handed over to ActionRecorder; The player control script is only responsible for forwarding the keystrokes.
                recorder.ToggleRecording();
            }

            if (Input.GetKeyDown(KeyCode.E) && recorder != null)
            {
// Echo Playback will reproduce the route just recorded, which can be used to press buttons or cooperate with mechanisms.
                recorder.PlayEcho();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
// F Only attempts to open nearby treasure chests facing the correct direction, and will not modify them directly. loot data.
                TryOpenChest();
            }
        }
        /// <summary>
/// Bundle Update The horizontal input recorded in is applied to Rigidbody2D. velocity. In this way, the player's left/right movement is driven by the physics system, and is updated according to the positive and negative input. FacingRight, for use in animations and attack directions.
        /// </summary>
        private void FixedUpdate()
        {
// The horizontal speed is FixedUpdate writes Rigidbody, guaranteed to move and Unity The physical step size is consistent.
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
/// Continuously checks if players are touching a standable surface. It will compare the collision normal with the current opposite direction of gravity, so both normal gravity and anti-gravity can correctly determine which side is "underfoot".
        /// </summary>
/// <param name="collision">Unity The incoming collision information, which includes contact points and normals, is used to determine whether the player is standing on the ground or a platform. </param>
        private void OnCollisionStay2D(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
// The closer the collision normal is to the "reverse direction of gravity", the more likely the player is to step on the ground corresponding to the current direction of gravity.
                if (Vector2.Dot(collision.GetContact(i).normal, -GravityDirection) > 0.45f)
                {
// Reserved for landing state 0. 12 Seconds buffering to reduce edge jitter on jumps, animations, and gravity flip detection.
                    groundedUntil = Time.time + 0.12f;
                    return;
                }
            }
        }
        /// <summary>
/// Return the player to the designated spawn point and clear the speed. Will be reset when reborn GravityFlipController, ensuring that players will not continue the death loop in an anti-gravity or spinning state.
        /// </summary>
/// <param name="position">Target world coordinates, often used for respawn, spawning objects or recording Echo frame. </param>
        public void Respawn(Vector3 position)
        {
            transform.position = position;
            body.velocity = Vector2.zero;
            GravityFlipController gravityFlip = GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
// Respawn after death must return to normal gravity, otherwise the player may spawn upside down and immediately fall into the death zone again.
                gravityFlip.ResetGravityState();
            }
            else
            {
// Not hung up GravityFlipController The test scenario should also be restored to at least normal gravityScale and rotation.
                body.gravityScale = Mathf.Abs(body.gravityScale);
                transform.rotation = Quaternion.identity;
            }
        }
        public Vector2 GravityDirection => body != null && body.gravityScale < 0f ? Vector2.up : Vector2.down;
        /// <summary>
/// Returns whether the player has recently touched the ground. use groundedUntil Make a very short buffer so that jumping and animation judgments will not jitter due to physical frame intervals.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        public bool IsGrounded()
        {
            return Time.time <= groundedUntil;
        }
        /// <summary>
/// player press F when called. It searches all treasure chests within the interaction radius, filters out those that cannot be interacted with, and then opens the closest one.
        /// </summary>
        private void TryOpenChest()
        {
// Use circular range search, compatible with treasure chests Collider Or the visual size is different.
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Chest nearestChest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                Chest chest = hit.GetComponent<Chest>();
                if (chest == null || chest.IsOpened || chest.IsOpening || !CanInteractWithChest(chest))
                {
// Skips non-treasure chests, treasure chests that have been opened, treasure chests with unboxing animations playing, and treasure chests that the player is not facing.
                    continue;
                }

                float distance = Vector2.Distance(transform.position, chest.transform.position);
                if (distance < nearestDistance)
                {
// If there are multiple treasure chests in range, only open the nearest one to avoid receiving multiple rewards with one button press.
                    nearestDistance = distance;
                    nearestChest = chest;
                }
            }

            if (nearestChest != null)
            {
                nearestChest.Open();
            }
        }
        /// <summary>
/// determines whether the player can actually open a certain treasure chest. It checks whether the treasure chest exists, whether it has been opened, whether the vertical distance is reasonable, and whether the player is facing the direction of the treasure chest.
        /// </summary>
/// <param name="chest">chest Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool CanInteractWithChest(Chest chest)
        {
            Vector2 toChest = chest.transform.position - transform.position;
            if (Mathf.Abs(toChest.y) > chestVerticalTolerance)
            {
// Unboxing is not allowed when the vertical distance is too large to prevent players from accidentally triggering across the upper and lower platforms.
                return false;
            }

            if (Mathf.Abs(toChest.x) <= chestFacingTolerance)
            {
// When the treasure chest is almost in the middle of the player, the facing direction is not mandatory to avoid feeling too strict when getting close to the treasure chest.
                return true;
            }

// If the treasure chest is on the left side of the player, it must face left, and if the treasure chest is on the right, it must face right.
            return FacingRight ? toChest.x > 0f : toChest.x < 0f;
        }
    }
}
