using UnityEngine;

namespace EchoEscape
{
    public class PrototypeAudio : MonoBehaviour
    {
        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioClip jump;
        private AudioClip coin;
        private AudioClip hurt;
        private AudioClip powerUp;
        private AudioClip tap;

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

        public void PlayJump()
        {
            Play(jump, 0.8f);
        }

        public void PlayChest()
        {
            Play(coin, 0.9f);
        }

        public void PlayHurt()
        {
            Play(hurt, 0.9f);
        }

        public void PlaySuccess()
        {
            Play(powerUp, 0.9f);
        }

        public void PlayTap()
        {
            Play(tap, 0.7f);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
