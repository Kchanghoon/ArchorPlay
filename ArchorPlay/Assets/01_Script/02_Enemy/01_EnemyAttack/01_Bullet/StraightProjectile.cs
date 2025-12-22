using UnityEngine;

public class StraightProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 10; // int로 변경

    private void OnCollisionEnter(Collision collision)
    {
        // 플레이어와 충돌 시
        if (collision.gameObject.CompareTag("Player"))
        {
            // HPBar 컴포넌트로 데미지 적용
            HPBar hpBar = collision.gameObject.GetComponent<HPBar>();
            if (hpBar != null)
            {
                hpBar.TakeDamage(damage);
                Debug.Log($"플레이어 피격! 데미지: {damage}, 남은 HP: {hpBar.CurrentHp}");
            }
            else
            {
                Debug.Log($"플레이어 피격! (HPBar 없음) 데미지: {damage}");
            }

            // 발사체 파괴
            Destroy(gameObject);
            return;
        }

        // 벽과 충돌 시 (Wall 레이어 체크)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        // 또는 Wall 태그로 체크하는 방법
        if (collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }
    }
}