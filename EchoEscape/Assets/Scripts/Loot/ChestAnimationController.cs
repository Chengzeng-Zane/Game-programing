using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Treasure Chest Animation Controller. Responsible for the closed status, opening process and completion of the treasure chest Sprite show.
/// Gameplay logic: When the treasure chest is opened, the picture is not changed instantly, but a set of frames is played; after the animation ends, the callback notification is used Chest Continue settlement loot。
/// Collaborates with: Chest call PlayOpenAnimation, callback after the animation is completed Chest. FinishOpening。
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
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
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
/// Shows the treasure chest's closed status. Called when a level starts or when a treasure chest is initialized.
        /// </summary>
        public void ShowClosed()
        {
            if (spriteRenderer != null && closedSprite != null)
            {
                spriteRenderer.sprite = closedSprite;
            }
        }
        /// <summary>
/// Play the treasure chest opening animation. Called after animation ends onComplete, let Chest Know that you can end the unboxing state.
        /// </summary>
/// <param name="onComplete">onComplete Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public void PlayOpenAnimation(Action onComplete = null)
        {
            if (spriteRenderer == null)
            {
// When there is no visual component, the callback is completed directly and does not affect loot Settlement.
                onComplete?.Invoke();
                return;
            }

            if (hasPlayedOpenAnimation)
            {
// When the unboxing animation has been played, the open status is directly displayed to avoid repeated playback.
                ShowOpened();
                onComplete?.Invoke();
                return;
            }

            if (isPlayingOpenAnimation)
            {
// Ignore repeated calls while playing to prevent multiple coroutines from changing at the same time Sprite。
                return;
            }

            if (openingFrames.Length == 0 || !isActiveAndEnabled)
            {
// No opening When the frame or component is not enabled, switch directly to the open diagram and the process will still continue.
                ShowOpened();
                hasPlayedOpenAnimation = true;
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(PlayOpenRoutine(onComplete));
        }
        /// <summary>
/// The treasure chest opens the animation coroutine. it presses framesPerSecond Switch frame by frame Sprite, and finally displays the open status.
        /// </summary>
/// <param name="onComplete">onComplete Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator PlayOpenRoutine(Action onComplete)
        {
            isPlayingOpenAnimation = true;
            float frameDelay = 1f / Mathf.Max(1f, framesPerSecond);
            for (int i = 0; i < openingFrames.Length; i++)
            {
// Display a treasure chest every frame opening sprite, forming a cover opening animation.
                spriteRenderer.sprite = openingFrames[i];
                yield return new WaitForSeconds(frameDelay);
            }

// After the animation ends, it will be marked as played and then notified. Chest ending.
            isPlayingOpenAnimation = false;
            hasPlayedOpenAnimation = true;
            ShowOpened();
            onComplete?.Invoke();
        }
        /// <summary>
/// Shows that the treasure chest has been opened. priority openedSprite, use it if you don’t have it openingFrames Last frame.
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
/// from Resources Load the treasure chest closing image, opening image and opening animation frame.
        /// </summary>
        private void LoadSprites()
        {
            closedSprite = Resources.Load<Sprite>(ClosedSpritePath);
            openedSprite = Resources.Load<Sprite>(OpenedSpritePath);
            openingFrames = LoadFrames(OpeningFramesPath);
        }
        /// <summary>
/// from Resources Load and sort a group Sprite Animation frames.
        /// </summary>
/// <param name="resourcePath">Resources The resource path in the directory, excluding the extension. </param>
/// <returns>Return a set Sprite Animation frames; may be an empty array if the resource does not exist. </returns>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
// Resources. LoadAll The order is not guaranteed, the unboxing animation will be played by frame name only after sorting.
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
