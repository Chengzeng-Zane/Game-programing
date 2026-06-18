using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Level Sound Effects Service. It is responsible for short sound effects, such as jumping, opening treasure chests, death from injury, successful level clearance and recording button prompts, but is not responsible for looping background music.
/// Gameplay logic: each level GameManager This service will be prepared, and other gameplay scripts only need to call PlayJump、PlayChest、PlayHurt Just wait for the function, you don’t need to manage it yourself AudioSource。
/// Collaborates with: EchoEscapeGameManager hold AudioService；PlayerController2D、Chest, death process, recording system and UI will use it indirectly; BackgroundMusic solely responsible BGM。
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
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
// Use this level for short sound effects GameManager on AudioSource, background music by BackgroundMusic Maintained individually.
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

// from PixelArtLibrary Sound effect resources are loaded uniformly, and other gameplay scripts do not need to care about the real file path.
            jump = PixelArtLibrary.LoadSound("jump");
            coin = PixelArtLibrary.LoadSound("coin");
            hurt = PixelArtLibrary.LoadSound("hurt");
            powerUp = PixelArtLibrary.LoadSound("power_up");
            tap = PixelArtLibrary.LoadSound("tap");

// Confirm by the way when the level sound effect service is initialized. BGM Playing, but not creating multiple music sources repeatedly.
            BackgroundMusic.EnsurePlaying();
        }
        /// <summary>
/// Plays the player jumping sound effect. PlayerController2D Called when jumping successfully.
        /// </summary>
        public void PlayJump()
        {
            Play(jump, 0.8f);
        }
        /// <summary>
/// Play to open treasure chest/Get bonus sound effects. Chest success to player pending loot when called.
        /// </summary>
        public void PlayChest()
        {
            Play(coin, 0.9f);
        }
        /// <summary>
/// Play injury or death sound effects. GameManager Called when entering the death process.
        /// </summary>
        public void PlayHurt()
        {
            Play(hurt, 0.9f);
        }
        /// <summary>
/// Play the sound effect of successful clearance. The player reaches the exit and completes loot Called during settlement.
        /// </summary>
        public void PlaySuccess()
        {
            Play(powerUp, 0.9f);
        }
        /// <summary>
/// Play a soft tone. Recording starts/Finish, Echo This is used by lightweight feedback such as playback.
        /// </summary>
        public void PlayTap()
        {
            Play(tap, 0.7f);
        }
        /// <summary>
/// Play a short sound effect. it is all PlayJump、PlayChest The underlying entrance shared by other functions.
        /// </summary>
/// <param name="clip">An audio resource to be played. </param>
/// <param name="volume">Playback volume, usually at 0 arrive 1 between. </param>
        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null)
            {
// PlayOneShot suitable for jumping/Short sound effects such as unboxing can be played overlapping with other short sound effects.
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
