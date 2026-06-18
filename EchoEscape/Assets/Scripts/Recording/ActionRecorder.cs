using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Echo recording system. player press Q To record your own movements, press E generate Echo, let Echo Repeat the course of action just now.
/// Gameplay logic: During recording, each FixedUpdate save RecordingFrame, including position, time, facing direction, and whether it is anti-gravity; create a EchoReplay GameObject, give it Rigidbody2D、Trigger Collider、EchoAnimationController and EchoReplayController。Echo When playback ends, it will stop at the last position, which is often used to hold down the pressure plate.
/// Collaboration: reads PlayerController2D and GravityFlipController; generate EchoReplayController；Echo can be triggered PressurePlate；PrototypeAudio Playback recorded feedback.
    /// </summary>
    public class ActionRecorder : MonoBehaviour
    {
        public float maxRecordSeconds = 5f;
        public Color echoColor = new Color(0.3f, 0.9f, 1f, 0.55f);
        public bool IsRecording { get; private set; }
        public bool HasRecording => frames.Count > 1;
        public float MaxRecordSeconds => Mathf.Max(0.1f, maxRecordSeconds);
        public float RecordingElapsedSeconds => IsRecording ? Mathf.Min(recordTimer, MaxRecordSeconds) : 0f;
        public float RecordingProgress => IsRecording ? Mathf.Clamp01(recordTimer / MaxRecordSeconds) : 0f;
        public EchoReplayController ActiveEcho => activeEcho;

        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private const int DefaultEchoSortingOrder = 8;
        private static readonly Vector3 DefaultEchoVisualOffset = new Vector3(0f, -0.31f, -0.03f);
        private static readonly Vector3 DefaultEchoVisualScale = new Vector3(0.65f, 0.65f, 1f);
        private const float EchoColliderFootPadding = 0.35f;
        private float recordTimer;
        private EchoReplayController activeEcho;
        private PlayerController2D player;
        private GravityFlipController gravityFlip;
        private Collider2D playerCollider;

        /// <summary>
/// Inspector Automatically called when parameters change. The maximum recording time here is limited to not less than 0. 1 seconds, avoid dividing by 0 Or the recording system stops abnormally immediately.
        /// </summary>
        private void OnValidate()
        {
            maxRecordSeconds = Mathf.Max(0.1f, maxRecordSeconds);
        }

        /// <summary>
/// Caching player controllers and gravity flip controllers. Recording frames requires knowing the player's facing direction and whether it is anti-gravity, so prepare a reference here first.
        /// </summary>
        private void Awake()
        {
            player = GetComponent<PlayerController2D>();
            gravityFlip = GetComponent<GravityFlipController>();
            playerCollider = GetComponent<Collider2D>();
        }
        /// <summary>
/// While recording is in progress, each physical frame saves the player's current position, recording time, facing direction, and Gravity Flip state. use FixedUpdate is to let Echo Playback and physical movement rhythm are consistent.
        /// </summary>
        private void FixedUpdate()
        {
            if (!IsRecording)
            {
                return;
            }

// Echo Not only the position is recorded, but also the facing direction and gravity state; otherwise the visual and upside-down states will not match during playback.
            bool facingRight = player == null || player.FacingRight;
            bool isGravityFlipped = gravityFlip != null && gravityFlip.IsFlipped;
            frames.Add(new RecordingFrame(transform.position, recordTimer, facingRight, isGravityFlipped));
            recordTimer += Time.fixedDeltaTime;

            if (recordTimer >= MaxRecordSeconds)
            {
// Automatically stops when the maximum recording duration is reached, preventing the list from growing indefinitely and making the puzzle time window controllable.
                StopRecording();
            }
        }
        /// <summary>
/// Q Recording switch called by key. If recording is currently in progress, stop and retain frames; if not recording, clear old data and start a new recording.
        /// </summary>
        public void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }
        /// <summary>
