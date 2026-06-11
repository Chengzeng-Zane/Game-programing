using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Drives the player's visual-only Ruby sprite animation from Rigidbody2D motion.
    /// </summary>
    /// <remarks>
    /// Attach this script to the PlayerVisual child object.
    /// It chooses idle, run, jump, attack, or death sprites based on player motion and combat events.
    /// The Player root still owns movement, physics, recording, and attacks.
    /// </remarks>
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private float idleFramesPerSecond = 10f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float attackFramesPerSecond = 14f;
        [SerializeField] private float attackDuration = 0.28f;
        [SerializeField] private float deathFramesPerSecond = 15f;
        [SerializeField] private float horizontalRunThreshold = 0.12f;
        [SerializeField] private float airborneVelocityThreshold = 0.25f;

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
        /// Description:
        /// Called when the player visual is created.
        /// It finds needed components and loads Ruby animation frames from Resources.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
            SetState(VisualState.Idle);
        }

        /// <summary>
        /// Description:
        /// Called every frame by Unity.
        /// It chooses the correct player animation based on velocity, grounded state, attack state, and death lock.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (animationLocked)
            {
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
            }

            UpdateFacing(velocity, horizontalSpeed);

            AdvanceFrame();
            UpdateAnimatorParameters(horizontalSpeed, isGrounded, velocity.y);
        }

        /// <summary>
        /// Description:
        /// Starts the Ruby attack animation.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
            attackTimer = Mathf.Max(attackDuration, fullClipDuration);
            SetState(VisualState.Attack);
        }

        /// <summary>
        /// Description:
        /// Starts the Ruby death animation and locks normal movement animation.
        /// Inputs:
        /// deathSource - text used in debug messages
        /// Returns:
        /// float - death animation duration, or 0 if death frames are missing
        /// </summary>
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
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }

            spriteRenderer.color = Color.white;

            if (deathFrames == null || deathFrames.Length == 0)
            {
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
        /// Description:
        /// Shows the first idle sprite when death frames are missing.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
        /// Description:
        /// Changes the current Ruby animation state.
        /// Inputs:
        /// nextState - animation state to show
        /// Returns:
        /// void (no return)
        /// </summary>
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
        }

        /// <summary>
        /// Description:
        /// Advances the current sprite animation by time.
        /// Inputs:
        /// loop - true to loop frames, false to stop on the last frame
        /// Returns:
        /// void (no return)
        /// </summary>
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
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }

        /// <summary>
        /// Description:
        /// Checks whether the player should be treated as grounded for animation.
        /// Inputs:
        /// velocity - current Rigidbody2D velocity
        /// Returns:
        /// bool - true if idle/run animation can be used
        /// </summary>
        private bool IsGrounded(Vector2 velocity)
        {
            return playerController != null
                ? playerController.IsGrounded()
                : Mathf.Abs(velocity.y) <= airborneVelocityThreshold;
        }

        /// <summary>
        /// Description:
        /// Flips the Ruby sprite to face the movement or player input direction.
        /// Inputs:
        /// velocity - current Rigidbody2D velocity
        /// horizontalSpeed - absolute horizontal speed
        /// Returns:
        /// void (no return)
        /// </summary>
        private void UpdateFacing(Vector2 velocity, float horizontalSpeed)
        {
            if (horizontalSpeed > horizontalRunThreshold)
            {
                spriteRenderer.flipX = velocity.x < 0f;
            }
            else if (playerController != null)
            {
                spriteRenderer.flipX = !playerController.FacingRight;
            }
        }

        /// <summary>
        /// Description:
        /// Applies the selected animation frame to the SpriteRenderer.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void ApplyCurrentFrame()
        {
            if (spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                spriteRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }

        /// <summary>
        /// Description:
        /// Plays the death sprite frames using unscaled time.
        /// Inputs:
        /// duration - total death animation duration
        /// Returns:
        /// IEnumerator - Unity coroutine steps for the death animation
        /// </summary>
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
        /// Description:
        /// Sends player movement values to an Animator if one exists.
        /// Inputs:
        /// speed - horizontal speed
        /// isGrounded - true when the player is on ground
        /// verticalVelocity - vertical Rigidbody2D velocity
        /// Returns:
        /// void (no return)
        /// </summary>
        private void UpdateAnimatorParameters(float speed, bool isGrounded, float verticalVelocity)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            SetAnimatorFloat("Speed", speed);
            SetAnimatorBool("IsGrounded", isGrounded);
            SetAnimatorFloat("VerticalVelocity", verticalVelocity);
            SetAnimatorBool("IsGravityFlipped", body != null && body.gravityScale < 0f);
        }

        /// <summary>
        /// Description:
        /// Safely sets a float Animator parameter if it exists.
        /// Inputs:
        /// parameterName - Animator parameter name
        /// value - value to assign
        /// Returns:
        /// void (no return)
        /// </summary>
        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }

        /// <summary>
        /// Description:
        /// Safely sets a bool Animator parameter if it exists.
        /// Inputs:
        /// parameterName - Animator parameter name
        /// value - value to assign
        /// Returns:
        /// void (no return)
        /// </summary>
        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }

        /// <summary>
        /// Description:
        /// Checks whether the Animator contains a parameter before setting it.
        /// Inputs:
        /// parameterName - Animator parameter name
        /// type - expected Animator parameter type
        /// Returns:
        /// bool - true if the parameter exists with the expected type
        /// </summary>
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
        /// Description:
        /// Loads and sorts Ruby sprite frames from Resources.
        /// Inputs:
        /// resourcePath - path under Assets/Resources without file extension
        /// Returns:
        /// Sprite[] - sorted animation frames
        /// </summary>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }

    }
}
