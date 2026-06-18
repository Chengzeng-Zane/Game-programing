using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Early alternate character visual scripts to give players or Echo Quickly generate a pixel character SpriteRenderer. The main character is now more fully animated by PlayerAnimationController responsible, but this script can still be used as an old scene or Echo visual fallback。
/// Gameplay logic: script based Rigidbody2D The speed determines whether the character is idle or running, and automatically hides the old stickman line visual to ensure that the character display is closer to the final pixel style.
/// Collaborates with: PrototypeVisualSkinner It can be automatically added to the character; PixelArtLibrary Provides idle and running frames; it only affects display and does not change movement, attack, or collision.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PixelCharacterVisual : MonoBehaviour
    {
        private const string PlayerSpriteName = "Player Pixel Sprite";
        private const string EchoSpriteName = "Echo Pixel Sprite";
        public bool isEcho;
        public Color tint = Color.white;
        public float idleFramesPerSecond = 5f;
        public float runFramesPerSecond = 12f;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Vector2 lastPosition;
        private string animationKey = string.Empty;
        private float animationTimer;
        private int frameIndex;
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            HideStickFigureLines();
            BuildSprite();
            ApplyStyle();
            lastPosition = transform.position;
        }
        /// <summary>
/// Set this alternate vision to be the player or Echo, and apply the corresponding color. old scene or fallback Vision calls it.
        /// </summary>
/// <param name="echoVisual">echoVisual Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="visualTint">visualTint Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public void SetStyle(bool echoVisual, Color visualTint)
        {
            isEcho = echoVisual;
            tint = visualTint;
// switch to pixels Sprite Hide old after LineRenderer Stickman, avoid visual overlap between the two sets.
            HideStickFigureLines();
            BuildSprite();
            ApplyStyle();
        }
        /// <summary>
/// Unity exist Update Called afterwards. This is often used for camera or visual synchronization to ensure that the final state of this frame is read.
        /// </summary>
        private void LateUpdate()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector2 position = transform.position;
            Vector2 visualVelocity = (position - lastPosition) / deltaTime;

            if (body != null && body.bodyType != RigidbodyType2D.Kinematic)
            {
// Player body use Rigidbody Speed ​​is more accurate; Echo This type Kinematic Objects use position differences to estimate velocity.
                visualVelocity = body.velocity;
            }

            bool moving = Mathf.Abs(visualVelocity.x) > 0.08f;
            if (Mathf.Abs(visualVelocity.x) > 0.05f)
            {
// flip based on horizontal speed Sprite, the visual facing direction is consistent with the moving direction.
                bool isGravityFlipped = body != null && body.gravityScale < 0f;
                bool shouldFlip = visualVelocity.x < 0f;
                spriteRenderer.flipX = isGravityFlipped ? !shouldFlip : shouldFlip;
            }

            Sprite[] frames = moving ? PixelArtLibrary.KnightRunFrames : PixelArtLibrary.KnightIdleFrames;
            float framesPerSecond = moving ? runFramesPerSecond : idleFramesPerSecond;
            PlayAnimation(moving ? "run" : "idle", frames, framesPerSecond, deltaTime);

            lastPosition = position;
        }
        /// <summary>
/// Assemble a set of runtime objects or UI Elements used to form a complete menu, panel, or visual structure.
        /// </summary>
        private void BuildSprite()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            Transform existing = transform.Find(PlayerSpriteName);
            if (existing == null)
            {
                existing = transform.Find(EchoSpriteName);
            }

            GameObject spriteObject;
            if (existing != null)
            {
                spriteObject = existing.gameObject;
            }
            else
            {
                spriteObject = new GameObject(PlayerSpriteName);
                spriteObject.transform.SetParent(transform, false);
            }

            spriteObject.transform.localPosition = new Vector3(0f, -0.18f, -0.25f);
            spriteObject.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            }
        }
        /// <summary>
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
        private void ApplyStyle()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.gameObject.name = isEcho ? EchoSpriteName : PlayerSpriteName;
            spriteRenderer.sprite = PixelArtLibrary.KnightIdle;
            spriteRenderer.color = tint;
            spriteRenderer.sortingOrder = isEcho ? 7 : 8;
        }
        /// <summary>
/// Play alternate pixel character animation. key When changing, it restarts from the first frame, and the same animation loops according to the frame rate.
        /// </summary>
/// <param name="key">cache Sprite the only one used when key, to avoid repeated cutting. </param>
/// <param name="frames">frames Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="framesPerSecond">Animation playback speed, how many frames are displayed per second. </param>
/// <param name="deltaTime">The elapsed time of the current frame, used to advance animation timing. </param>
        private void PlayAnimation(string key, Sprite[] frames, float framesPerSecond, float deltaTime)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (animationKey != key)
            {
// Reset the frame index when the animation state switches to avoid starting from the middle frame of the previous animation.
                animationKey = key;
                frameIndex = 0;
                animationTimer = 0f;
                spriteRenderer.sprite = frames[frameIndex];
                return;
            }

            animationTimer += deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (animationTimer >= frameDuration)
            {
// Loop play idle/run Frames, as early backup character animations.
                animationTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }
        /// <summary>
/// Hide correspondence UI Or visual state, usually called when the prompt ends, the pop-up window is closed, or the process is cleaned up.
        /// </summary>
        private void HideStickFigureLines()
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
// old stickman made of LineRenderer Composed, they just need to be hidden after the pixel character is enabled.
                lineRenderer.enabled = false;
            }
        }
    }
}