/// E key called Echo Replay entry. it will destroy the old Echo, create new EchoReplay object, add Rigidbody2D、Trigger Collider、EchoAnimationController and EchoReplayController, and then give it the recorded frame for playback.
        /// </summary>
        public void PlayEcho()
        {
            if (!HasRecording)
            {
// At least two frames are required to form a visible route; when there is no recording, the player is only prompted and no empty space is generated. Echo。
                EchoEscapeGameManager.Instance?.UpdateStatus("Record a movement first with Q, then press E to replay it.");
                Debug.Log("No recording available.");
                return;
            }

// Destroy the old one before each playback Echo, so that there is only one controllable playback body in the scene to avoid multiple Echo Press the machine at the same time.
            DestroyActiveEcho();

            GameObject echoObject = new GameObject("EchoReplay");
            echoObject.transform.position = frames[0].position;
            TrySetEchoTag(echoObject);

            Rigidbody2D body = echoObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

// Echo Use separate visual sub-objects so that the logical collider and character display can be resized independently/Location.
            GameObject visualObject = new GameObject("EchoVisual");
            visualObject.transform.SetParent(echoObject.transform, false);
            Transform playerVisual = ResolvePlayerVisualTransform();
            Vector3 visualOffset = ResolveEchoVisualOffset(playerVisual);
            Vector3 visualScale = ResolveEchoVisualScale(playerVisual);
            visualObject.transform.localPosition = visualOffset;
            visualObject.transform.localScale = visualScale;

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.color = echoColor;
            renderer.sortingOrder = ResolveEchoSortingOrder();

            Animator animator = visualObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("RubyPlayer");
            EchoAnimationController echoAnimation = visualObject.AddComponent<EchoAnimationController>();
            echoAnimation.ConfigureVisualTransform(visualOffset, visualScale);

            BoxCollider2D collider = echoObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            ConfigureEchoCollider(collider, out Vector2 normalColliderOffset, out Vector2 flippedColliderOffset);

// Real path playback is handed over to EchoReplayController, it will press RecordingFrame Moving frame by frame.
            activeEcho = echoObject.AddComponent<EchoReplayController>();
            activeEcho.ConfigureCollider(collider, normalColliderOffset, flippedColliderOffset);
            activeEcho.Load(frames);

            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Echo replay started. Use it to hold switches or time hazards.");
            Debug.Log("Echo replaying.");
        }
        /// <summary>
/// Destroy the existing Echo. This way every time the player plays a new Echo, only one playback body is retained in the scene to avoid multiple Echo Pressing down on the machine at the same time leads to confusion in the logic of the level.
        /// </summary>
        public void DestroyActiveEcho()
        {
            if (activeEcho == null)
            {
                return;
            }

            Destroy(activeEcho.gameObject);
            activeEcho = null;
        }

        /// <summary>
/// reads player animation SpriteRenderer display level, let Echo Displayed in front of buttons, mechanisms, etc. just like players.
        /// </summary>
/// <returns>Echo should be used SpriteRenderer sortingOrder。</returns>
        private int ResolveEchoSortingOrder()
        {
            PlayerAnimationController playerAnimation = GetComponentInChildren<PlayerAnimationController>(true);
            if (playerAnimation != null &&
                playerAnimation.TryGetComponent(out SpriteRenderer playerRenderer))
            {
                return playerRenderer.sortingOrder;
            }

            return DefaultEchoSortingOrder;
        }

        /// <summary>
/// Find what really shows in the player Ruby of PlayerVisual。Echo Its local position and scale are copied, preventing the copy from appearing to sink into the ground or hang in the air.
        /// </summary>
/// <returns>Player visual sub-object Transform; returns if not found null, the caller will use the default Echo visual parameters. </returns>
        private Transform ResolvePlayerVisualTransform()
        {
            PlayerAnimationController playerAnimation = GetComponentInChildren<PlayerAnimationController>(true);
            if (playerAnimation != null)
            {
                return playerAnimation.transform;
            }

            Transform namedVisual = transform.Find("PlayerVisual");
            if (namedVisual != null)
            {
                return namedVisual;
            }

            return null;
        }

        private Vector3 ResolveEchoVisualOffset(Transform playerVisual)
        {
            if (playerVisual == null)
            {
                return DefaultEchoVisualOffset;
            }

            Vector3 parentScale = transform.lossyScale;
            return new Vector3(
                playerVisual.localPosition.x * Mathf.Abs(parentScale.x),
                playerVisual.localPosition.y * Mathf.Abs(parentScale.y),
                playerVisual.localPosition.z);
        }

        private static Vector3 ResolveEchoVisualScale(Transform playerVisual)
        {
            if (playerVisual == null)
            {
                return DefaultEchoVisualScale;
            }

            Vector3 worldScale = playerVisual.lossyScale;
            return new Vector3(
                Mathf.Abs(worldScale.x),
                Mathf.Abs(worldScale.y),
                Mathf.Abs(worldScale.z));
        }

        /// <summary>
