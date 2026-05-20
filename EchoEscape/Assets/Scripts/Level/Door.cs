using UnityEngine;

namespace EchoEscape
{
    public class Door : MonoBehaviour
    {
        public Color closedColor = new Color(0.85f, 0.18f, 0.14f);
        public Color openColor = new Color(0.12f, 0.75f, 0.32f);
        public bool IsOpen => isOpen;

        private Collider2D doorCollider;
        private bool isOpen;

        private void Awake()
        {
            doorCollider = GetComponent<Collider2D>();
            SetOpen(false);
        }

        public void OpenDoor()
        {
            SetOpen(true);
        }

        public void CloseDoor()
        {
            SetOpen(false);
        }

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
