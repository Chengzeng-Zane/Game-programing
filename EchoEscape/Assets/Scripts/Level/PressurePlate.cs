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
        /// True when at least one Player or Echo collider is currently on the plate.
        /// </summary>
        public bool IsPressed => occupants.Count > 0;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private Vector3 restingLocalPosition;

        /// <summary>
        /// Unity event method called when the pressure plate object is created.
        /// </summary>
        /// <remarks>
        /// Stores the unpressed local position so the plate can visually move down when pressed.
        /// </remarks>
        private void Awake()
        {
            restingLocalPosition = transform.localPosition;
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
            transform.localPosition = restingLocalPosition + (pressed ? new Vector3(0f, -0.05f, 0f) : Vector3.zero);
            PrototypeFactory.Tint(gameObject, pressed ? new Color(0.15f, 0.9f, 0.45f) : new Color(1f, 0.85f, 0.15f));

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