/// Configuration Echo trigger collision body. Echo use this trigger press PressurePlate, does not participate in real mobile collisions.
/// Here, the world size of the player's current body collision body is copied and symmetrically enlarged a little in the up and down direction.
/// so Echo It is easier to interact with the buttons while standing normally on the ground or hanging upside down against gravity to press the ceiling buttons. PressurePlate trigger overlapping.
        /// </summary>
/// <param name="collider">Echo body BoxCollider2D trigger。</param>
        private void ConfigureEchoCollider(
            BoxCollider2D collider,
            out Vector2 normalColliderOffset,
            out Vector2 flippedColliderOffset)
        {
            normalColliderOffset = Vector2.zero;
            flippedColliderOffset = Vector2.zero;

            if (collider == null)
            {
                return;
            }

            if (playerCollider == null)
            {
                playerCollider = GetComponent<Collider2D>();
            }

            if (playerCollider == null)
            {
                collider.size = new Vector2(0.75f, 1.75f);
                collider.offset = Vector2.zero;
                return;
            }

            Vector2 size = ResolveEchoColliderSize();
            normalColliderOffset = ResolveEchoColliderOffset(flipped: false);
            flippedColliderOffset = ResolveEchoColliderOffset(flipped: true);

            size.y += EchoColliderFootPadding * 2f;

            collider.size = size;
            collider.offset = gravityFlip != null && gravityFlip.IsFlipped
                ? flippedColliderOffset
                : normalColliderOffset;
        }

        private Vector2 ResolveEchoColliderSize()
        {
            Vector2 size = playerCollider.bounds.size;

            if (playerCollider is CapsuleCollider2D capsule)
            {
                Vector3 scale = transform.lossyScale;
                size = new Vector2(
                    capsule.size.x * Mathf.Abs(scale.x),
                    capsule.size.y * Mathf.Abs(scale.y));
            }
            else if (playerCollider is BoxCollider2D box)
            {
                Vector3 scale = transform.lossyScale;
                size = new Vector2(
                    box.size.x * Mathf.Abs(scale.x),
                    box.size.y * Mathf.Abs(scale.y));
            }

            return new Vector2(
                Mathf.Max(0.72f, size.x),
                Mathf.Max(1.05f, size.y));
        }

        private Vector2 ResolveEchoColliderOffset(bool flipped)
        {
            Vector2 offset = (Vector2)(playerCollider.bounds.center - transform.position);

            if (playerCollider is CapsuleCollider2D capsule)
            {
                offset = capsule.offset;
            }
            else if (playerCollider is BoxCollider2D box)
            {
                offset = box.offset;
            }

            Vector3 scale = transform.lossyScale;
            offset = new Vector2(
                offset.x * Mathf.Abs(scale.x),
                offset.y * Mathf.Abs(scale.y));

            return flipped ? -offset : offset;
        }

        /// <summary>
/// Start recording a new Echo. It clears old frames, resets timers, sets IsRecording, and play a tone/Status text tells the player that recording has started.
        /// </summary>
        private void StartRecording()
        {
// The new recording will overwrite the old recording, ensuring that the player presses E What is playing is the most recently attempted route.
            frames.Clear();
            recordTimer = 0f;
            IsRecording = true;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Recording. Move, jump, or stand on a plate, then press Q to stop.");
            Debug.Log("Recording...");
        }
        /// <summary>
/// End recording. it stops continuing to writes RecordingFrame, and update UI status, after which the player can press E generate Echo Playback.
        /// </summary>
        private void StopRecording()
        {
            IsRecording = false;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus($"Recording saved: {frames.Count} frame(s). Press E to replay your echo.");
            Debug.Log("Recording stopped. Press E to replay Echo.");
        }
        /// <summary>
/// try to put Echo set to Echo Label. This way enemies, danger zones, and other logic can identify that it's not the player, avoiding Echo Triggers player death.
        /// </summary>
/// <param name="echoObject">echoObject Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private void TrySetEchoTag(GameObject echoObject)
        {
            try
            {
// Echo Tags are used for logical filtering of enemies, dead zones, etc. to avoid Echo Be treated like a real player.
                echoObject.tag = "Echo";
            }
            catch (UnityException)
            {
// If the item Tag Not in the list Echo, component detection can still work, so here it only warns and does not terminate the game.
                Debug.LogWarning("Echo tag is missing. The Echo can still press plates by component detection, but add an Echo tag in Project Settings if you want tag-based filtering.");
            }
        }

    }
}
