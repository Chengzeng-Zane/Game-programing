using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：Ruby 玩家角色动画控制器。它根据玩家移动状态播放待机、跑步、跳跃、攻击和死亡动画。
    /// 玩法逻辑：Update 根据 Rigidbody2D 速度和 PlayerController2D.IsGrounded 判断当前动作；攻击时 PlayerAttack 调用 PlayAttack 临时锁定攻击帧；死亡时 GameManager 调用 PlayDeath 锁住普通动画，保证死亡视觉不会被待机/跑步覆盖。
    /// 协作关系：读取 Rigidbody2D 和 PlayerController2D；被 PlayerAttack 和 EchoEscapeGameManager 调用。它只控制视觉，不改变移动或伤害。
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float runFramesPerSecond = 16f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float attackFramesPerSecond = 14f;
        [SerializeField] private float attackDuration = 0.28f;
        [SerializeField] private float deathFramesPerSecond = 15f;
        [SerializeField] private float horizontalRunThreshold = 0.35f;
        [SerializeField] private float airborneVelocityThreshold = 0.25f;
        [SerializeField] private int idleHoldFrameIndex = 4;

        private const string IdlePath = "Ancient Forest 1.6/Ruby V4 - Demo/Idle/ruby_idle-Sheet";
        private const string RunPath = "Ancient Forest 1.6/Ruby V4 - Demo/Run/run-Sheet";
        private const string JumpPath = "Ancient Forest 1.6/Ruby V4 - Demo/Jump/Sheet/jump_up-Sheet";
        private const string AttackPath = "Ancient Forest 1.6/Ruby V4 - Demo/Attack/attack_1-Sheet";
        private const string DeathPath = "Ancient Forest 1.6/Ruby V4 - Demo/Death/deatht-Sheet";

        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private Sprite[] jumpFrames;
        private Sprite[] attackFrames;
        private Sprite[] deathFrames;
        private Sprite[] currentFrames;
        private int currentFrameIndex;
        private float frameTimer;
        private float attackTimer;
        private Coroutine deathRoutine;
        private VisualState currentState = VisualState.Idle;
        private bool animationLocked;

        private enum VisualState
        {
            Idle,
            Run,
            Jump,
            Attack,
            Death
        }
        /// <summary>
        /// 编辑器中参数变化时调用。这里用于限制 Inspector 参数范围，避免运行时出现非法设置。
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator != null)
            {
                // 当前角色动画由脚本逐帧换 Sprite 控制，禁用 Animator 防止它覆盖脚本选中的帧。
                animator.enabled = false;
            }

            Sprite[] previewIdleFrames = LoadFrames(IdlePath);
            if (spriteRenderer != null && previewIdleFrames.Length > 0)
            {
                // 编辑器里直接预览指定 idle 帧，避免场景视图显示成不合适的抬脚姿势。
                int targetFrameIndex = Mathf.Clamp(idleHoldFrameIndex, 0, previewIdleFrames.Length - 1);
                spriteRenderer.sprite = previewIdleFrames[targetFrameIndex];
            }
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

            if (animator != null)
            {
                // 运行时同样禁用 Animator，保证 Ruby 的帧动画完全由本脚本控制。
                animator.enabled = false;
            }

            if (body == null)
            {
                body = GetComponentInParent<Rigidbody2D>();
            }

            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController2D>();
            }

            idleFrames = LoadFrames(IdlePath);
            runFrames = LoadFrames(RunPath);
            jumpFrames = LoadFrames(JumpPath);
            attackFrames = LoadFrames(AttackPath);
            deathFrames = LoadFrames(DeathPath);
            // 初始显示 idle；SetState 会进一步调用 HoldIdleFrame 固定到更自然的站立帧。
            SetState(VisualState.Idle);
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
        /// </summary>
        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (animationLocked)
            {
                // 死亡动画会锁住普通动画，避免下一帧又被 idle/run 覆盖。
                return;
            }

            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.velocity;
            float horizontalSpeed = Mathf.Abs(velocity.x);

            if (attackTimer > 0f && attackFrames.Length > 0)
            {
                // 攻击期间优先显示攻击动画，不根据移动速度切回跑步或待机。
                attackTimer -= Time.deltaTime;
                SetState(VisualState.Attack);
                UpdateFacing(velocity, horizontalSpeed);
                AdvanceFrame(false);
                UpdateAnimatorParameters(horizontalSpeed, IsGrounded(velocity), velocity.y);
                return;
            }

            bool isGrounded = playerController != null
                ? playerController.IsGrounded()
                : Mathf.Abs(velocity.y) <= airborneVelocityThreshold;
            bool isJumping = !isGrounded;

            // 状态优先级：跳跃 > 跑步 > 待机。这样空中移动不会错误显示跑步帧。
            if (isJumping)
            {
                SetState(VisualState.Jump);
            }
            else if (horizontalSpeed > horizontalRunThreshold)
            {
                SetState(VisualState.Run);
            }
            else
            {
                SetState(VisualState.Idle);
                // 待机不循环完整 idle 表，固定到自然站立帧，避免角色静止时像在走路。
                HoldIdleFrame();
            }

            UpdateFacing(velocity, horizontalSpeed);

            if (currentState != VisualState.Idle)
            {
                // idle 固定帧不推进；run/jump/attack/death 才按帧率播放。
                AdvanceFrame();
            }

            UpdateAnimatorParameters(horizontalSpeed, isGrounded, velocity.y);
        }
        /// <summary>
        /// 播放玩家攻击动画。PlayerAttack 命中逻辑独立处理，本函数只负责视觉上挥剑。
        /// </summary>
        public void PlayAttack()
        {
            if (animationLocked)
            {
                return;
            }

            if (attackFrames == null || attackFrames.Length == 0)
            {
                return;
            }

            float fullClipDuration = attackFrames.Length / Mathf.Max(1f, attackFramesPerSecond);
            // 攻击至少持续 attackDuration，也至少播完素材本身长度，避免动画被提前截断。
            attackTimer = Mathf.Max(attackDuration, fullClipDuration);
            SetState(VisualState.Attack);
        }
        /// <summary>
        /// 播放玩家死亡动画并锁定普通动画状态。GameManager 会根据返回时长决定何时显示死亡 UI 和重载关卡。
        /// </summary>
        /// <param name="deathSource">deathSource 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回浮点数结果，通常表示时间、距离、速度或动画时长。</returns>
        public float PlayDeath(string deathSource = "death")
        {
            if (spriteRenderer == null)
            {
                return 0f;
            }

            string spriteBefore = spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "none";
            bool animatorEnabledBefore = animator != null && animator.enabled;
            bool stoppedDeathCoroutine = deathRoutine != null;
            bool stoppedAttackAnimation = attackTimer > 0f;

            attackTimer = 0f;
            animationLocked = true;

            if (deathRoutine != null)
            {
                // 如果短时间内重复调用死亡，先停掉旧协程，避免两个死亡动画同时推进帧。
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            if (animator != null && animator.enabled)
            {
                // 防止 Animator 在死亡动画期间继续覆盖 SpriteRenderer。
                animator.enabled = false;
            }

            spriteRenderer.color = Color.white;

            if (deathFrames == null || deathFrames.Length == 0)
            {
                // 死亡素材缺失时不让流程报错，用 idle 帧兜底，GameManager 仍会继续重载关卡。
                ForceIdleFrame();
                Debug.LogWarning(
                    $"[PlayerDeathVisual] Death sprites missing at Resources/{DeathPath}; " +
                    $"falling back to idle frame. deathSource={deathSource}, spriteBefore={spriteBefore}");
                return 0f;
            }

            currentState = VisualState.Death;
            currentFrames = deathFrames;
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();

            float duration = deathFrames.Length / Mathf.Max(1f, deathFramesPerSecond);
            deathRoutine = StartCoroutine(PlayDeathSequence(duration));
            string spriteAfter = spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "none";

            Debug.Log(
                $"[PlayerDeathVisual] deathSource={deathSource}, method=PlayerAnimationController.PlayDeath, " +
                $"playHurtCalled=false, deathFrames={deathFrames.Length}, deathDuration={duration:F2}, " +
                $"spriteBefore={spriteBefore}, spriteAfter={spriteAfter}, stoppedDeathCoroutine={stoppedDeathCoroutine}, " +
                $"stoppedAttackAnimation={stoppedAttackAnimation}, " +
                $"animationLocked={animationLocked}, animatorEnabledBefore={animatorEnabledBefore}, " +
                $"animatorEnabledAfter={(animator != null && animator.enabled)}");

            return duration;
        }
        /// <summary>
        /// 强制显示第一帧待机图。它是死亡素材缺失或恢复显示时的安全兜底。
        /// </summary>
        private void ForceIdleFrame()
        {
            if (idleFrames == null || idleFrames.Length == 0)
            {
                return;
            }

            currentState = VisualState.Idle;
            currentFrames = idleFrames;
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        /// <summary>
        /// 待机时固定到指定 idle 帧，避免完整 idle 序列里不适合静止展示的过渡帧循环出现。
        /// </summary>
        private void HoldIdleFrame()
        {
            if (idleFrames == null || idleFrames.Length == 0)
            {
                return;
            }

            int targetFrameIndex = Mathf.Clamp(idleHoldFrameIndex, 0, idleFrames.Length - 1);
            if (currentFrames != idleFrames || currentFrameIndex == targetFrameIndex)
            {
                // 已经不是 idle 或已经在目标帧时，不重复刷新 Sprite。
                return;
            }

            currentFrameIndex = targetFrameIndex;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        /// <summary>
        /// 切换当前动画状态，并把 currentFrames 指向对应素材数组。
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
                VisualState.Attack => attackFrames,
                VisualState.Death => deathFrames,
                _ => idleFrames
            };

            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();

            if (nextState == VisualState.Idle)
            {
                // 进入待机状态后立刻固定自然站立帧。
                HoldIdleFrame();
            }
        }
        /// <summary>
        /// 推进动画帧或流程计时，让动画按帧率继续播放。
        /// </summary>
        /// <param name="loop">true 表示动画循环播放，false 表示播到最后一帧停住。</param>
        private void AdvanceFrame(bool loop = true)
        {
            if (currentFrames == null || currentFrames.Length <= 1)
            {
                return;
            }

            float framesPerSecond = currentState switch
            {
                VisualState.Run => runFramesPerSecond,
                VisualState.Jump => jumpFramesPerSecond,
                VisualState.Attack => attackFramesPerSecond,
                VisualState.Death => deathFramesPerSecond,
                _ => idleFramesPerSecond
            };

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                // 用 while 而不是 if，保证低帧率卡顿时动画能补足跳过的帧。
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }
        /// <summary>
        /// 判断动画系统是否应该认为玩家落地。优先用 PlayerController2D 的真实落地检测，没有时才用速度兜底。
        /// </summary>
        /// <param name="velocity">velocity 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsGrounded(Vector2 velocity)
        {
            return playerController != null
                ? playerController.IsGrounded()
                : Mathf.Abs(velocity.y) <= airborneVelocityThreshold;
        }
        /// <summary>
        /// 根据移动方向或 PlayerController2D.FacingRight 翻转 Ruby 精灵。
        /// </summary>
        /// <param name="velocity">velocity 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="horizontalSpeed">horizontalSpeed 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void UpdateFacing(Vector2 velocity, float horizontalSpeed)
        {
            if (horizontalSpeed > horizontalRunThreshold)
            {
                // 有明显水平移动时按速度方向朝向。
                spriteRenderer.flipX = velocity.x < 0f;
            }
            else if (playerController != null)
            {
                // 静止时沿用玩家最后输入朝向，避免停下后角色突然转回默认方向。
                spriteRenderer.flipX = !playerController.FacingRight;
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
        /// 用 unscaled time 播放死亡帧。死亡流程可能暂停时间，所以不能依赖普通 Time.deltaTime。
        /// </summary>
        /// <param name="duration">等待或播放持续时间。</param>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator PlayDeathSequence(float duration)
        {
            if (deathFrames == null || deathFrames.Length == 0)
            {
                deathRoutine = null;
                yield break;
            }

            float frameDuration = 1f / Mathf.Max(1f, deathFramesPerSecond);
            float elapsed = 0f;

            while (elapsed < duration && currentFrameIndex < deathFrames.Length - 1)
            {
                elapsed += Time.unscaledDeltaTime;
                frameTimer += Time.unscaledDeltaTime;
                while (frameTimer >= frameDuration && currentFrameIndex < deathFrames.Length - 1)
                {
                    // 死亡动画不循环，帧推进到最后一张后停住等待 GameManager 重载。
                    frameTimer -= frameDuration;
                    currentFrameIndex++;
                    ApplyCurrentFrame();
                }

                yield return null;
            }

            currentFrameIndex = deathFrames.Length - 1;
            ApplyCurrentFrame();
            deathRoutine = null;
        }
        /// <summary>
        /// 如果角色还保留 Animator Controller，就把移动、落地和反重力参数同步进去。当前主要逻辑仍由脚本换帧控制。
        /// </summary>
        /// <param name="speed">水平速度，用于设置 Animator 参数。</param>
        /// <param name="isGrounded">玩家或 Echo 是否落地，用于动画状态判断。</param>
        /// <param name="verticalVelocity">竖直速度，用于跳跃/下落动画参数。</param>
        private void UpdateAnimatorParameters(float speed, bool isGrounded, float verticalVelocity)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                // 大多数场景已经禁用 Animator；没有 Controller 时直接跳过，不影响脚本帧动画。
                return;
            }

            SetAnimatorFloat("Speed", speed);
            SetAnimatorBool("IsGrounded", isGrounded);
            SetAnimatorFloat("VerticalVelocity", verticalVelocity);
            SetAnimatorBool("IsGravityFlipped", body != null && body.gravityScale < 0f);
        }
        /// <summary>
        /// 安全设置 Animator float 参数。只有参数存在时才设置，避免 Controller 缺参数报错。
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
        /// 安全设置 Animator bool 参数。只有参数存在时才设置，避免 Controller 缺参数报错。
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
        /// 检查 Animator Controller 是否真的包含某个参数。这样换 Controller 或删参数时脚本不会报错。
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
            // Resources.LoadAll 返回顺序不稳定，排序后才能按素材帧名稳定播放动画。
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }

    }
}
