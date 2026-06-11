using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Loops sprite frames for a tutorial question marker visual.
    /// </summary>
    public class AnimatedQuestionMarker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;

        private int currentFrameIndex;
        private float frameTimer;

        /// <summary>
        /// Finds the SpriteRenderer when it is not assigned.
        /// </summary>
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Advances the marker animation every frame.
        /// </summary>
        private void Update()
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
                spriteRenderer.sprite = frames[currentFrameIndex];
            }
        }
    }
}
