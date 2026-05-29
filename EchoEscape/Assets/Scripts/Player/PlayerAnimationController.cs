using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Drives the player's visual-only Ruby sprite animation from Rigidbody2D motion.
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private float idleFramesPerSecond = 10f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float horizontalRunThreshold = 0.12f;
        [SerializeField] private float airborneVelocityThreshold = 0.25f;

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
            SetState(VisualState.Idle);
        }

        private void Update()
        {
            if (spriteRenderer == null || body == null)
            {
                return;
            }

            Vector2 velocity = body.velocity;
            bool isGrounded = playerController != null
                ? playerController.IsGrounded()
                : Mathf.Abs(velocity.y) <= airborneVelocityThreshold;
            bool isJumping = !isGrounded;
            float horizontalSpeed = Mathf.Abs(velocity.x);

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

            if (horizontalSpeed > horizontalRunThreshold)
            {
                spriteRenderer.flipX = velocity.x < 0f;
            }

            AdvanceFrame();
            UpdateAnimatorParameters(horizontalSpeed, isGrounded, velocity.y);
        }

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

        private void ApplyCurrentFrame()
        {
            if (spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                spriteRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }

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

        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }

        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }

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

        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
