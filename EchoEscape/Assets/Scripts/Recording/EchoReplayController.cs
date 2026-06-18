using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Echo Playback controller. it moves by recording frame Echo, let Echo Replay the route the player just took.
/// Gameplay logic: Load take over ActionRecorder saved frames; FixedUpdate Each physical frame uses Rigidbody2D. MovePosition Move to the next frame position; after all playback Echo It does not disappear, but stops at the last frame, so that it can continue to suppress the mechanism.
/// Collaborates with: ActionRecorder Create and load it; EchoAnimationController show action; PressurePlate can be Echo of Trigger Press down.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EchoReplayController : MonoBehaviour
    {
        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private Rigidbody2D body;
        private EchoAnimationController visual;
        private BoxCollider2D echoCollider;
        private Vector2 normalColliderOffset;
        private Vector2 flippedColliderOffset;
        private int index;
        private bool finished;

        /// <summary>
/// Echo Whether you are still playing back according to the recorded route. After playback is complete Echo It will stop at the last frame and continue to press the machine, but it will return here false。
        /// </summary>
        public bool IsReplaying => frames.Count > 0 && !finished;

        /// <summary>
/// Echo The current number of seconds played. UI Use it to display replay Timing will not affect Echo actual movement.
        /// </summary>
        public float ReplayElapsedSeconds => frames.Count == 0
            ? 0f
            : Mathf.Clamp(frames[Mathf.Clamp(index, 0, frames.Count - 1)].time, 0f, ReplayDurationSeconds);

        /// <summary>
/// Echo The total duration of this recording, from the last frame RecordingFrame of time。
        /// </summary>
        public float ReplayDurationSeconds => frames.Count == 0 ? 0f : Mathf.Max(0f, frames[frames.Count - 1].time);
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            visual = GetComponentInChildren<EchoAnimationController>();
            echoCollider = GetComponent<BoxCollider2D>();
        }
        /// <summary>
/// Unity Called at a fixed physical step size. Processed here Rigidbody move, Echo Playback, etc. require logic to stabilize the physical rhythm.
        /// </summary>
        private void FixedUpdate()
        {
            if (frames.Count == 0)
            {
                return;
            }

            if (!finished)
            {
// Moves the recorded frames one by one during playback instead of re-simulating the input; this way Echo The position and trajectory of the player just now will be completely reproduced.
                RecordingFrame currentFrame = frames[index];
                RecordingFrame previousFrame = index > 0 ? frames[index - 1] : currentFrame;
                ApplyColliderFrame(currentFrame);
                ApplyVisualFrame(currentFrame, previousFrame, false);
                body.MovePosition((Vector2)currentFrame.position);
                index++;

                if (index >= frames.Count)
                {
// Echo It stops at the last frame after playing, which is why it keeps pressing the pressure plate.
                    index = frames.Count - 1;
                    finished = true;
                    ApplyColliderFrame(frames[index]);
                    ApplyVisualFrame(frames[index], frames[index], true);
                    EchoEscapeGameManager.Instance?.UpdateStatus("Echo finished and is holding its final position.");
                    Debug.Log("Echo finished and is holding its final position.");
                }
            }
            else
            {
// Continues even after it has ended MovePosition to the final position to avoid changes in the physics system or parent object. Echo Float away.
                ApplyVisualFrame(frames[index], frames[index], true);
                ApplyColliderFrame(frames[index]);
                body.MovePosition((Vector2)frames[index].position);
            }
        }

        public void ConfigureCollider(BoxCollider2D collider, Vector2 normalOffset, Vector2 flippedOffset)
        {
            echoCollider = collider;
            normalColliderOffset = normalOffset;
            flippedColliderOffset = flippedOffset;
        }

        /// <summary>
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <param name="sourceFrames">ActionRecorder List of saved recording frames. </param>
        public void Load(IEnumerable<RecordingFrame> sourceFrames)
        {
// Copy a copy of the frame data to avoid external frames If the list is later cleared or re-recorded, the currently playing Echo。
            frames.Clear();
            frames.AddRange(sourceFrames);
            index = 0;
            finished = false;

            if (frames.Count > 0)
            {
// When generating, place it at the first frame position to prevent Echo Appears briefly at the origin before the first frame.
                transform.position = frames[0].position;
                ApplyColliderFrame(frames[0]);
                ApplyVisualFrame(frames[0], frames[0], false);
            }
        }
        /// <summary>
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
/// <param name="frame">current Echo records or playback frames. </param>
/// <param name="previousFrame">Previous frame Echo Data to compare movement direction, speed and status changes. </param>
/// <param name="isFinished">Echo Whether playback has reached the last frame. </param>
        private void ApplyVisualFrame(RecordingFrame frame, RecordingFrame previousFrame, bool isFinished)
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<EchoAnimationController>();
            }

            if (visual == null)
            {
                return;
            }

            visual.ApplyFrame(frame, previousFrame, isFinished);
        }

        private void ApplyColliderFrame(RecordingFrame frame)
        {
            if (echoCollider == null)
            {
                echoCollider = GetComponent<BoxCollider2D>();
            }

            if (echoCollider == null)
            {
                return;
            }

            echoCollider.offset = frame.isGravityFlipped ? flippedColliderOffset : normalColliderOffset;
        }
    }
}
