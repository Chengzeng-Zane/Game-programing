using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Drives the visual-only Ruby ghost animation for an Echo replay object.
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

        private void Update()
        {
            AdvanceFrame();
        }

        /// <summary>
        /// Applies the recorded visual state for one Echo replay frame.
        /// </summary>
        public void ApplyFrame(RecordingFrame frame, RecordingFrame previousFrame, bool finished)
        {
            float frameTime = Mathf.Max(0.0001f, frame.time - previousFrame.time);
            Vector2 velocity = ((Vector2)frame.position - (Vector2)previousFrame.position) / frameTime;

            if (finished)
            {
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
                spriteRenderer.flipX = !frame.facingRight;
                spriteRenderer.color = echoTint;
            }

            transform.localPosition = frame.isGravityFlipped ? flippedVisualOffset : normalVisualOffset;
            transform.localRotation = Quaternion.Euler(0f, 0f, frame.isGravityFlipped ? 180f : 0f);
            UpdateAnimatorParameters(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y) <= verticalJumpThreshold, velocity.y, frame.isGravityFlipped);
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

        private void UpdateAnimatorParameters(float speed, bool isGrounded, float verticalVelocity, bool isGravityFlipped)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            SetAnimatorFloat("Speed", speed);
            SetAnimatorBool("IsGrounded", isGrounded);
            SetAnimatorFloat("VerticalVelocity", verticalVelocity);
            SetAnimatorBool("IsGravityFlipped", isGravityFlipped);
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
