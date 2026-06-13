using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：教学问号的动画脚本。它让问号提示牌轻微上下浮动，提示玩家这里有教学内容。
    /// 玩法逻辑：运行时记录问号初始位置，然后用正弦函数在 Update 中改变 Y 坐标。
    /// 协作关系：通常和 TutorialPopupTrigger 挂在同一个问号物体上。
    /// </summary>
    public class AnimatedQuestionMarker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;

        private int currentFrameIndex;
        private float frameTimer;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
        /// <summary>
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
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
