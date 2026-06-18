using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Enemy animation component, responsible for the enemy's standby floating, attack and death animation frames.
/// Gameplay logic: Enemy behavior is split into movement, attack, life and animation; this script only manages the visual state, and the outside tells it to play Idle、Attack or Death, it then advances according to the frame rate Sprite。
/// Collaborates with: EnemyController initialize it; EnemyAttack Call attack animation; EnemyHealth Call the death animation.
    /// </summary>
    public class EnemyAnimationController : MonoBehaviour
    {
        private enum VisualState
        {
            Idle,
            Attack,
            Death
        }

        private SpriteRenderer visualRenderer;
        private bool spriteDefaultFacesRight;
        private Sprite[] idleFrames = Array.Empty<Sprite>();
        private Sprite[] attackFrames = Array.Empty<Sprite>();
        private Sprite[] deathFrames = Array.Empty<Sprite>();
        private Sprite[] currentFrames = Array.Empty<Sprite>();
        private int currentFrameIndex;
        private float frameTimer;
        private float idleFramesPerSecond = 8f;
        private float attackFramesPerSecond = 12f;
        private float deathFramesPerSecond = 10f;
        private float attackAnimationDuration = 0.35f;
        private float attackAnimationTimer;
        private VisualState visualState = VisualState.Idle;
        public bool IsAttackAnimating => attackAnimationTimer > 0f;
        public bool HasDeathAnimation => visualRenderer != null && deathFrames.Length > 0;
        public float DeathDuration => deathFrames.Length / Mathf.Max(1f, deathFramesPerSecond);
        /// <summary>
/// Receive the parameters passed in by the external script and configure the current component to the state required by this scene or this enemy.
        /// </summary>
/// <param name="renderer">renderer Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="defaultFacesRight">defaultFacesRight Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingRight">facingRight Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="idleFramesPath">idleFramesPath Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="fallbackIdleFramesPath">fallbackIdleFramesPath Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="attackFramesPath">attackFramesPath Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="deathFramesPath">deathFramesPath Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="idleFps">idleFps Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="attackFps">attackFps Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="deathFps">deathFps Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="attackDuration">attackDuration Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public void Configure(
            SpriteRenderer renderer,
            bool defaultFacesRight,
            bool facingRight,
            string idleFramesPath,
            string fallbackIdleFramesPath,
            string attackFramesPath,
            string deathFramesPath,
            float idleFps,
            float attackFps,
            float deathFps,
            float attackDuration)
        {
            visualRenderer = renderer;
            spriteDefaultFacesRight = defaultFacesRight;
            idleFramesPerSecond = idleFps;
            attackFramesPerSecond = attackFps;
            deathFramesPerSecond = deathFps;
            attackAnimationDuration = attackDuration;

            idleFrames = LoadFrames(idleFramesPath);
            if (idleFrames.Length == 0)
            {
                idleFrames = LoadFrames(fallbackIdleFramesPath);
            }

            attackFrames = LoadFrames(attackFramesPath);
            deathFrames = LoadFrames(deathFramesPath);
            SetFacing(facingRight);
            PlayIdle();
        }
        /// <summary>
/// Sets the direction the enemy is facing. The default facing direction of different materials may be different, so use spriteDefaultFacesRight Make a conversion.
        /// </summary>
/// <param name="facingRight">facingRight Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public void SetFacing(bool facingRight)
        {
            if (visualRenderer != null)
            {
// If the material is facing right by default, it will not be flipped when facing right; if the material is facing left by default, the logic should be reversed.
                visualRenderer.flipX = spriteDefaultFacesRight ? !facingRight : facingRight;
            }
        }
        /// <summary>
/// Switch back to enemy standby/Floating animation. Used when not pursuing, attacking or dying.
        /// </summary>
        public void PlayIdle()
        {
            SetVisualState(VisualState.Idle);
        }
        /// <summary>
/// Play the enemy's attack animation and ensure that it at least covers the attack forward swing and attack frame effective time.
        /// </summary>
/// <param name="minimumDuration">minimumDuration Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public void PlayAttack(float minimumDuration)
        {
            if (attackFrames.Length == 0)
            {
// No error will be reported when there is no attack frame, and the enemy attack logic can still continue to run.
                return;
            }

            float frameDuration = attackFrames.Length / Mathf.Max(1f, attackFramesPerSecond);
// Take the maximum value of the three to prevent the animation from switching back when the attack frame is still valid. idle。
            attackAnimationTimer = Mathf.Max(attackAnimationDuration, Mathf.Max(minimumDuration, frameDuration));
            SetVisualState(VisualState.Attack);
        }
        /// <summary>
/// Play the enemy death animation. The timing of actually closing the object is given by EnemyHealth control.
        /// </summary>
        public void PlayDeath()
        {
            SetVisualState(VisualState.Death);
        }
        /// <summary>
/// Advance enemy standby animation. EnemyController Called when the enemy does not have attack, death or pursuit special status.
        /// </summary>
        public void TickIdle()
        {
            PlayIdle();
            AdvanceFrame(idleFramesPerSecond, true);
        }
        /// <summary>
/// Advances the enemy's attack animation and counts down the remaining time of the attack animation.
        /// </summary>
        public void TickAttack()
        {
            if (attackAnimationTimer <= 0f)
            {
// After the attack animation time ends, let EnemyController Next frame you can go back to moving/Standby logic.
                return;
            }

            attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);
            SetVisualState(VisualState.Attack);
            AdvanceFrame(attackFramesPerSecond, false);
        }
        /// <summary>
/// Advance enemy death animation. The death animation does not loop and ends with EnemyHealth Close the object.
        /// </summary>
        public void TickDeath()
        {
            SetVisualState(VisualState.Death);
            AdvanceFrame(deathFramesPerSecond, false);
        }
        /// <summary>
/// Switch the enemy's current visual state and select the corresponding frame array.
        /// </summary>
/// <param name="nextState">The new animation or logic state to switch to. </param>
        private void SetVisualState(VisualState nextState)
        {
            if (visualState == nextState && currentFrames != null && currentFrames.Length > 0)
            {
                return;
            }

            visualState = nextState;
            currentFrames = nextState switch
            {
                VisualState.Attack => attackFrames.Length > 0 ? attackFrames : idleFrames,
                VisualState.Death => deathFrames.Length > 0 ? deathFrames : idleFrames,
                _ => idleFrames
            };

// Each time the state is switched, the broadcast starts from the first frame to avoid frame errors caused by inheritance from the frame index of the previous state.
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        /// <summary>
/// Advance animation frames or process timing so the animation continues to play at the frame rate.
        /// </summary>
/// <param name="framesPerSecond">Animation playback speed, how many frames are displayed per second. </param>
/// <param name="loop">true Indicates that the animation plays in a loop, false Indicates that the playback will stop at the last frame. </param>
        private void AdvanceFrame(float framesPerSecond, bool loop)
        {
            if (visualRenderer == null || currentFrames == null || currentFrames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
// while Frames can be supplemented when the frame rate is low to ensure that the animation speed is close to the setting FPS。
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }
        /// <summary>
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
        private void ApplyCurrentFrame()
        {
            if (visualRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                visualRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }
        /// <summary>
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <param name="resourcePath">Resources The resource path in the directory, excluding the extension. </param>
/// <returns>Return a set Sprite Animation frames; may be an empty array if the resource does not exist. </returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return Array.Empty<Sprite>();
            }

            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
// Sort by frame name uniformly to avoid Resources. LoadAll The order causes the animation to play out of order.
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
