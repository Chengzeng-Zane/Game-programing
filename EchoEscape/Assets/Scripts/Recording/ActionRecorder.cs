using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Records the player's movement and creates an Echo object that can replay it.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Player object together with PlayerController2D.
    /// PlayerController2D calls ToggleRecording when Q is pressed and PlayEcho when E is pressed.
    /// The spawned Echo works with PressurePlate and Door so the player can solve replay puzzles.
    /// </remarks>
    public class ActionRecorder : MonoBehaviour
    {
        /// <summary>
        /// Maximum number of seconds the player can record before recording stops automatically.
        /// </summary>
        public float maxRecordSeconds = 5f;

        /// <summary>
        /// Color used by the ghostly Ruby Echo visual created during playback.
        /// </summary>
        public Color echoColor = new Color(0.3f, 0.9f, 1f, 0.55f);

        /// <summary>
        /// True while player positions are being captured.
        /// </summary>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// True when enough frames exist to replay an Echo.
        /// </summary>
        public bool HasRecording => frames.Count > 1;

        /// <summary>
        /// Normalized progress through the current recording duration.
        /// </summary>
        public float RecordingProgress => IsRecording ? Mathf.Clamp01(recordTimer / maxRecordSeconds) : 0f;

        /// <summary>
        /// The Echo replay object currently active in the scene, if one exists.
        /// </summary>
        public EchoReplayController ActiveEcho => activeEcho;

        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private float recordTimer;
        private EchoReplayController activeEcho;
        private PlayerController2D player;
        private GravityFlipController gravityFlip;

        /// <summary>
        /// Unity event method called when the component is created.
        /// </summary>
        /// <remarks>
        /// Caches the PlayerController2D reference so recorded frames can also store facing direction.
        /// </remarks>
        private void Awake()
        {
            player = GetComponent<PlayerController2D>();
            gravityFlip = GetComponent<GravityFlipController>();
        }

        /// <summary>
        /// Unity physics event method called at a fixed timestep.
        /// </summary>
        /// <remarks>
        /// While recording, this samples the player's position into RecordingFrame data.
        /// Recording stops automatically when maxRecordSeconds is reached.
        /// </remarks>
        private void FixedUpdate()
        {
            if (!IsRecording)
            {
                return;
            }

            bool facingRight = player == null || player.FacingRight;
            bool isGravityFlipped = gravityFlip != null && gravityFlip.IsFlipped;
            frames.Add(new RecordingFrame(transform.position, recordTimer, facingRight, isGravityFlipped));
            recordTimer += Time.fixedDeltaTime;

            if (recordTimer >= maxRecordSeconds)
            {
                StopRecording();
            }
        }

        /// <summary>
        /// Starts recording if idle, or stops recording if recording is already active.
        /// </summary>
        /// <remarks>
        /// Called by PlayerController2D when the player presses Q.
        /// </remarks>
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
        /// Spawns an EchoReplay object and gives it the saved movement frames to replay.
        /// </summary>
        /// <remarks>
        /// Called by PlayerController2D when the player presses E.
        /// If no recording exists, the method only reports that playback is unavailable.
        /// </remarks>
        public void PlayEcho()
        {
            if (!HasRecording)
            {
                EchoEscapeGameManager.Instance?.UpdateStatus("Record a movement first with Q, then press E to replay it.");
                Debug.Log("No recording available.");
                return;
            }

            DestroyActiveEcho();

            GameObject echoObject = new GameObject("EchoReplay");
            echoObject.transform.position = frames[0].position;
            TrySetEchoTag(echoObject);

            Rigidbody2D body = echoObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            GameObject visualObject = new GameObject("EchoVisual");
            visualObject.transform.SetParent(echoObject.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, -0.58f, -0.03f);
            visualObject.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.color = echoColor;
            renderer.sortingOrder = 5;

            Animator animator = visualObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("RubyPlayer");
            visualObject.AddComponent<EchoAnimationController>();

            BoxCollider2D collider = echoObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.75f, 1.5f);
            collider.offset = new Vector2(0f, -0.33f);

            activeEcho = echoObject.AddComponent<EchoReplayController>();
            activeEcho.Load(frames);

            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Echo replay started. Use it to hold switches or time hazards.");
            Debug.Log("Echo replaying.");
        }

        /// <summary>
        /// Removes the currently active Echo replay object from the scene.
        /// </summary>
        /// <remarks>
        /// Used before spawning a new Echo and when the player dies, preventing old Echoes from stacking up.
        /// </remarks>
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
        /// Clears old frames and begins capturing the player's movement.
        /// </summary>
        /// <remarks>
        /// This is the first half of the Q key toggle flow.
        /// </remarks>
        private void StartRecording()
        {
            frames.Clear();
            recordTimer = 0f;
            IsRecording = true;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Recording. Move, jump, or stand on a plate, then press Q to stop.");
            Debug.Log("Recording...");
        }

        /// <summary>
        /// Finishes the current recording and keeps the saved frames ready for playback.
        /// </summary>
        /// <remarks>
        /// This is called when Q is pressed during recording or when the maximum recording time is reached.
        /// </remarks>
        private void StopRecording()
        {
            IsRecording = false;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus($"Recording saved: {frames.Count} frame(s). Press E to replay your echo.");
            Debug.Log("Recording stopped. Press E to replay Echo.");
        }

        /// <summary>
        /// Attempts to label the spawned Echo object with the Echo tag.
        /// </summary>
        /// <param name="echoObject">The runtime Echo object created for playback.</param>
        /// <remarks>
        /// PressurePlate can also detect EchoReplayController, so the mechanic still works if the tag is missing.
        /// </remarks>
        private void TrySetEchoTag(GameObject echoObject)
        {
            try
            {
                echoObject.tag = "Echo";
            }
            catch (UnityException)
            {
                Debug.LogWarning("Echo tag is missing. The Echo can still press plates by component detection, but add an Echo tag in Project Settings if you want tag-based filtering.");
            }
        }

    }
}
