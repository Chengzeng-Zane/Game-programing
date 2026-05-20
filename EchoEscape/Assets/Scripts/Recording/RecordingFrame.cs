using UnityEngine;

namespace EchoEscape
{
    public struct RecordingFrame
    {
        public Vector2 position;
        public float time;
        public bool facingRight;

        public RecordingFrame(Vector2 position, float time, bool facingRight)
        {
            this.position = position;
            this.time = time;
            this.facingRight = facingRight;
        }
    }
}
