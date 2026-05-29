using UnityEngine;

namespace EchoEscape
{
    // 这个结构保存玩家某一瞬间的移动数据，供回声回放使用。
    /// <summary>
    /// Stores one sampled moment of the player's movement for the Echo replay system.
    /// </summary>
    /// <remarks>
    /// The ActionRecorder creates a list of these frames while the player is recording.
    /// EchoReplayController later reads the same frames to move the Echo through the saved path.
    /// </remarks>
    public struct RecordingFrame
    {
        // 这个变量保存这一帧录制时玩家的世界坐标位置。
        /// <summary>
        /// The world position of the player when this frame was recorded.
        /// </summary>
        public Vector3 position;

        // 这个变量保存这一帧被记录时已经过去的录制时间。
        /// <summary>
        /// The elapsed recording time when this frame was captured.
        /// </summary>
        public float time;

        // 这个变量表示录制这一帧时玩家是否朝右。
        /// <summary>
        /// True when the player was facing right during this frame.
        /// </summary>
        public bool facingRight;

        /// <summary>
        /// True when the player was upside down from gravity flip during this frame.
        /// </summary>
        public bool isGravityFlipped;

        // 这个构造函数创建一帧保存下来的移动数据，之后给回声回放使用。
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
