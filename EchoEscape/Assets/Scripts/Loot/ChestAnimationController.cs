using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：宝箱动画控制器。负责宝箱关闭状态、打开过程和打开完成后的 Sprite 显示。
    /// 玩法逻辑：宝箱打开不是瞬间换图，而是播放一组帧；动画结束后通过回调通知 Chest 继续结算 loot。
    /// 协作关系：Chest 调用 PlayOpenAnimation，动画完成后回调 Chest.FinishOpening。
    /// </summary>
    public class ChestAnimationController : MonoBehaviour
    {
        private const string ClosedSpritePath = "Ancient Forest 1.6/Animated Tiles/Simple Chest/simple_chest_idle";
        private const string OpenedSpritePath = "Ancient Forest 1.6/Animated Tiles/Simple Chest/simple_chest_open";
        private const string OpeningFramesPath = "Ancient Forest 1.6/Animated Tiles/Simple Chest/simple_chest_opening-Sheet";

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private float framesPerSecond = 12f;

        private Sprite closedSprite;
        private Sprite openedSprite;
        private Sprite[] openingFrames = Array.Empty<Sprite>();
        private bool isPlayingOpenAnimation;
        private bool hasPlayedOpenAnimation;
        public bool HasVisual => spriteRenderer != null
            && (closedSprite != null || openedSprite != null || openingFrames.Length > 0);
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            LoadSprites();
            ShowClosed();
        }
        /// <summary>
        /// 显示宝箱关闭状态。关卡开始或宝箱初始化时调用。
        /// </summary>
        public void ShowClosed()
        {
            if (spriteRenderer != null && closedSprite != null)
            {
                spriteRenderer.sprite = closedSprite;
            }
        }
        /// <summary>
        /// 播放宝箱打开动画。动画结束后调用 onComplete，让 Chest 知道可以结束开箱状态。
        /// </summary>
        /// <param name="onComplete">onComplete 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void PlayOpenAnimation(Action onComplete = null)
        {
            if (spriteRenderer == null)
            {
                // 没有视觉组件时直接完成回调，不影响 loot 结算。
                onComplete?.Invoke();
                return;
            }

            if (hasPlayedOpenAnimation)
            {
                // 已经播过开箱动画时直接显示打开状态，避免重复播放。
                ShowOpened();
                onComplete?.Invoke();
                return;
            }

            if (isPlayingOpenAnimation)
            {
                // 正在播放时忽略重复调用，防止多个协程同时改 Sprite。
                return;
            }

            if (openingFrames.Length == 0 || !isActiveAndEnabled)
            {
                // 没有 opening 帧或组件未启用时直接切到打开图，流程仍然继续。
                ShowOpened();
                hasPlayedOpenAnimation = true;
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(PlayOpenRoutine(onComplete));
        }
        /// <summary>
        /// 宝箱开启动画协程。它按 framesPerSecond 逐帧切换 Sprite，最后显示打开状态。
        /// </summary>
        /// <param name="onComplete">onComplete 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator PlayOpenRoutine(Action onComplete)
        {
            isPlayingOpenAnimation = true;
            float frameDelay = 1f / Mathf.Max(1f, framesPerSecond);
            for (int i = 0; i < openingFrames.Length; i++)
            {
                // 每一帧显示一张宝箱 opening sprite，形成开盖动画。
                spriteRenderer.sprite = openingFrames[i];
                yield return new WaitForSeconds(frameDelay);
            }

            // 动画结束后统一标记为已播放，再通知 Chest 收尾。
            isPlayingOpenAnimation = false;
            hasPlayedOpenAnimation = true;
            ShowOpened();
            onComplete?.Invoke();
        }
        /// <summary>
        /// 显示宝箱已经打开的状态。优先用 openedSprite，没有就用 openingFrames 最后一帧。
        /// </summary>
        private void ShowOpened()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (openedSprite != null)
            {
                spriteRenderer.sprite = openedSprite;
                return;
            }

            if (openingFrames.Length > 0)
            {
                spriteRenderer.sprite = openingFrames[openingFrames.Length - 1];
            }
        }
        /// <summary>
        /// 从 Resources 加载宝箱关闭图、打开图和开启动画帧。
        /// </summary>
        private void LoadSprites()
        {
            closedSprite = Resources.Load<Sprite>(ClosedSpritePath);
            openedSprite = Resources.Load<Sprite>(OpenedSpritePath);
            openingFrames = LoadFrames(OpeningFramesPath);
        }
        /// <summary>
        /// 从 Resources 加载并排序一组 Sprite 动画帧。
        /// </summary>
        /// <param name="resourcePath">Resources 目录下的资源路径，不包含扩展名。</param>
        /// <returns>返回一组 Sprite 动画帧；资源不存在时可能是空数组。</returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            // Resources.LoadAll 不保证顺序，排序后开箱动画才会按帧名播放。
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
