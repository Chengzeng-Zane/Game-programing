using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Echo One frame of recorded data. It records the player's position, time, facing direction, and gravity state at a certain physics frame.
/// Gameplay logic: ActionRecorder Save a lot in a row RecordingFrame，EchoReplayController Reading them in sequence will restore the player's previous route.
/// Collaborates with: ActionRecorder write; EchoReplayController and EchoAnimationController Read.
    /// </summary>
    public struct RecordingFrame
    {
        public Vector3 position;
        public float time;
        public bool facingRight;
        public bool isGravityFlipped;
        /// <summary>
/// Constructor: Create this data object and save the incoming fields so that other scripts can read them in a unified format.
        /// </summary>
/// <param name="position">Target world coordinates, often used for respawn, spawning objects or recording Echo frame. </param>
/// <param name="time">time Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="this(position">this(position Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="time">time Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingRight">facingRight Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="false">false Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public RecordingFrame(Vector3 position, float time, bool facingRight)
            : this(position, time, facingRight, false)
        {
        }
        /// <summary>
/// Constructor: Create this data object and save the incoming fields so that other scripts can read them in a unified format.
        /// </summary>
/// <param name="position">Target world coordinates, often used for respawn, spawning objects or recording Echo frame. </param>
/// <param name="time">time Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingRight">facingRight Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="isGravityFlipped">isGravityFlipped Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public RecordingFrame(Vector3 position, float time, bool facingRight, bool isGravityFlipped)
        {
            this.position = position;
            this.time = time;
            this.facingRight = facingRight;
            this.isGravityFlipped = isGravityFlipped;
        }
    }
}
