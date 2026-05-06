using UnityEngine;

namespace EchoVault
{
    public class HazardZone : MonoBehaviour
    {
        public string deathReason = "hit a hazard";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                EchoVaultGameManager.Instance?.KillPlayer(deathReason);
            }
        }
    }
}
