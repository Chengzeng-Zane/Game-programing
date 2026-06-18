using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Pressure Plate Mechanism Script. player or Echo Standing on it will press a button, which is often used to open doors or dispel magic barriers.
/// Gameplay logic: The script maintains a pressure on the board Collider list, as long as there are still valid players in the list or Echo, the button is pressed; Updates door, button animation and color feedback when pressed. Echo After the replay ends, stop on the pressure plate and it will continue to open the door for the player.
/// Collaborates with: ActionRecorder/EchoReplayController generate Echo；Door Receives pressure plate switch status.
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        public Door linkedDoor;
        public bool enableDebugLogs = true;
        [SerializeField] private bool useLayeredVisualColors;
        [SerializeField] private Transform layeredVisualRoot;
        [SerializeField] private float pressedVisualBoost = 1.35f;
        [SerializeField] private Animator buttonVisualAnimator;
        [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.05f, 0f);
        public bool IsPressed => occupants.Count > 0;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private readonly Dictionary<SpriteRenderer, Color> layeredVisualBaseColors = new Dictionary<SpriteRenderer, Color>();
        private Transform pressedMotionRoot;
        private Vector3 restingPressedMotionLocalPosition;
        /// <summary>
/// Initialize the pressure plate component and initial visual state to ensure that the door and button states are consistent at the beginning.
        /// </summary>
        private void Awake()
        {
// First find the movable button visual root node, and then press/When released, only the vision moves, not the reality Collider。
            pressedMotionRoot = ResolvePressedMotionRoot();
            if (pressedMotionRoot != null)
            {
                restingPressedMotionLocalPosition = pressedMotionRoot.localPosition;
            }

// The original color is cached, and the original color can be accurately restored after pressing to brighten.
            CacheLayeredVisualColors();
            Refresh();
        }
        /// <summary>
/// player or Echo Adds occupancy list when entering pressure plate and refreshes button and door state.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants.Add(other);
            LogOccupant(other);
            Refresh();
        }

        /// <summary>
/// Continuously confirm player or Echo Are you still standing on the pressure plate.
/// if Echo Already overlapped with the button when generated, Enter Events may be unstable; Stay You can add it back to the occupied list.
        /// </summary>
/// <param name="other">Currently staying in the pressure plate trigger zone Collider。</param>
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!CanPress(other) || occupants.Contains(other))
            {
                return;
            }

            occupants.Add(other);
            LogOccupant(other);
            Refresh();
        }

        /// <summary>
/// player or Echo When leaving the pressure plate, it is removed from the occupied list and re-judges whether there are still objects holding it down.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants.Remove(other);
            Refresh();
        }
        /// <summary>
/// judge a Collider Is it possible to press the pressure plate. generally Player and Echo Yes, normal props or enemies will not trigger.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool CanPress(Collider2D other)
        {
// Player and Echo All can suppress the mechanism; Echo It is used for solving puzzles, so you cannot just judge Player。
            return HasTag(other, "Player") ||
                HasTag(other, "Echo") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }
        /// <summary>
/// Calculate whether a button is pressed based on the current occupancy list, and synchronize doors, animations, colors, and button positions.
        /// </summary>
        private void Refresh()
        {
// Echo or when the player is destroyed, HashSet Null references may be left inside; clean them up before refreshing.
            occupants.RemoveWhere(occupant => occupant == null);

            bool pressed = occupants.Count > 0;
// pressed State drives both visual feedback and reality Door logic.
            ApplyPressedMotion(pressed);
            UpdateButtonVisualAnimator(pressed);

            if (useLayeredVisualColors)
            {
                ApplyLayeredVisualColors(pressed);
            }
            else if (buttonVisualAnimator == null)
            {
                PrototypeFactory.Tint(gameObject, pressed ? new Color(0.15f, 0.9f, 0.45f) : new Color(1f, 0.85f, 0.15f));
            }

            if (linkedDoor != null)
            {
                if (pressed)
                {
// The door remains open as long as there are any valid objects holding it down.
                    linkedDoor.OpenDoor();
                }
                else
                {
// The door is closed after all objects have left, forming a need Echo The puzzle that keeps pressing.
                    linkedDoor.CloseDoor();
                }
            }
        }
        /// <summary>
