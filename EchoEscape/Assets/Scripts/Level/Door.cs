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

        public void SetOpen(bool open)
        {
            isOpen = open;

            if (doorCollider != null)
            {
                doorCollider.enabled = !isOpen;
            }

            PrototypeFactory.Tint(gameObject, isOpen ? openColor : closedColor);
        }
    }
}
