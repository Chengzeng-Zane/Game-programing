using UnityEngine;

namespace EchoVault
{
    public class PressurePlate : MonoBehaviour
    {
        public Door linkedDoor;
        public bool IsPressed => occupants > 0;

        private int occupants;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants++;
            Refresh();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants = Mathf.Max(0, occupants - 1);
            Refresh();
        }

        private bool CanPress(Collider2D other)
        {
            return other.GetComponent<PlayerController2D>() != null || other.GetComponent<EchoReplayController>() != null;
        }

        private void Refresh()
        {
            bool pressed = occupants > 0;
            PrototypeFactory.Tint(gameObject, pressed ? new Color(0.15f, 0.9f, 0.45f) : new Color(1f, 0.85f, 0.15f));

            if (linkedDoor != null)
            {
                linkedDoor.SetOpen(pressed);
            }
        }
    }
}
