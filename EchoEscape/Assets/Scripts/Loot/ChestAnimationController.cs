using System;
using System.Collections;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Drives the Simple Chest sprite state without owning any loot logic.
    /// </summary>
    /// <remarks>
    /// Attach this script to the ChestVisual child object.
    /// Chest calls PlayOpenAnimation when the player opens the chest with F.
    /// This script only changes sprites; Chest and EchoEscapeGameManager still control loot.
    /// </remarks>
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

        /// <summary>
        /// True when this visual has a renderer and at least one loaded chest sprite.
        /// </summary>
        public bool HasVisual => spriteRenderer != null
            && (closedSprite != null || openedSprite != null || openingFrames.Length > 0);

        /// <summary>
        /// Description:
        /// Called when the visual object is created.
        /// It finds the SpriteRenderer, loads chest sprites from Resources, and shows the closed chest.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
        /// Description:
        /// Shows the closed chest sprite.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        public void ShowClosed()
        {
            if (spriteRenderer != null && closedSprite != null)
            {
                spriteRenderer.sprite = closedSprite;
            }
        }

        /// <summary>
        /// Description:
        /// Plays the chest opening animation once, then calls the optional finish callback.
        /// If no animation frames exist, it switches straight to the opened sprite.
        /// Inputs:
        /// onComplete - optional action called after the chest visual has opened
        /// Returns:
        /// void (no return)
        /// </summary>
        public void PlayOpenAnimation(Action onComplete = null)
        {
            if (spriteRenderer == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (hasPlayedOpenAnimation)
            {
                ShowOpened();
                onComplete?.Invoke();
                return;
            }

            if (isPlayingOpenAnimation)
            {
                return;
            }

            if (openingFrames.Length == 0 || !isActiveAndEnabled)
            {
                ShowOpened();
                hasPlayedOpenAnimation = true;
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(PlayOpenRoutine(onComplete));
        }

        /// <summary>
        /// Description:
        /// Steps through each opening frame over time.
        /// Inputs:
        /// onComplete - optional action called after the last frame
        /// Returns:
        /// IEnumerator - Unity coroutine steps for the animation
        /// </summary>
        private IEnumerator PlayOpenRoutine(Action onComplete)
        {
            isPlayingOpenAnimation = true;
            float frameDelay = 1f / Mathf.Max(1f, framesPerSecond);
            for (int i = 0; i < openingFrames.Length; i++)
            {
                spriteRenderer.sprite = openingFrames[i];
                yield return new WaitForSeconds(frameDelay);
            }

            isPlayingOpenAnimation = false;
            hasPlayedOpenAnimation = true;
            ShowOpened();
            onComplete?.Invoke();
        }

        /// <summary>
        /// Description:
        /// Shows the opened chest sprite or the last opening frame.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
        /// Description:
        /// Loads the closed, opened, and opening animation sprites from Resources.
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void LoadSprites()
        {
            closedSprite = Resources.Load<Sprite>(ClosedSpritePath);
            openedSprite = Resources.Load<Sprite>(OpenedSpritePath);
            openingFrames = LoadFrames(OpeningFramesPath);
        }

        /// <summary>
        /// Description:
        /// Loads and sorts all sprites from one Resources path.
        /// Inputs:
        /// resourcePath - path under Assets/Resources without file extension
        /// Returns:
        /// Sprite[] - sorted animation frames
        /// </summary>
        private static Sprite[] LoadFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            Array.Sort(frames, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return frames;
        }
    }
}