/// Find the visual root node of the button that needs to be animated.
        /// </summary>
/// <returns>Return found Transform; may return if not found null。</returns>
        private Transform ResolvePressedMotionRoot()
        {
            if (buttonVisualAnimator != null)
            {
                return buttonVisualAnimator.transform;
            }

            if (layeredVisualRoot != null && layeredVisualRoot != transform)
            {
                return layeredVisualRoot;
            }

            return null;
        }
        /// <summary>
/// according to pressed Status move button visuals to make the pressure plate look like it's actually being depressed.
        /// </summary>
/// <param name="pressed">true Indicates that the pressure plate is pressed, false Indicates loosening. </param>
        private void ApplyPressedMotion(bool pressed)
        {
            if (pressedMotionRoot == null)
            {
                return;
            }

            pressedMotionRoot.localPosition = restingPressedMotionLocalPosition + (pressed ? pressedLocalOffset : Vector3.zero);
        }
        /// <summary>
/// Bundle pressed Pass status to button Animator, used to play the animation of a button being pressed or released.
        /// </summary>
/// <param name="pressed">true Indicates that the pressure plate is pressed, false Indicates loosening. </param>
        private void UpdateButtonVisualAnimator(bool pressed)
        {
            if (buttonVisualAnimator == null ||
                !buttonVisualAnimator.isActiveAndEnabled ||
                !buttonVisualAnimator.gameObject.activeInHierarchy)
            {
// Animator There will be a direct call when the object is not activated. Unity Warning, so filter first.
                return;
            }

            buttonVisualAnimator.SetBool("Pressed", pressed);
            buttonVisualAnimator.Play(pressed ? "Pressed" : "Idle");
        }
        /// <summary>
/// Cache button multi-layer SpriteRenderer The original color can be easily brightened when pressed and restored when released.
        /// </summary>
        private void CacheLayeredVisualColors()
        {
            if (!useLayeredVisualColors)
            {
                return;
            }

            Transform root = layeredVisualRoot != null ? layeredVisualRoot : transform;
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                layeredVisualBaseColors[renderer] = renderer.color;
            }
        }
        /// <summary>
/// Depending on whether the button is pressed, give multiple layers SpriteRenderer Apply different color feedback.
        /// </summary>
/// <param name="pressed">true Indicates that the pressure plate is pressed, false Indicates loosening. </param>
        private void ApplyLayeredVisualColors(bool pressed)
        {
            if (layeredVisualBaseColors.Count == 0)
            {
                CacheLayeredVisualColors();
            }

            foreach (KeyValuePair<SpriteRenderer, Color> entry in layeredVisualBaseColors)
            {
                SpriteRenderer renderer = entry.Key;
                if (renderer == null)
                {
                    continue;
                }

                renderer.color = pressed ? BoostPressedColor(entry.Value) : entry.Value;
            }
        }
        /// <summary>
/// Brighten the original color to use as visual feedback when the button is pressed.
        /// </summary>
/// <param name="baseColor">baseColor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns the processed color value. </returns>
        private Color BoostPressedColor(Color baseColor)
        {
            float red = Mathf.Min(1f, baseColor.r * 1.08f + 0.03f);
            float green = Mathf.Min(1f, baseColor.g * pressedVisualBoost + 0.08f);
            float blue = Mathf.Min(1f, baseColor.b * pressedVisualBoost + 0.05f);
            return new Color(red, green, blue, baseColor.a);
        }
        /// <summary>
/// Output the object information entering the pressure plate to facilitate debugging who pressed the button.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void LogOccupant(Collider2D other)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            string occupantName = IsEcho(other) ? "Echo" : "Player";
            Debug.Log($"PressurePlate pressed by {occupantName}");
        }
        /// <summary>
/// judge Collider whether from Echo Playback body. Echo The mechanism can be suppressed, but it cannot be considered as the player's death or clearance.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool IsEcho(Collider2D other)
        {
            return HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }
        /// <summary>
/// security check Collider or root object tag, avoid tag Absence causes an exception.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <param name="tagName">tagName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
