using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Replays saved player movement frames on a spawned Echo object.
    /// </summary>
    /// <remarks>
    /// Attach this script to the runtime Echo object created by ActionRecorder.
    /// It moves a kinematic Rigidbody2D through recorded positions and holds the final position
    /// so the Echo can keep a PressurePlate pressed after playback finishes.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EchoReplayController : MonoBehaviour
    {
        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private Rigidbody2D body;
        private PixelCharacterVisual visual;
        private int index;
        private bool finished;

        /// <summary>
        /// Unity event method called when the Echo object is created.
        /// </summary>
        /// <remarks>
        /// Caches the Rigidbody2D used for MovePosition playback.
        /// </remarks>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            visual = GetComponent<PixelCharacterVisual>();
        }

        /// <summary>
        /// Unity physics event method called at a fixed timestep.
        /// </summary>
        /// <remarks>
        /// Moves the Echo through one recorded frame per physics tick.
        /// After the final frame, the Echo remains at that final position instead of disappearing.
        /// </remarks>
        private void FixedUpdate()
        {
            if (frames.Count == 0)
            {
                return;
            }

            if (!finished)
            {
                ApplyFacing(frames[index]);
                body.MovePosition(frames[index].position);
                index++;

                if (index >= frames.Count)
                {
                    index = frames.Count - 1;
                    finished = true;
                    EchoEscapeGameManager.Instance?.UpdateStatus("Echo finished and is holding its final position.");
                    Debug.Log("Echo finished and is holding its final position.");
                }
            }
            else
            {
                ApplyFacing(frames[index]);
                body.MovePosition(frames[index].position);
            }
        }

        /// <summary>
        /// Loads the recorded player frames that this Echo should replay.
        /// </summary>
        /// <param name="sourceFrames">The saved movement frames from ActionRecorder.</param>
        public void Load(IEnumerable<RecordingFrame> sourceFrames)
        {
            frames.Clear();
            frames.AddRange(sourceFrames);
            index = 0;
            finished = false;

            if (frames.Count > 0)
            {
                transform.position = frames[0].position;
                ApplyFacing(frames[0]);
            }
        }

        /// <summary>
        /// Applies recorded facing direction to an optional pixel character visual.
        /// </summary>
        /// <param name="frame">The frame containing the facing direction to display.</param>
        /// <remarks>
        /// The current Echo is a square, so this safely does nothing unless a character visual exists.
        /// </remarks>
        private void ApplyFacing(RecordingFrame frame)
        {
            if (visual == null)
            {
                return;
            }

            Transform sprite = transform.Find("Echo Pixel Sprite");
            if (sprite != null)
            {
                SpriteRenderer renderer = sprite.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.flipX = !frame.facingRight;
                }
            }
        }
    }
}
