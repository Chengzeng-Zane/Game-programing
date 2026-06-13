using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：敌人动画组件，负责敌人的待机漂浮、攻击和死亡动画帧。
    /// 玩法逻辑：敌人行为被拆成移动、攻击、生命和动画几块；这个脚本只管理视觉状态，外部告诉它播放 Idle、Attack 或 Death，它再按帧率推进 Sprite。
    /// 协作关系：SimpleEnemy 初始化它；EnemyAttack 调用攻击动画；EnemyHealth 调用死亡动画。
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
        /// 接收外部脚本传入的参数，把当前组件配置成这个场景或这个敌人需要的状态。
        /// </summary>
        /// <param name="renderer">renderer 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="defaultFacesRight">defaultFacesRight 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingRight">facingRight 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="idleFramesPath">idleFramesPath 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="fallbackIdleFramesPath">fallbackIdleFramesPath 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="attackFramesPath">attackFramesPath 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="deathFramesPath">deathFramesPath 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="idleFps">idleFps 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="attackFps">attackFps 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="deathFps">deathFps 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="attackDuration">attackDuration 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
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
        /// 设置敌人面朝方向。不同素材默认朝向可能不同，所以用 spriteDefaultFacesRight 做一次转换。
        /// </summary>
        /// <param name="facingRight">facingRight 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void SetFacing(bool facingRight)
        {
            if (visualRenderer != null)
            {
                // 如果素材默认朝右，面朝右时不翻转；如果素材默认朝左，逻辑要反过来。
                visualRenderer.flipX = spriteDefaultFacesRight ? !facingRight : facingRight;
            }
        }
        /// <summary>
        /// 切回敌人待机/漂浮动画。没有追击、攻击或死亡时使用。
        /// </summary>
        public void PlayIdle()
        {
            SetVisualState(VisualState.Idle);
        }
        /// <summary>
        /// 播放敌人攻击动画，并保证至少覆盖攻击前摇和攻击框有效时间。
        /// </summary>
        /// <param name="minimumDuration">minimumDuration 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void PlayAttack(float minimumDuration)
        {
            if (attackFrames.Length == 0)
            {
                // 没有攻击帧时不报错，敌人攻击逻辑仍然可以继续运行。
                return;
            }

            float frameDuration = attackFrames.Length / Mathf.Max(1f, attackFramesPerSecond);
            // 取三者最大值，避免攻击框还有效时动画已经切回 idle。
            attackAnimationTimer = Mathf.Max(attackAnimationDuration, Mathf.Max(minimumDuration, frameDuration));
            SetVisualState(VisualState.Attack);
        }
        /// <summary>
        /// 播放敌人死亡动画。真正关闭对象的计时由 EnemyHealth 控制。
        /// </summary>
        public void PlayDeath()
        {
            SetVisualState(VisualState.Death);
        }
        /// <summary>
        /// 推进敌人待机动画。SimpleEnemy 在敌人没有攻击、死亡或追击特殊状态时调用。
        /// </summary>
        public void TickIdle()
        {
            PlayIdle();
            AdvanceFrame(idleFramesPerSecond, true);
        }
        /// <summary>
        /// 推进敌人攻击动画，并倒计时攻击动画剩余时间。
        /// </summary>
        public void TickAttack()
        {
            if (attackAnimationTimer <= 0f)
            {
                // 攻击动画时间结束后，让 SimpleEnemy 下一帧可以回到移动/待机逻辑。
                return;
            }

            attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);
            SetVisualState(VisualState.Attack);
            AdvanceFrame(attackFramesPerSecond, false);
        }
        /// <summary>
        /// 推进敌人死亡动画。死亡动画不循环，最后由 EnemyHealth 关闭对象。
        /// </summary>
        public void TickDeath()
        {
            SetVisualState(VisualState.Death);
            AdvanceFrame(deathFramesPerSecond, false);
        }
        /// <summary>
        /// 切换敌人当前视觉状态，并选择对应的帧数组。
        /// </summary>
        /// <param name="nextState">要切换到的新动画或逻辑状态。</param>
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

            // 每次切换状态都从第一帧开始播，避免从上一个状态的帧索引继承导致错帧。
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }
        /// <summary>
        /// 推进动画帧或流程计时，让动画按帧率继续播放。
        /// </summary>
        /// <param name="framesPerSecond">动画播放速度，每秒显示多少帧。</param>
        /// <param name="loop">true 表示动画循环播放，false 表示播到最后一帧停住。</param>
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
                // while 可以在低帧率时补帧，保证动画速度接近设定 FPS。
                frameTimer -= frameDuration;
                currentFrameIndex = loop
                    ? (currentFrameIndex + 1) % currentFrames.Length
                    : Mathf.Min(currentFrameIndex + 1, currentFrames.Length - 1);
                ApplyCurrentFrame();
            }
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        private void ApplyCurrentFrame()
        {
            if (visualRenderer != null && currentFrames != null && currentFrames.Length > 0)
            {
                visualRenderer.sprite = currentFrames[Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1)];
            }
        }
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <param name="resourcePath">Resources 目录下的资源路径，不包含扩展名。</param>
        /// <returns>返回一组 Sprite 动画帧；资源不存在时可能是空数组。</returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return Array.Empty<Sprite>();
            }

            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            // 统一按帧名排序，避免 Resources.LoadAll 顺序导致动画播放错乱。
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
