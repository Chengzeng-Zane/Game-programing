using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Door or Magic Barrier Control Script. It opens or closes the channel according to the state of the machine.
/// Gameplay logic: disabled when open Collider and hide/Dilute the visual to allow players or Echo Can pass; restores collision and display when turned off, re-blocking routes.
/// Collaborates with: PressurePlate Or level process call OpenDoor、CloseDoor、SetOpen。
    /// </summary>
    public class Door : MonoBehaviour
    {
        public Color closedColor = new Color(0.85f, 0.18f, 0.14f);
        public Color openColor = new Color(0.12f, 0.75f, 0.32f);
        public bool IsOpen => isOpen;

        private Collider2D doorCollider;
        private bool isOpen;
        /// <summary>
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            doorCollider = GetComponent<Collider2D>();
            SetOpen(false);
        }
        /// <summary>
/// Open doors, chests, or pathways that allow players to progress or receive rewards.
        /// </summary>
        public void OpenDoor()
        {
            SetOpen(true);
        }
        /// <summary>
/// Close a door, panel or passage to restore it to a blocked or hidden state.
        /// </summary>
        public void CloseDoor()
        {
            SetOpen(false);
        }
        /// <summary>
/// Set the door's opening and closing status. Disables collision and changes to the open color when turned on; restores collision and turns to the off color when turned off.
        /// </summary>
/// <param name="open">true means open, false means closed. </param>
        public void SetOpen(bool open)
        {
            bool changed = isOpen != open;
            isOpen = open;

            if (doorCollider != null)
            {
// Disabled when door is open Collider, players and Echo to actually through through; blocking is restored when closed.
                doorCollider.enabled = !isOpen;
            }

// PrototypeFactory. Tint Responsible for changing SpriteRenderer/Renderer Color as quick visual feedback of switch status.
            PrototypeFactory.Tint(gameObject, isOpen ? openColor : closedColor);

            if (changed)
            {
// Only writes when the status really changes HUD and logs to prevent the pressure plate from refreshing every frame.
                string message = isOpen ? "Door opened" : "Door closed";
                EchoEscapeGameManager.Instance?.UpdateStatus(message);
                Debug.Log(message);
            }
        }
    }
}
