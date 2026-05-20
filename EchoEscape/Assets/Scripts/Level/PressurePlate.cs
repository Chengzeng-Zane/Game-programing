using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    public class PressurePlate : MonoBehaviour
    {
        public Door linkedDoor;
        public bool enableDebugLogs = true;
        public bool IsPressed => occupants.Count > 0;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private Vector3 restingLocalPosition;

        private void Awake()
        {
            restingLocalPosition = transform.localPosition;
            Refresh();
        }

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

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants.Remove(other);
            Refresh();
        }

        private bool CanPress(Collider2D other)
        {
            return HasTag(other, "Player") ||
                HasTag(other, "Echo") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }

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

        private void LogOccupant(Collider2D other)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            string occupantName = IsEcho(other) ? "Echo" : "Player";
            Debug.Log($"PressurePlate pressed by {occupantName}");
        }

        private bool IsEcho(Collider2D other)
        {
            return HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }

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
