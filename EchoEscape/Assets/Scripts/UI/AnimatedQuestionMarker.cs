using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Animation script for teaching question marks. It makes the question mark prompt card float slightly up and down, reminding the player that there is teaching content here.
/// Game logic: record the initial position of the question mark during runtime, and then use the sine function to Update medium change Y coordinate.
/// Collaboration: usually with TutorialPopupTrigger Hanging on the same question mark object.
    /// </summary>
    public class AnimatedQuestionMarker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;

        private int currentFrameIndex;
        private float frameTimer;
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
        /// <summary>
/// Unity Called every frame. This handles input, timers, UI Real-time refresh of state or non-physics.
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
