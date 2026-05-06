using UnityEngine;

namespace EchoVault
{
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

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            HideStickFigureLines();
            BuildSprite();
            ApplyStyle();
            lastPosition = transform.position;
        }

        public void SetStyle(bool echoVisual, Color visualTint)
        {
            isEcho = echoVisual;
            tint = visualTint;
            HideStickFigureLines();
            BuildSprite();
            ApplyStyle();
        }

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
                visualVelocity = body.velocity;
            }

            bool moving = Mathf.Abs(visualVelocity.x) > 0.08f;
            if (Mathf.Abs(visualVelocity.x) > 0.05f)
            {
                spriteRenderer.flipX = visualVelocity.x < 0f;
            }

            Sprite[] frames = moving ? PixelArtLibrary.KnightRunFrames : PixelArtLibrary.KnightIdleFrames;
            float framesPerSecond = moving ? runFramesPerSecond : idleFramesPerSecond;
            PlayAnimation(moving ? "run" : "idle", frames, framesPerSecond, deltaTime);

            lastPosition = position;
        }

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

        private void PlayAnimation(string key, Sprite[] frames, float framesPerSecond, float deltaTime)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (animationKey != key)
            {
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
                animationTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }

        private void HideStickFigureLines()
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
