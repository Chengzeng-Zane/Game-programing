using UnityEngine;

namespace EchoEscape
{
    public class GoalZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                EchoEscapeGameManager.Instance?.Win();
            }
        }
    }
}
