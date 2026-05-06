using UnityEngine;

namespace EchoVault
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float jumpForce = 10f;
        public float interactRadius = 1.1f;

        private Rigidbody2D body;
        private ActionRecorder recorder;
        private float moveInput;
        private float groundedUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            recorder = GetComponent<ActionRecorder>();
        }

        private void Update()
        {
            moveInput = Input.GetAxisRaw("Horizontal");

            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && IsGrounded())
            {
                body.velocity = new Vector2(body.velocity.x, jumpForce);
                EchoVaultGameManager.Instance?.AudioService?.PlayJump();
            }

            if (Input.GetKeyDown(KeyCode.Q) && recorder != null)
            {
                recorder.ToggleRecording();
            }

            if (Input.GetKeyDown(KeyCode.E) && recorder != null)
            {
                recorder.PlayEcho();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                TryOpenChest();
            }
        }

        private void FixedUpdate()
        {
            body.velocity = new Vector2(moveInput * moveSpeed, body.velocity.y);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.45f)
                {
                    groundedUntil = Time.time + 0.12f;
                    return;
                }
            }
        }

        public void Respawn(Vector3 position)
        {
            transform.position = position;
            body.velocity = Vector2.zero;
        }

        private bool IsGrounded()
        {
            return Time.time <= groundedUntil;
        }

        private void TryOpenChest()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Chest nearestChest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                Chest chest = hit.GetComponent<Chest>();
                if (chest == null || chest.IsOpened)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, chest.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestChest = chest;
                }
            }

            if (nearestChest != null)
            {
                nearestChest.Open();
            }
            else if (EchoVaultGameManager.Instance != null)
            {
                EchoVaultGameManager.Instance.UpdateStatus("No unopened chest close enough. Stand beside it and press F.");
            }
        }
    }
}
