using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Drives the visual-only Ruby ghost animation for an Echo replay object.
    /// </summary>
    /// <remarks>
    /// Attach this script to the EchoVisual child object created by ActionRecorder.
    /// It reads RecordingFrame data from EchoReplayController and displays a tinted Ruby animation.
    /// It does not move the Echo or press buttons by itself.
    /// </remarks>
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
        /// Description:
        /// Called when the Echo visual is created.
        /// It finds visual components, loads Ruby sprite frames, and starts in idle state.
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
        /// Description:
        /// Called every frame by Unity.
        /// It advances the current sprite animation.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
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

        /// <summary>
        /// Description:
        /// Changes the Echo visual state between idle, run, and jump.
        /// Inputs:
        /// nextState - visual animation state to show
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
                _ => idleFrames
            };

            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }

        /// <summary>
        /// Description:
        /// Moves to the next sprite frame when enough time has passed.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
        /// Description:
        /// Applies the current sprite frame to the SpriteRenderer.
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
        /// Sends Echo movement values to an Animator if one exists.
        /// Inputs:
        /// speed - horizontal movement speed
        /// isGrounded - true when the Echo is not jumping
        /// verticalVelocity - vertical movement speed
        /// isGravityFlipped - true when the recorded frame was upside down
        /// Returns:
        /// void (no return)
        /// </summary>
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
        /// Loads and sorts Ruby animation frames from Resources.
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
