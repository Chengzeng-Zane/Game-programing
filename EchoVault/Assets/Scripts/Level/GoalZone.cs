using UnityEngine;

namespace EchoVault
{
    public class GoalZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                EchoVaultGameManager.Instance?.Win();
            }
        }
    }
}
