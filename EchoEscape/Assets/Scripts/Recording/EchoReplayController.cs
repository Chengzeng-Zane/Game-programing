using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EchoReplayController : MonoBehaviour
    {
        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private Rigidbody2D body;
        private int index;
        private bool finished;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FixedUpdate()
        {
            if (frames.Count == 0)
            {
                return;
            }

            if (!finished)
            {
                body.MovePosition(frames[index].position);
                index++;

                if (index >= frames.Count)
                {
                    index = frames.Count - 1;
                    finished = true;
                    EchoEscapeGameManager.Instance?.UpdateStatus("Echo finished and is holding its final position.");
                }
            }
            else
            {
                body.MovePosition(frames[index].position);
            }
        }

        public void Load(IEnumerable<RecordingFrame> sourceFrames)
        {
            frames.Clear();
            frames.AddRange(sourceFrames);
            index = 0;
            finished = false;
        }
    }
}
