using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Controls a pressure plate that opens or closes a linked door.
    /// </summary>
    /// <remarks>
    /// Attach this script to the yellow pressure plate trigger object.
    /// It detects Player and Echo colliders, stores current occupants in a HashSet,
    /// and keeps the linked Door open while at least one valid occupant remains on the plate.
    /// </remarks>
    public class PressurePlate : MonoBehaviour
    {
        /// <summary>
        /// Door opened while this plate is pressed.
        /// </summary>
        public Door linkedDoor;

        /// <summary>
        /// If true, writes Console messages when Player or Echo presses the plate.
        /// </summary>
        public bool enableDebugLogs = true;

        /// <summary>
        /// Keeps decorative child renderer colors instead of applying one flat tint.
        /// </summary>
        [SerializeField] private bool useLayeredVisualColors;

        /// <summary>
        /// Root transform for decorative pressure plate layers.
        /// </summary>
        [SerializeField] private Transform layeredVisualRoot;

        /// <summary>
        /// Brightness multiplier used when the layered visual is pressed.
        /// </summary>
        [SerializeField] private float pressedVisualBoost = 1.35f;

        /// <summary>
        /// Animator used by the decorative button visual.
        /// </summary>
        [SerializeField] private Animator buttonVisualAnimator;

        /// <summary>
        /// Local movement applied while the plate is pressed.
        /// </summary>
        [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.05f, 0f);

        /// <summary>
        /// True when at least one Player or Echo collider is currently on the plate.
        /// </summary>
        public bool IsPressed => occupants.Count > 0;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private readonly Dictionary<SpriteRenderer, Color> layeredVisualBaseColors = new Dictionary<SpriteRenderer, Color>();
        private Transform pressedMotionRoot;
        private Vector3 restingPressedMotionLocalPosition;

        /// <summary>
        /// Unity event method called when the pressure plate object is created.
        /// </summary>
        /// <remarks>
        /// Stores the unpressed visual position so the plate can visually move when pressed.
        /// </remarks>
        private void Awake()
        {
            pressedMotionRoot = ResolvePressedMotionRoot();
            if (pressedMotionRoot != null)
            {
                restingPressedMotionLocalPosition = pressedMotionRoot.localPosition;
            }

            CacheLayeredVisualColors();
            Refresh();
        }

        /// <summary>
        /// Unity physics event called when another 2D collider enters the trigger area.
        /// </summary>
        /// <param name="other">The collider that entered the pressure plate trigger.</param>
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
        /// Unity physics event called when another 2D collider leaves the trigger area.
        /// </summary>
        /// <param name="other">The collider that exited the pressure plate trigger.</param>
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
        /// Determines whether a collider is allowed to press this plate.
        /// </summary>
        /// <param name="other">The collider being tested.</param>
        /// <returns>True for Player or Echo objects; otherwise false.</returns>
        private bool CanPress(Collider2D other)
        {
            return HasTag(other, "Player") ||
                HasTag(other, "Echo") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }

        /// <summary>
        /// Updates plate visuals and opens or closes the linked door based on current occupants.
        /// </summary>
        /// <remarks>
        /// The HashSet prevents the door from closing when the player leaves but the Echo remains on the plate.
        /// </remarks>
        private void Refresh()
        {
            occupants.RemoveWhere(occupant => occupant == null);

            bool pressed = occupants.Count > 0;
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
                    linkedDoor.OpenDoor();
                }
                else
                {
                    linkedDoor.CloseDoor();
                }
            }
        }

        /// <summary>
        /// Finds the visual transform that should move when the plate is pressed.
        /// </summary>
        /// <returns>The visual transform, or null if this plate has no movable visual.</returns>
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
        /// Moves only the decorative visual, never the trigger collider root.
        /// </summary>
        /// <param name="pressed">True when the plate is currently pressed.</param>
        private void ApplyPressedMotion(bool pressed)
        {
            if (pressedMotionRoot == null)
            {
                return;
            }

            pressedMotionRoot.localPosition = restingPressedMotionLocalPosition + (pressed ? pressedLocalOffset : Vector3.zero);
        }

        /// <summary>
        /// Switches the decorative button animator between idle and pressed states.
        /// </summary>
        /// <param name="pressed">True when the pressure plate is active.</param>
        private void UpdateButtonVisualAnimator(bool pressed)
        {
            if (buttonVisualAnimator == null ||
                !buttonVisualAnimator.isActiveAndEnabled ||
                !buttonVisualAnimator.gameObject.activeInHierarchy)
            {
                return;
            }

            buttonVisualAnimator.SetBool("Pressed", pressed);
            buttonVisualAnimator.Play(pressed ? "Pressed" : "Idle");
        }

        /// <summary>
        /// Stores the original renderer colors for layered visual mode.
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
        /// Applies the idle or pressed color state to layered visuals.
        /// </summary>
        /// <param name="pressed">True when the plate is currently pressed.</param>
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
        /// Brightens a decorative renderer color while preserving its base hue.
        /// </summary>
        /// <param name="baseColor">The original renderer color.</param>
        /// <returns>The color used while the plate is pressed.</returns>
        private Color BoostPressedColor(Color baseColor)
        {
            float red = Mathf.Min(1f, baseColor.r * 1.08f + 0.03f);
            float green = Mathf.Min(1f, baseColor.g * pressedVisualBoost + 0.08f);
            float blue = Mathf.Min(1f, baseColor.b * pressedVisualBoost + 0.05f);
            return new Color(red, green, blue, baseColor.a);
        }

        /// <summary>
        /// Writes a debug message that identifies what pressed the plate.
        /// </summary>
        /// <param name="other">The collider that pressed the plate.</param>
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
        /// Checks whether a collider belongs to an Echo replay object.
        /// </summary>
        /// <param name="other">The collider to inspect.</param>
        /// <returns>True if the collider or parent has Echo identity; otherwise false.</returns>
        private bool IsEcho(Collider2D other)
        {
            return HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }

        /// <summary>
        /// Safely checks a tag without throwing if the tag does not exist in Unity settings.
        /// </summary>
        /// <param name="other">The collider whose object or root should be checked.</param>
        /// <param name="tagName">The Unity tag name to compare.</param>
        /// <returns>True if the collider object or root object has the tag; otherwise false.</returns>
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
