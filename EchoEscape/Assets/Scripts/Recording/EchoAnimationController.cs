using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：Echo 的动画控制器。它让 Echo 回放时看起来像玩家的灵魂复制体，而不是静态方块。
    /// 玩法逻辑：EchoReplayController 每帧把 RecordingFrame 传进来；脚本根据当前帧和上一帧的位置差判断 Echo 是待机、跑步、跳跃还是攻击，并同步反重力状态到 Animator 参数。
    /// 协作关系：由 EchoReplayController 驱动；使用和 PlayerAnimationController 相同的 Ruby 素材。
    /// </summary>
    public class EchoAnimationController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Color echoTint = new Color(0.3f, 0.9f, 1f, 0.55f);
        [SerializeField] private float idleFramesPerSecond = 8f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float horizontalRunThreshold = 0.08f;
        [SerializeField] private float verticalJumpThreshold = 0.18f;
        [SerializeField] private Vector3 normalVisualOffset = new Vector3(0f, -0.58f, -0.03f);
        [SerializeField] private Vector3 flippedVisualOffset = new Vector3(0f, 0.58f, -0.03f);

        private const string IdlePath = "Ancient Forest 1.6/Ruby V4 - Demo/Idle/ruby_idle-Sheet";
        private const string RunPath = "Ancient Forest 1.6/Ruby V4 - Demo/Run/run-Sheet";
        private const string JumpPath = "Ancient Forest 1.6/Ruby V4 - Demo/Jump/Sheet/jump_up-Sheet";

        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private Sprite[] jumpFrames;
        private Sprite[] currentFrames;
        private int currentFrameIndex;
        private float frameTimer;
        private VisualState currentState = VisualState.Idle;

        private enum VisualState
        {
            Idle,
            Run,
            Jump
        }
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            idleFrames = LoadFrames(IdlePath);
            runFrames = LoadFrames(RunPath);
            jumpFrames = LoadFrames(JumpPath);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = echoTint;
            }

            SetState(VisualState.Idle);
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
        /// </summary>
        private void Update()
        {
            AdvanceFrame();
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        /// <param name="frame">当前 Echo 录制或回放帧。</param>
        /// <param name="previousFrame">上一帧 Echo 数据，用来比较移动方向、速度和状态变化。</param>
        /// <param name="finished">finished 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void ApplyFrame(RecordingFrame frame, RecordingFrame previousFrame, bool finished)
        {
            // 用当前帧和上一帧的位置差估算 Echo 速度，再决定显示 idle/run/jump。
            float frameTime = Mathf.Max(0.0001f, frame.time - previousFrame.time);
            Vector2 velocity = ((Vector2)frame.position - (Vector2)previousFrame.position) / frameTime;

            if (finished)
            {
                // 回放结束后 Echo 停在最后位置压机关，所以视觉也固定成 idle。
                SetState(VisualState.Idle);
            }
            else if (Mathf.Abs(velocity.y) > verticalJumpThreshold)
            {
                SetState(VisualState.Jump);
            }
            else if (Mathf.Abs(velocity.x) > horizontalRunThreshold)
            {
                SetState(VisualState.Run);
            }
            else
            {
                SetState(VisualState.Idle);
            }

            if (spriteRenderer != null)
            {
                // Echo 使用玩家同一套素材，但用半透明青色区分本体和回放。
                spriteRenderer.flipX = !frame.facingRight;
                spriteRenderer.color = echoTint;
            }

            // 录制帧里保存了重力状态，所以 Echo 可以显示倒挂时的视觉偏移和旋转。
            transform.localPosition = frame.isGravityFlipped ? flippedVisualOffset : normalVisualOffset;
            transform.localRotation = Quaternion.Euler(0f, 0f, frame.isGravityFlipped ? 180f : 0f);
            UpdateAnimatorParameters(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y) <= verticalJumpThreshold, velocity.y, frame.isGravityFlipped);
        }
        /// <summary>
        /// 切换 Echo 当前动画状态，并选择对应的玩家动画帧数组。
        /// </summary>
        /// <param name="nextState">要切换到的新动画或逻辑状态。</param>
        private void SetState(VisualState nextState)
        {
            if (currentState == nextState && currentFrames != null)
            {
                return;
            }

            currentState = nextState;
            currentFrames = nextState switch
            {
                VisualState.Run => runFrames,
                VisualState.Jump => jumpFrames,
                _ => idleFrames
            };

            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        /// <summary>
        /// 推进动画帧或流程计时，让动画按帧率继续播放。
        /// </summary>
        private void AdvanceFrame()
        {
            if (currentFrames == null || currentFrames.Length <= 1)
            {
                return;
            }

            float framesPerSecond = currentState switch
            {
                VisualState.Run => runFramesPerSecond,
                VisualState.Jump => jumpFramesPerSecond,
                _ => idleFramesPerSecond
            };

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % currentFrames.Length;
                ApplyCurrentFrame();
            }
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        private void ApplyCurrentFrame()
        {
            if (spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                spriteRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }
        /// <summary>
        /// 如果 Echo 视觉上还有 Animator，就同步速度、落地和反重力参数；没有 Animator 时直接跳过。
        /// </summary>
        /// <param name="speed">水平速度，用于设置 Animator 参数。</param>
        /// <param name="isGrounded">玩家或 Echo 是否落地，用于动画状态判断。</param>
        /// <param name="verticalVelocity">竖直速度，用于跳跃/下落动画参数。</param>
        /// <param name="isGravityFlipped">isGravityFlipped 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void UpdateAnimatorParameters(float speed, bool isGrounded, float verticalVelocity, bool isGravityFlipped)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                // 当前主要用脚本换 Sprite，Animator 缺失不影响 Echo 回放。
                return;
            }

            SetAnimatorFloat("Speed", speed);
            SetAnimatorBool("IsGrounded", isGrounded);
            SetAnimatorFloat("VerticalVelocity", verticalVelocity);
            SetAnimatorBool("IsGravityFlipped", isGravityFlipped);
        }
        /// <summary>
        /// 安全设置 Animator float 参数。只有参数存在时才设置，避免 Controller 不匹配时报错。
        /// </summary>
        /// <param name="parameterName">Animator 参数名称。</param>
        /// <param name="value">要设置的新参数值。</param>
        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }
        /// <summary>
        /// 安全设置 Animator bool 参数。只有参数存在时才设置，避免 Controller 不匹配时报错。
        /// </summary>
        /// <param name="parameterName">Animator 参数名称。</param>
        /// <param name="value">要设置的新参数值。</param>
        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }
        /// <summary>
        /// 检查 Animator Controller 是否包含指定参数，避免直接 SetFloat/SetBool 时抛错。
        /// </summary>
        /// <param name="parameterName">Animator 参数名称。</param>
        /// <param name="type">期望的 Animator 参数类型。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == type && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <param name="resourcePath">Resources 目录下的资源路径，不包含扩展名。</param>
        /// <returns>返回一组 Sprite 动画帧；资源不存在时可能是空数组。</returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            // 按名字排序，保证 Echo 动画帧顺序和玩家动画一致。
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
