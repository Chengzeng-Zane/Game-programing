using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Echo Visual animation controller for clones.
/// EchoReplayController Each frame RecordingFrame Pass in, this script selects according to the position change and gravity state during recording idle、run or jump image.
/// it only controls EchoVisual of SpriteRenderer, facing direction, transparent blue appearance and anti-gravity upside down display, and does not participate in real movement, button triggering or death logic.
/// Echo The real position of EchoReplayController control, Echo The button is pressed EchoReplay on the root object BoxCollider2D trigger。
    /// </summary>
    public class EchoAnimationController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Color echoTint = new Color(0.3f, 0.9f, 1f, 0.55f);
        [SerializeField] private float idleFramesPerSecond = 8f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float jumpFramesPerSecond = 10f;
        [SerializeField] private float horizontalRunThreshold = 0.35f;
        [SerializeField] private float verticalJumpThreshold = 0.18f;
        [SerializeField] private Vector3 normalVisualOffset = new Vector3(0f, -0.31f, -0.03f);
        [SerializeField] private Vector3 flippedVisualOffset = new Vector3(0f, 0.31f, -0.03f);
        [SerializeField] private int idleHoldFrameIndex = 4;

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
/// Unity create EchoVisual when called.
/// Cache here SpriteRenderer and Animator, disable Animator, load Ruby of idle/run/jump Sprite frame, and display the natural standing frame first.
/// Disable Animator is to prevent Animator Override script manually selected Sprite, otherwise Echo May display incorrect posture.
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
                animator.enabled = false;
            }

            idleFrames = LoadFrames(IdlePath);
            runFrames = LoadFrames(RunPath);
            jumpFrames = LoadFrames(JumpPath);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = echoTint;
            }

            SetState(VisualState.Idle);
            HoldIdleFrame();
        }

        /// <summary>
/// ActionRecorder create Echo is called when EchoVisual Position and scale aligned to current player PlayerVisual。
/// enter normalOffset is the local offset at normal gravity, visualScale yes PlayerVisual local scaling.
/// When anti-gravity, it will automatically Y Negate the offset and let it hang upside down Echo Still close to the corresponding platform surface.
        /// </summary>
/// <param name="normalOffset">Under normal gravity EchoVisual relatively EchoReplay The local offset of the root object. </param>
/// <param name="visualScale">EchoVisual The local scaling used, usually from the player PlayerVisual。</param>
        public void ConfigureVisualTransform(Vector3 normalOffset, Vector3 visualScale)
        {
            normalVisualOffset = normalOffset;
            flippedVisualOffset = new Vector3(normalOffset.x, -normalOffset.y, normalOffset.z);
            transform.localPosition = normalVisualOffset;
            transform.localScale = visualScale;
        }

        /// <summary>
/// Unity Called every frame.
/// only run and jump Need to advance animation normally; idle Fixed at the natural standing frame to avoid looping into unnatural transition poses when the clone is stationary.
        /// </summary>
        private void Update()
        {
            if (currentState == VisualState.Idle)
            {
                HoldIdleFrame();
                return;
            }

            AdvanceFrame();
        }

        /// <summary>
/// EchoReplayController Called every playback physics frame.
/// This function uses the position difference between the current frame and the previous frame to calculate Echo moving speed, select run、jump or idle, and synchronize the facing direction and anti-gravity upside down display.
/// enter frame is the current recording frame, previousFrame is the previous frame, finished express Echo Whether the last frame has been reached.
        /// </summary>
/// <param name="frame">current Echo Playback frame, including position, time, facing direction and gravity flip state. </param>
/// <param name="previousFrame">Previous frame Echo Data used to calculate movement speed. </param>
/// <param name="finished">for true time means Echo Playback has been completed, it should stop at the last position and display idle。</param>
        public void ApplyFrame(RecordingFrame frame, RecordingFrame previousFrame, bool finished)
        {
            float frameTime = Mathf.Max(0.0001f, frame.time - previousFrame.time);
            Vector2 velocity = ((Vector2)frame.position - (Vector2)previousFrame.position) / frameTime;

            if (finished)
            {
                SetState(VisualState.Idle);
                HoldIdleFrame();
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
                HoldIdleFrame();
            }

            if (spriteRenderer != null)
            {
                bool shouldFlip = !frame.facingRight;
                spriteRenderer.flipX = frame.isGravityFlipped ? !shouldFlip : shouldFlip;
                spriteRenderer.color = echoTint;
            }

            transform.localPosition = frame.isGravityFlipped ? flippedVisualOffset : normalVisualOffset;
            transform.localRotation = Quaternion.Euler(0f, 0f, frame.isGravityFlipped ? 180f : 0f);
            UpdateAnimatorParameters(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y) <= verticalJumpThreshold, velocity.y, frame.isGravityFlipped);
        }

        /// <summary>
