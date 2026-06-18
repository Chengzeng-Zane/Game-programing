using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Ruby Player character animation controller. It plays standby, running, jumping, attack and death animations based on the player's movement status.
/// Gameplay logic: Update according to Rigidbody2D speed and PlayerController2D. IsGrounded determines the current action; when attacking PlayerAttack call PlayAttack Temporarily locks attack frame; upon death GameManager call PlayDeath Lock normal animation to ensure that death vision will not be on standby/Running coverage.
/// Collaboration: reads Rigidbody2D and PlayerController2D; quilt PlayerAttack and EchoEscapeGameManager call. It only controls vision and does not change movement or damage.
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
/// Called when parameters change in the editor. used here to limit Inspector Parameter range to avoid illegal settings during runtime.
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
// The current character animation is changed frame by frame by the script Sprite control, disable Animator Prevents it from overwriting frames selected by the script.
                animator.enabled = false;
            }

            Sprite[] previewIdleFrames = LoadFrames(IdlePath);
            if (spriteRenderer != null && previewIdleFrames.Length > 0)
            {
// Preview and specify directly in the editor idle frame to prevent the scene view from displaying inappropriate foot-raising postures.
                int targetFrameIndex = Mathf.Clamp(idleHoldFrameIndex, 0, previewIdleFrames.Length - 1);
                spriteRenderer.sprite = previewIdleFrames[targetFrameIndex];
            }
        }
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
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
// Also disabled at runtime Animator, ensure Ruby The frame animation is completely controlled by this script.
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
// initial display idle；SetState will be called further HoldIdleFrame Fixed to a more natural standing frame.
            SetState(VisualState.Idle);
        }
        /// <summary>
/// Unity Called every frame. This handles input, timers, UI Real-time refresh of state or non-physics.
        /// </summary>
        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (animationLocked)
            {
// The death animation will lock the normal animation to prevent the next frame from being idle/run cover.
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
// During the attack, the attack animation is given priority and does not switch back to running or standby according to the movement speed.
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

// Status Priority: Jump > running > Standby. This way mid-air movement will not incorrectly display running frames.
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
// Standby does not cycle completely idle table, fixed to a natural standing frame to avoid the character appearing to be walking when stationary.
                HoldIdleFrame();
            }

            UpdateFacing(velocity, horizontalSpeed);

            if (currentState != VisualState.Idle)
            {
// idle Fixed frames are not advanced; run/jump/attack/death Only play according to the frame rate.
                AdvanceFrame();
            }

            UpdateAnimatorParameters(horizontalSpeed, isGrounded, velocity.y);
        }
        /// <summary>
/// Play player attack animation. PlayerAttack The hit logic is processed independently, and this function is only responsible for visually swinging the sword.
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
// The attack lasts at least attackDuration, and at least the length of the material itself will be played to prevent the animation from being cut off in advance.
            attackTimer = Mathf.Max(attackDuration, fullClipDuration);
            SetState(VisualState.Attack);
        }
        /// <summary>
/// Play the player death animation and lock the normal animation state. GameManager When the death is displayed will be determined based on the return duration. UI and reload levels.
        /// </summary>
/// <param name="deathSource">deathSource Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns a floating point result, typically representing time, distance, speed, or animation duration. </returns>
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
// If death is called repeatedly in a short period of time, stop the old coroutine first to prevent two death animations from advancing frames at the same time.
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            if (animator != null && animator.enabled)
            {
// prevent Animator Continue coverage during death animation SpriteRenderer。
                animator.enabled = false;
            }

            spriteRenderer.color = Color.white;

            if (deathFrames == null || deathFrames.Length == 0)
            {
// To prevent the process from reporting an error when the death material is missing, use idle The frame is hidden, GameManager Level reloading will still continue.
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
/// Force display of the first frame standby image. It's a safe haven when death footage is missing or restored.
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
/// Fixed to specified when in standby idle frame, avoid complete idle Transition frames in the sequence that are not suitable for static presentation appear repeatedly.
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
// No longer idle Or when it is already in the target frame, it will not be refreshed repeatedly. Sprite。
                return;
            }

            currentFrameIndex = targetFrameIndex;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        /// <summary>
/// Switch the current animation state and put currentFrames Points to the corresponding material array.
        /// </summary>
/// <param name="nextState">The new animation or logic state to switch to. </param>
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
// Immediately after entering standby mode, the natural standing frame is fixed.
                HoldIdleFrame();
            }
        }
        /// <summary>
