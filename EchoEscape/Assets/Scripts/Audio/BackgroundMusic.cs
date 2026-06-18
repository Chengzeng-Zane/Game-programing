using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Global Background Music Player. The main menu and the three levels all use the same background music object to avoid generating an extra one every time the scene is cut. AudioSource。
/// Gameplay logic: When the main menu or level requests to play music, if the global object does not exist, create it and DontDestroyOnLoad; If the same piece of music is already playing, keep playing without starting over.
/// Collaborates with: MainMenuController and PrototypeAudio call EnsurePlaying；PixelArtLibrary. LoadMusic Responsible for actually reading mp3 Music resources.
    /// </summary>
    public sealed class BackgroundMusic : MonoBehaviour
    {
        private const string DefaultTrackName = "dark_forest";
        private const float DefaultVolume = 0.28f;

        private static BackgroundMusic instance;

        private AudioSource source;
        /// <summary>
/// Confirm that background music is playing using the default track and default volume. It can be called directly from menus and levels.
        /// </summary>
        public static void EnsurePlaying()
        {
            EnsurePlaying(DefaultTrackName, DefaultVolume);
        }
        /// <summary>
/// Confirm that the specified background music is playing. The first call will create a global player, and subsequent scenes will not be re-created.
        /// </summary>
/// <param name="trackName">Music resource name, corresponding to Resources background music files. </param>
/// <param name="volume">Playback volume, usually at 0 arrive 1 between. </param>
        public static void EnsurePlaying(string trackName, float volume)
        {
            if (instance == null)
            {
// Create a music object when you first enter a menu or level, and have it persist across scenes.
                GameObject musicObject = new GameObject("BackgroundMusic");
                instance = musicObject.AddComponent<BackgroundMusic>();
                DontDestroyOnLoad(musicObject);
            }

// Subsequent scenes will only be updated/Confirm playback and will not create multiple duplicates AudioSource。
            instance.Play(trackName, volume);
        }
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
// If there is another one in the scene BackgroundMusic, destroy the new one, ensuring that there is only one music player globally.
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            source = GetComponent<AudioSource>();
            if (source == null)
            {
// Allow scenes to be hung manually AudioSource, the script will automatically complete it.
                source = gameObject.AddComponent<AudioSource>();
            }

            source.loop = true;
            source.playOnAwake = false;
        }
        /// <summary>
/// Actually play the specified music resource. If the same song is already being played, the continuous playback will be continued without starting over.
        /// </summary>
/// <param name="trackName">Music resource name, corresponding to Resources background music files. </param>
/// <param name="volume">Playback volume, usually at 0 arrive 1 between. </param>
        private void Play(string trackName, float volume)
        {
            if (source == null)
            {
// Prevent the music system from failing when components are accidentally deleted.
                source = gameObject.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
            }

            AudioClip clip = PixelArtLibrary.LoadMusic(trackName);
            if (clip == null)
            {
// The lack of music resources only gives a warning and does not interrupt the level running.
                Debug.LogWarning("Background music clip missing: " + trackName);
                return;
            }

            source.volume = Mathf.Clamp01(volume);
            if (source.clip == clip && source.isPlaying)
            {
// The same song will not start over when it is already playing, and the music will continue when switching off.
                return;
            }

// Set when switching to a new track or playing it for the first time clip and play.
            source.clip = clip;
            source.Play();
        }
    }
}
