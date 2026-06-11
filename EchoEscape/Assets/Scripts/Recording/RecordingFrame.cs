using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Stores one sampled moment of the player's movement for the Echo replay system.
    /// </summary>
    /// <remarks>
    /// The ActionRecorder creates a list of these frames while the player is recording.
    /// EchoReplayController later reads the same frames to move the Echo through the saved path.
    /// </remarks>
    public struct RecordingFrame
    {
        /// <summary>
        /// The world position of the player when this frame was recorded.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The elapsed recording time when this frame was captured.
        /// </summary>
        public float time;

        /// <summary>
        /// True when the player was facing right during this frame.
        /// </summary>
        public bool facingRight;

        /// <summary>
        /// True when the player was upside down from gravity flip during this frame.
        /// </summary>
        public bool isGravityFlipped;

        /// <summary>
        /// Creates one saved movement frame for later Echo playback.
        /// </summary>
        /// <param name="position">The player position to replay.</param>
        /// <param name="time">The elapsed time since recording started.</param>
        /// <param name="facingRight">Whether the player was facing right at this frame.</param>
        public RecordingFrame(Vector3 position, float time, bool facingRight)
            : this(position, time, facingRight, false)
        {
        }

        /// <summary>
        /// Creates one saved movement frame for later Echo playback.
        /// </summary>
        /// <param name="position">The player position to replay.</param>
        /// <param name="time">The elapsed time since recording started.</param>
        /// <param name="facingRight">Whether the player was facing right at this frame.</param>
        /// <param name="isGravityFlipped">Whether the player was upside down at this frame.</param>
        public RecordingFrame(Vector3 position, float time, bool facingRight, bool isGravityFlipped)
        {
            this.position = position;
            this.time = time;
            this.facingRight = facingRight;
            this.isGravityFlipped = isGravityFlipped;
        }
    }
}
