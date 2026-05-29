using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    // 这个脚本让生成出来的回声对象按保存的玩家移动帧进行回放。
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
        private EchoAnimationController visual;
        private int index;
        private bool finished;

        // 这个函数在回声对象创建时运行，用来准备 Rigidbody2D 和碰撞设置。
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
            visual = GetComponentInChildren<EchoAnimationController>();
        }

        // 这个函数按固定物理帧运行，用来更新 Rigidbody2D 移动速度。
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
                RecordingFrame currentFrame = frames[index];
                RecordingFrame previousFrame = index > 0 ? frames[index - 1] : currentFrame;
                ApplyVisualFrame(currentFrame, previousFrame, false);
                body.MovePosition((Vector2)currentFrame.position);
                index++;

                if (index >= frames.Count)
                {
                    index = frames.Count - 1;
                    finished = true;
                    ApplyVisualFrame(frames[index], frames[index], true);
                    EchoEscapeGameManager.Instance?.UpdateStatus("Echo finished and is holding its final position.");
                    Debug.Log("Echo finished and is holding its final position.");
                }
            }
            else
            {
                ApplyVisualFrame(frames[index], frames[index], true);
                body.MovePosition((Vector2)frames[index].position);
            }
        }

        // 这个函数把玩家录制好的帧数据加载到当前回声对象里。
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
                ApplyVisualFrame(frames[0], frames[0], false);
            }
        }

        // 这个函数把录制时的朝向应用到回声的像素角色外观上。
        /// <summary>
        /// Applies recorded facing direction to an optional pixel character visual.
        /// </summary>
        /// <param name="frame">The frame containing the facing direction to display.</param>
        /// <remarks>
        /// The current Echo is a square, so this safely does nothing unless a character visual exists.
        /// </remarks>
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
    }
}
