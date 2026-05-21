using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Loads and plays prototype sound effects and background music.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Game Manager or another scene service object.
    /// EchoEscapeGameManager and gameplay scripts call its public methods for jump, chest, hurt, success, and UI tap feedback.
    /// </remarks>
    public class PrototypeAudio : MonoBehaviour
    {
        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioClip jump;
        private AudioClip coin;
        private AudioClip hurt;
        private AudioClip powerUp;
        private AudioClip tap;

        /// <summary>
        /// Unity event method called when the audio service is created.
        /// </summary>
        /// <remarks>
        /// Creates AudioSource components and loads clips from the BrackeysPlatformer Resources folders.
        /// </remarks>
        private void Awake()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.25f;

            jump = PixelArtLibrary.LoadSound("jump");
            coin = PixelArtLibrary.LoadSound("coin");
            hurt = PixelArtLibrary.LoadSound("hurt");
            powerUp = PixelArtLibrary.LoadSound("power_up");
            tap = PixelArtLibrary.LoadSound("tap");

            AudioClip music = PixelArtLibrary.LoadMusic("time_for_adventure");
            if (music != null)
            {
                musicSource.clip = music;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Plays the jump sound effect.
        /// </summary>
        public void PlayJump()
        {
            Play(jump, 0.8f);
        }

        /// <summary>
        /// Plays the chest or coin collection sound effect.
        /// </summary>
        public void PlayChest()
        {
            Play(coin, 0.9f);
        }

        /// <summary>
        /// Plays the hurt or death sound effect.
        /// </summary>
        public void PlayHurt()
        {
            Play(hurt, 0.9f);
        }

        /// <summary>
        /// Plays the success sound effect used when the player reaches the exit.
        /// </summary>
        public void PlaySuccess()
        {
            Play(powerUp, 0.9f);
        }

        /// <summary>
        /// Plays a short tap sound for recording or UI feedback.
        /// </summary>
        public void PlayTap()
        {
            Play(tap, 0.7f);
        }

        /// <summary>
        /// Safely plays a one-shot sound effect if the clip and audio source exist.
        /// </summary>
        /// <param name="clip">Audio clip to play.</param>
        /// <param name="volume">Playback volume for this sound.</param>
        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
