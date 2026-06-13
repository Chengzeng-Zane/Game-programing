using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：早期备用角色视觉脚本，用于给玩家或 Echo 快速生成一个像素角色 SpriteRenderer。现在主角更完整的动画由 PlayerAnimationController 负责，但这个脚本仍可作为旧场景或 Echo 视觉的 fallback。
    /// 玩法逻辑：脚本根据 Rigidbody2D 速度判断角色是在待机还是奔跑，并自动隐藏旧的火柴人线条视觉，保证角色显示更接近最终像素风格。
    /// 协作关系：PrototypeVisualSkinner 可以给角色自动补上它；PixelArtLibrary 提供待机和跑步帧；它只影响显示，不改变移动、攻击或碰撞。
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
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
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
        /// 设置这个备用视觉是玩家还是 Echo，并应用对应颜色。旧场景或 fallback 视觉会调用它。
        /// </summary>
        /// <param name="echoVisual">echoVisual 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="visualTint">visualTint 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void SetStyle(bool echoVisual, Color visualTint)
        {
            isEcho = echoVisual;
            tint = visualTint;
            // 切换成像素 Sprite 后隐藏旧的 LineRenderer 火柴人，避免两套视觉重叠。
            HideStickFigureLines();
            BuildSprite();
            ApplyStyle();
        }
        /// <summary>
        /// Unity 在 Update 之后调用。这里常用于相机或视觉同步，确保读到的是本帧最终状态。
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
                // 玩家本体使用 Rigidbody 速度更准确；Echo 这类 Kinematic 对象用位置差估算速度。
                visualVelocity = body.velocity;
            }

            bool moving = Mathf.Abs(visualVelocity.x) > 0.08f;
            if (Mathf.Abs(visualVelocity.x) > 0.05f)
            {
                // 根据水平速度翻转 Sprite，视觉朝向和移动方向一致。
                spriteRenderer.flipX = visualVelocity.x < 0f;
            }

            Sprite[] frames = moving ? PixelArtLibrary.KnightRunFrames : PixelArtLibrary.KnightIdleFrames;
            float framesPerSecond = moving ? runFramesPerSecond : idleFramesPerSecond;
            PlayAnimation(moving ? "run" : "idle", frames, framesPerSecond, deltaTime);

            lastPosition = position;
        }
        /// <summary>
        /// 组装一组运行时对象或 UI 元素，用来形成完整菜单、面板或视觉结构。
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
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
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
        /// 播放备用像素角色动画。key 变化时从第一帧重新开始，同一动画则按帧率循环。
        /// </summary>
        /// <param name="key">缓存 Sprite 时使用的唯一 key，避免重复切图。</param>
        /// <param name="frames">frames 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="framesPerSecond">动画播放速度，每秒显示多少帧。</param>
        /// <param name="deltaTime">当前帧经过的时间，用来推进动画计时。</param>
        private void PlayAnimation(string key, Sprite[] frames, float framesPerSecond, float deltaTime)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (animationKey != key)
            {
                // 动画状态切换时重置帧索引，避免从上一段动画的中间帧开始。
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
                // 循环播放 idle/run 帧，作为早期备用角色动画。
                animationTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
        /// </summary>
        private void HideStickFigureLines()
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                // 旧火柴人由 LineRenderer 组成，像素角色启用后它们只需要隐藏。
                lineRenderer.enabled = false;
            }
        }
    }
}
