using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 250;

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null && !player.IsDead)
        {
            player.Heal(healAmount);
            Debug.Log($"플레이어 회복 +{healAmount}");
            Destroy(gameObject);
        }
    }
}