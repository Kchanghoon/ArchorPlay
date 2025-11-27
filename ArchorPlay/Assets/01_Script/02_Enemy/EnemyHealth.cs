using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHp = 30;
    int currentHp;

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // TODO: 죽는 애니메이션 넣고 싶으면 여기서
        // Anim.SetTrigger("Die"); 후에 Destroy 지연도 가능
        Destroy(gameObject);
    }
}
