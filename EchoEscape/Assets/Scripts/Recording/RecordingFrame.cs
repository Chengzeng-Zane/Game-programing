using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：Echo 录制的一帧数据。它记录玩家在某个物理帧的位置、时间、朝向和重力状态。
    /// 玩法逻辑：ActionRecorder 连续保存很多 RecordingFrame，EchoReplayController 再按顺序读取，就能还原玩家刚才的路线。
    /// 协作关系：ActionRecorder 写入；EchoReplayController 和 EchoAnimationController 读取。
    /// </summary>
    public struct RecordingFrame
    {
        public Vector3 position;
        public float time;
        public bool facingRight;
        public bool isGravityFlipped;
        /// <summary>
        /// 构造函数：创建这个数据对象，并把传入的字段保存起来，方便其他脚本用统一格式读取。
        /// </summary>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        /// <param name="time">time 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="this(position">this(position 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="time">time 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingRight">facingRight 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="false">false 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public RecordingFrame(Vector3 position, float time, bool facingRight)
            : this(position, time, facingRight, false)
        {
        }
        /// <summary>
        /// 构造函数：创建这个数据对象，并把传入的字段保存起来，方便其他脚本用统一格式读取。
        /// </summary>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        /// <param name="time">time 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingRight">facingRight 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="isGravityFlipped">isGravityFlipped 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public RecordingFrame(Vector3 position, float time, bool facingRight, bool isGravityFlipped)
        {
            this.position = position;
            this.time = time;
            this.facingRight = facingRight;
            this.isGravityFlipped = isGravityFlipped;
        }
    }
}
