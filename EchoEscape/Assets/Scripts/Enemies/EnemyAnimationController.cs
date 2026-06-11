using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Handles sprite-frame animation and facing for a simple enemy.
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

        /// <summary>
        /// True while the attack animation should keep occupying the enemy.
        /// </summary>
        public bool IsAttackAnimating => attackAnimationTimer > 0f;

        /// <summary>
        /// True when this enemy has usable death animation frames.
        /// </summary>
        public bool HasDeathAnimation => visualRenderer != null && deathFrames.Length > 0;

        /// <summary>
        /// Duration needed to play the death frames once.
        /// </summary>
        public float DeathDuration => deathFrames.Length / Mathf.Max(1f, deathFramesPerSecond);

        /// <summary>
        /// Initializes animation data from the serialized SimpleEnemy settings.
        /// </summary>
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
        /// Applies the enemy facing direction to the visual renderer.
        /// </summary>
        /// <param name="facingRight">True when the enemy faces right.</param>
        public void SetFacing(bool facingRight)
        {
            if (visualRenderer != null)
            {
                visualRenderer.flipX = spriteDefaultFacesRight ? !facingRight : facingRight;
            }
        }

        /// <summary>
        /// Starts or keeps the idle animation state.
        /// </summary>
        public void PlayIdle()
        {
            SetVisualState(VisualState.Idle);
        }

        /// <summary>
        /// Starts attack animation for at least the requested duration.
        /// </summary>
        /// <param name="minimumDuration">Minimum time the attack animation should stay visible.</param>
        public void PlayAttack(float minimumDuration)
        {
            if (attackFrames.Length == 0)
            {
                return;
            }

            float frameDuration = attackFrames.Length / Mathf.Max(1f, attackFramesPerSecond);
            attackAnimationTimer = Mathf.Max(attackAnimationDuration, Mathf.Max(minimumDuration, frameDuration));
            SetVisualState(VisualState.Attack);
        }

        /// <summary>
        /// Starts the death animation state.
        /// </summary>
        public void PlayDeath()
        {
            SetVisualState(VisualState.Death);
        }

        /// <summary>
        /// Advances the idle animation.
        /// </summary>
        public void TickIdle()
        {
            PlayIdle();
            AdvanceFrame(idleFramesPerSecond, true);
        }

        /// <summary>
        /// Advances the attack animation timer and frames.
        /// </summary>
        public void TickAttack()
        {
            if (attackAnimationTimer <= 0f)
            {
                return;
            }

            attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);
            SetVisualState(VisualState.Attack);
            AdvanceFrame(attackFramesPerSecond, false);
        }

        /// <summary>
        /// Advances the death animation.
        /// </summary>
        public void TickDeath()
        {
            SetVisualState(VisualState.Death);
            AdvanceFrame(deathFramesPerSecond, false);
        }

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

            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }

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
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }

        private void ApplyCurrentFrame()
        {
            if (visualRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                visualRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }

        private static Sprite[] LoadFrames(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return Array.Empty<Sprite>();
            }

            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
