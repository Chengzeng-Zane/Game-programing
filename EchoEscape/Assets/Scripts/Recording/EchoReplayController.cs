using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EchoReplayController : MonoBehaviour
    {
        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private Rigidbody2D body;
        private PixelCharacterVisual visual;
        private int index;
        private bool finished;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            visual = GetComponent<PixelCharacterVisual>();
        }

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
