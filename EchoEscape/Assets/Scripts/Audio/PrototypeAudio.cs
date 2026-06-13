using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：关卡音效服务。它负责短音效，例如跳跃、开宝箱、受伤死亡、通关成功和录制按钮提示，不负责循环背景音乐。
    /// 玩法逻辑：每个关卡的 GameManager 会准备这个服务，其他玩法脚本只需要调用 PlayJump、PlayChest、PlayHurt 等函数即可，不需要自己管理 AudioSource。
    /// 协作关系：EchoEscapeGameManager 持有 AudioService；PlayerController2D、Chest、死亡流程、录制系统和 UI 会间接使用它；BackgroundMusic 单独负责 BGM。
    /// </summary>
    public class PrototypeAudio : MonoBehaviour
    {
        private AudioSource sfxSource;
        private AudioClip jump;
        private AudioClip coin;
        private AudioClip hurt;
        private AudioClip powerUp;
        private AudioClip tap;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            // 短音效使用本关 GameManager 上的 AudioSource，背景音乐由 BackgroundMusic 单独维护。
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            // 从 PixelArtLibrary 统一加载音效资源，其他玩法脚本不用关心真实文件路径。
            jump = PixelArtLibrary.LoadSound("jump");
            coin = PixelArtLibrary.LoadSound("coin");
            hurt = PixelArtLibrary.LoadSound("hurt");
            powerUp = PixelArtLibrary.LoadSound("power_up");
            tap = PixelArtLibrary.LoadSound("tap");

            // 关卡音效服务初始化时顺便确认 BGM 正在播放，但不会重复创建多个音乐源。
            BackgroundMusic.EnsurePlaying();
        }
        /// <summary>
        /// 播放玩家跳跃音效。PlayerController2D 成功跳起时调用。
        /// </summary>
        public void PlayJump()
        {
            Play(jump, 0.8f);
        }
        /// <summary>
        /// 播放开宝箱/获得奖励音效。Chest 成功给玩家 pending loot 时调用。
        /// </summary>
        public void PlayChest()
        {
            Play(coin, 0.9f);
        }
        /// <summary>
        /// 播放受伤或死亡音效。GameManager 进入死亡流程时调用。
        /// </summary>
        public void PlayHurt()
        {
            Play(hurt, 0.9f);
        }
        /// <summary>
        /// 播放通关成功音效。玩家到达出口并完成 loot 结算时调用。
        /// </summary>
        public void PlaySuccess()
        {
            Play(powerUp, 0.9f);
        }
        /// <summary>
        /// 播放轻提示音。录制开始/结束、Echo 播放等轻量反馈会使用它。
        /// </summary>
        public void PlayTap()
        {
            Play(tap, 0.7f);
        }
        /// <summary>
        /// 播放一个短音效。它是所有 PlayJump、PlayChest 等函数共用的底层入口。
        /// </summary>
        /// <param name="clip">要播放的一段音频资源。</param>
        /// <param name="volume">播放音量，通常在 0 到 1 之间。</param>
        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null)
            {
                // PlayOneShot 适合跳跃/开箱这类短音效，可以和其他短音效重叠播放。
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