/// switch Echo the current animation state and put currentFrames Point to the corresponding Ruby Array of sprite frames.
/// enter nextState is the target animation state; the function has no return value, and the result is directly reflected in SpriteRenderer superior.
        /// </summary>
/// <param name="nextState">The new animation state to switch to. </param>
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

            if (nextState == VisualState.Idle)
            {
                HoldIdleFrame();
            }
        }

        /// <summary>
/// Echo Fixed to the same natural standing frame as the player when idle.
/// so Echo It will not loop to when still or after playback is completed. idle Transition frames in a sequence that look bloated, suspended, or walking.
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
                return;
            }

            currentFrameIndex = targetFrameIndex;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }

        /// <summary>
/// Advance by the frame rate of the current animation state run or jump animation.
/// idle Don't loop through this because idle already by HoldIdleFrame Fixed to natural standing frame.
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
/// Bundle currentFrameIndex pointed Sprite apply to SpriteRenderer。
/// If the material is empty, skip it directly to avoid Echo Null reference error when generating.
        /// </summary>
        private void ApplyCurrentFrame()
        {
            if (spriteRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                spriteRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }

        /// <summary>
/// if EchoVisual reserved on Animator Controller, to synchronize speed, landing and anti-gravity parameters.
/// at present Animator It is disabled by default, and the main animation is controlled by this script's frame change; this function is reserved for re-enabling it in the future. Animator There is no need to rewrite the interface.
        /// </summary>
/// <param name="speed">Horizontal speed, used to set Animator of Speed parameter. </param>
/// <param name="isGrounded">Whether it is close to the landing state, used to set Animator of IsGrounded parameter. </param>
/// <param name="verticalVelocity">Vertical speed, used to set Animator of VerticalVelocity parameter. </param>
/// <param name="isGravityFlipped">Whether it is in anti-gravity state, used to set Animator of IsGravityFlipped parameter. </param>
        private void UpdateAnimatorParameters(float speed, bool isGrounded, float verticalVelocity, bool isGravityFlipped)
        {
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController == null)
            {
                return;
            }

            SetAnimatorFloat("Speed", speed);
            SetAnimatorBool("IsGrounded", isGrounded);
            SetAnimatorFloat("VerticalVelocity", verticalVelocity);
            SetAnimatorBool("IsGravityFlipped", isGravityFlipped);
        }

        /// <summary>
/// Security settings Animator float parameter. Set only when the parameter exists to avoid Animator Controller If there is no match, an error will occur.
        /// </summary>
/// <param name="parameterName">Animator Parameter name. </param>
/// <param name="value">to be written float value. </param>
        private void SetAnimatorFloat(string parameterName, float value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }

        /// <summary>
/// Security settings Animator bool parameter. Set only when the parameter exists to avoid Animator Controller If there is no match, an error will occur.
        /// </summary>
/// <param name="parameterName">Animator Parameter name. </param>
/// <param name="value">to be written bool value. </param>
        private void SetAnimatorBool(string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }

        /// <summary>
/// examine Animator Controller Whether to include specified parameters.
/// enter parameterName is the parameter name, type is the expected type; returns true Indicates that this parameter can be safely set.
        /// </summary>
/// <param name="parameterName">Animator Parameter name. </param>
/// <param name="type">expected Animator Parameter type. </param>
/// <returns>if Animator If there are parameters with the same name and the same type, return true; Otherwise return false。</returns>
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
/// from Resources Directory loads and sorts by name Ruby animation frames.
/// enter resourcePath yes Resources Path without extension; returns sorted Sprite Array, an empty array is returned when the resource does not exist.
        /// </summary>
/// <param name="resourcePath">Resources The resource path in the directory, excluding the file extension. </param>
/// <returns>sorted Sprite Array of animation frames. </returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
