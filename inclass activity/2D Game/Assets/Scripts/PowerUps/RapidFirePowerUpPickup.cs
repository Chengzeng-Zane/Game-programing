using UnityEngine;

/// <summary>
/// Pickup behaviour for the rapid fire gameplay improvement.
/// The visible RapidFirePowerUp prefab uses this component.
/// </summary>
public class RapidFirePowerUpPickup : MonoBehaviour
{
    private GameManager gameManager;
    private float duration = 6f;
    private Vector3 baseScale = Vector3.one;

    public void SetUp(GameManager manager, float powerUpDuration)
    {
        gameManager = manager;
        duration = powerUpDuration;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, 120f * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.12f;
        transform.localScale = baseScale * pulse;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool hitPlayer = other.CompareTag("Player") || other.GetComponentInParent<Controller>() != null;
        if (!hitPlayer || gameManager == null)
        {
            return;
        }

        gameManager.ActivateRapidFirePowerUp(duration);
        Destroy(gameObject);
    }
}
