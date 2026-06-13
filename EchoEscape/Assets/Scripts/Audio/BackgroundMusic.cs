using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：全局背景音乐播放器。主菜单和三个关卡都使用同一个背景音乐对象，避免每切一次场景就多生成一个 AudioSource。
    /// 玩法逻辑：当主菜单或关卡请求播放音乐时，如果全局对象不存在就创建并 DontDestroyOnLoad；如果同一首音乐已经播放中，就保持播放，不重头开始。
    /// 协作关系：MainMenuController 和 PrototypeAudio 调用 EnsurePlaying；PixelArtLibrary.LoadMusic 负责真正读取 mp3 音乐资源。
    /// </summary>
    public sealed class BackgroundMusic : MonoBehaviour
    {
        private const string DefaultTrackName = "time_for_adventure";
        private const float DefaultVolume = 0.22f;

        private static BackgroundMusic instance;

        private AudioSource source;
        /// <summary>
        /// 使用默认曲目和默认音量确认背景音乐正在播放。菜单和关卡都可以直接调用它。
        /// </summary>
        public static void EnsurePlaying()
        {
            EnsurePlaying(DefaultTrackName, DefaultVolume);
        }
        /// <summary>
        /// 确认指定背景音乐正在播放。第一次调用会创建全局播放器，之后切场景不会重复创建。
        /// </summary>
        /// <param name="trackName">音乐资源名，对应 Resources 里的背景音乐文件。</param>
        /// <param name="volume">播放音量，通常在 0 到 1 之间。</param>
        public static void EnsurePlaying(string trackName, float volume)
        {
            if (instance == null)
            {
                // 第一次进入菜单或关卡时创建音乐对象，并让它跨场景保留。
                GameObject musicObject = new GameObject("BackgroundMusic");
                instance = musicObject.AddComponent<BackgroundMusic>();
                DontDestroyOnLoad(musicObject);
            }

            // 后续场景只更新/确认播放，不会重复创建多个 AudioSource。
            instance.Play(trackName, volume);
        }
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                // 如果场景里又放了一个 BackgroundMusic，销毁新的，保证全局只有一个音乐播放器。
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            source = GetComponent<AudioSource>();
            if (source == null)
            {
                // 允许场景不手动挂 AudioSource，脚本会自动补齐。
                source = gameObject.AddComponent<AudioSource>();
            }

            source.loop = true;
            source.playOnAwake = false;
        }
        /// <summary>
        /// 实际播放指定音乐资源。如果同一首已经播放中，就保持连续播放不重头开始。
        /// </summary>
        /// <param name="trackName">音乐资源名，对应 Resources 里的背景音乐文件。</param>
        /// <param name="volume">播放音量，通常在 0 到 1 之间。</param>
        private void Play(string trackName, float volume)
        {
            if (source == null)
            {
                // 防止组件被误删时音乐系统失效。
                source = gameObject.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
            }

            AudioClip clip = PixelArtLibrary.LoadMusic(trackName);
            if (clip == null)
            {
                // 缺音乐资源只警告，不中断关卡运行。
                Debug.LogWarning("Background music clip missing: " + trackName);
                return;
            }

            source.volume = Mathf.Clamp01(volume);
            if (source.clip == clip && source.isPlaying)
            {
                // 同一首歌已经在播就不重头开始，切关时音乐能连续。
                return;
            }

            // 切换到新曲目或首次播放时才设置 clip 并播放。
            source.clip = clip;
            source.Play();
        }
    }
}
