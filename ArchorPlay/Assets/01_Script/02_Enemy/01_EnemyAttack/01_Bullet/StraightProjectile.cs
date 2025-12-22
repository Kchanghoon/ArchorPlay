using UnityEngine;

public class StraightProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 100; // int로 변경
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log($"플레이어 피격! 데미지:{damage}");
            }
            else
            {
                Debug.LogWarning("PlayerMovement를 찾지 못함");
            }

            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") || collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }
    }
}