using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：Echo 回放控制器。它按录制帧移动 Echo，让 Echo 重演玩家刚才的路线。
    /// 玩法逻辑：Load 接收 ActionRecorder 保存的帧；FixedUpdate 每个物理帧用 Rigidbody2D.MovePosition 移到下一帧位置；全部播放完后 Echo 不消失，而是停在最后一帧，方便持续压住机关。
    /// 协作关系：ActionRecorder 创建并加载它；EchoAnimationController 显示动作；PressurePlate 可以被 Echo 的 Trigger 压住。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EchoReplayController : MonoBehaviour
    {
        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private Rigidbody2D body;
        private EchoAnimationController visual;
        private int index;
        private bool finished;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            visual = GetComponentInChildren<EchoAnimationController>();
        }
        /// <summary>
        /// Unity 按固定物理步长调用。这里处理 Rigidbody 移动、Echo 回放等需要稳定物理节奏的逻辑。
        /// </summary>
        private void FixedUpdate()
        {
            if (frames.Count == 0)
            {
                return;
            }

            if (!finished)
            {
                // 回放时按录制帧逐个移动，而不是重新模拟输入；这样 Echo 会完全复现玩家刚才的位置轨迹。
                RecordingFrame currentFrame = frames[index];
                RecordingFrame previousFrame = index > 0 ? frames[index - 1] : currentFrame;
                ApplyVisualFrame(currentFrame, previousFrame, false);
                body.MovePosition((Vector2)currentFrame.position);
                index++;

                if (index >= frames.Count)
                {
                    // Echo 播完后停在最后一帧，这正是它能持续压住压力板的原因。
                    index = frames.Count - 1;
                    finished = true;
                    ApplyVisualFrame(frames[index], frames[index], true);
                    EchoEscapeGameManager.Instance?.UpdateStatus("Echo finished and is holding its final position.");
                    Debug.Log("Echo finished and is holding its final position.");
                }
            }
            else
            {
                // 已结束时仍持续 MovePosition 到最后位置，避免物理系统或父物体变动让 Echo 漂走。
                ApplyVisualFrame(frames[index], frames[index], true);
                body.MovePosition((Vector2)frames[index].position);
            }
        }
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <param name="sourceFrames">ActionRecorder 保存的录制帧列表。</param>
        public void Load(IEnumerable<RecordingFrame> sourceFrames)
        {
            // 拷贝一份帧数据，避免外部 frames 列表之后被清空或重新录制时影响正在播放的 Echo。
            frames.Clear();
            frames.AddRange(sourceFrames);
            index = 0;
            finished = false;

            if (frames.Count > 0)
            {
                // 生成时先放到第一帧位置，防止 Echo 在第一帧前短暂出现在原点。
                transform.position = frames[0].position;
                ApplyVisualFrame(frames[0], frames[0], false);
            }
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        /// <param name="frame">当前 Echo 录制或回放帧。</param>
        /// <param name="previousFrame">上一帧 Echo 数据，用来比较移动方向、速度和状态变化。</param>
        /// <param name="isFinished">Echo 回放是否已经播放到最后一帧。</param>
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