/// Advance animation frames or process timing so the animation continues to play at the frame rate.
        /// </summary>
/// <param name="loop">true Indicates that the animation plays in a loop, false Indicates that the playback will stop at the last frame. </param>
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
// use while instead of if, to ensure that the animation can make up for the skipped frames when the frame rate is low.
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }
        /// <summary>
/// Determines whether the animation system should consider the player to have landed. priority PlayerController2D Real ground detection, only use speed to get the bottom line if not available.
        /// </summary>
/// <param name="velocity">velocity Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsGrounded(Vector2 velocity)
        {
            return playerController != null
                ? playerController.IsGrounded()
                : Mathf.Abs(velocity.y) <= airborneVelocityThreshold;
        }
        /// <summary>
/// Depending on the direction of movement or PlayerController2D. FacingRight flip Ruby Elf.
        /// </summary>
/// <param name="velocity">velocity Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="horizontalSpeed">horizontalSpeed Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private void UpdateFacing(Vector2 velocity, float horizontalSpeed)
        {
            bool isGravityFlipped = body != null && body.gravityScale < 0f;

            if (horizontalSpeed > horizontalRunThreshold)
            {
// When there is obvious horizontal movement, first move in the direction of world speed.
// Anti-gravity Player The root object will rotate 180 The local left and right of the elf will be opposite to the left and right of the world, so it needs to be inverted here.
                bool shouldFlip = velocity.x < 0f;
                spriteRenderer.flipX = isGravityFlipped ? !shouldFlip : shouldFlip;
            }
            else if (playerController != null)
            {
// When stationary, the last direction input by the player is used; when anti-gravity is used, the left and right reversal caused by the rotation of the root object is also offset.
                bool shouldFlip = !playerController.FacingRight;
                spriteRenderer.flipX = isGravityFlipped ? !shouldFlip : shouldFlip;
            }
        }
        /// <summary>
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
        private void ApplyCurrentFrame()
        {
            if (spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                spriteRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }
        /// <summary>
/// use unscaled time Play the death frame. The death process may pause for time, so you cannot rely on ordinary Time. deltaTime。
        /// </summary>
/// <param name="duration">Wait or play duration. </param>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
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
// The death animation does not loop. The frame advances to the last frame and stops waiting. GameManager Overload.
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
/// If the role remains Animator Controller, and synchronize the movement, landing and anti-gravity parameters. The current main logic is still controlled by script frame changing.
        /// </summary>
/// <param name="speed">Horizontal speed, used to set Animator parameter. </param>
/// <param name="isGrounded">player or Echo Whether it has landed is used to determine the animation status. </param>
/// <param name="verticalVelocity">Vertical speed, used for jumping/Fall animation parameters. </param>
        private void UpdateAnimatorParameters(float speed, bool isGrounded, float verticalVelocity)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
// Most scenes have been disabled Animator; No Controller Skip directly without affecting the script frame animation.
                return;
            }

            SetAnimatorFloat("Speed", speed);
            SetAnimatorBool("IsGrounded", isGrounded);
            SetAnimatorFloat("VerticalVelocity", verticalVelocity);
            SetAnimatorBool("IsGravityFlipped", body != null && body.gravityScale < 0f);
        }
        /// <summary>
/// Security settings Animator float parameter. Set only when the parameter exists to avoid Controller Missing parameters error.
        /// </summary>
/// <param name="parameterName">Animator Parameter name. </param>
/// <param name="value">The new parameter value to set. </param>
        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }
        /// <summary>
/// Security settings Animator bool parameter. Set only when the parameter exists to avoid Controller Missing parameters error.
        /// </summary>
/// <param name="parameterName">Animator Parameter name. </param>
/// <param name="value">The new parameter value to set. </param>
        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }
        /// <summary>
/// examine Animator Controller Whether a certain parameter is really included. changes this Controller Or the script will not report an error when deleting parameters.
        /// </summary>
/// <param name="parameterName">Animator Parameter name. </param>
/// <param name="type">expected Animator Parameter type. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
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
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <param name="resourcePath">Resources The resource path in the directory, excluding the extension. </param>
/// <returns>Return a set Sprite Animation frames; may be an empty array if the resource does not exist. </returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
// Resources. LoadAll The return order is unstable. Only after sorting can the animation be played stably by the material frame name.
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }

    }
}
