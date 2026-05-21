using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Controls a simple puzzle door that can open or close.
    /// </summary>
    /// <remarks>
    /// Attach this script to a door object with a Collider2D.
    /// PressurePlate calls OpenDoor and CloseDoor to disable or restore the collider,
    /// letting the player pass only while the door is open.
    /// </remarks>
    public class Door : MonoBehaviour
    {
        /// <summary>
        /// Visual color used while the door blocks the player.
        /// </summary>
        public Color closedColor = new Color(0.85f, 0.18f, 0.14f);

        /// <summary>
        /// Visual color used while the door is open.
        /// </summary>
        public Color openColor = new Color(0.12f, 0.75f, 0.32f);

        /// <summary>
        /// True when the door collider is disabled and the player can pass.
        /// </summary>
        public bool IsOpen => isOpen;

        private Collider2D doorCollider;
        private bool isOpen;

        /// <summary>
        /// Unity event method called when the door object is created.
        /// </summary>
        /// <remarks>
        /// Caches the Collider2D and starts the door in the closed state.
        /// </remarks>
        private void Awake()
        {
            doorCollider = GetComponent<Collider2D>();
            SetOpen(false);
        }

        /// <summary>
        /// Opens the door so the player can pass through it.
        /// </summary>
        /// <remarks>
        /// Called by PressurePlate while the plate is pressed.
        /// </remarks>
        public void OpenDoor()
        {
            SetOpen(true);
        }

        /// <summary>
        /// Closes the door so it blocks the player again.
        /// </summary>
        /// <remarks>
        /// Called by PressurePlate when no valid occupant remains on the plate.
        /// </remarks>
        public void CloseDoor()
        {
            SetOpen(false);
        }

        /// <summary>
        /// Applies the requested door state to collider and visual feedback.
        /// </summary>
        /// <param name="open">True to open the door; false to close it.</param>
        public void SetOpen(bool open)
        {
            bool changed = isOpen != open;
            isOpen = open;

            if (doorCollider != null)
            {
                doorCollider.enabled = !isOpen;
            }

            PrototypeFactory.Tint(gameObject, isOpen ? openColor : closedColor);

            if (changed)
            {
                string message = isOpen ? "Door opened" : "Door closed";
                EchoEscapeGameManager.Instance?.UpdateStatus(message);
                Debug.Log(message);
            }
        }
    }
}
