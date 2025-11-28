using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHp = 30;

    private int currentHp;
    private bool isDead = false;

    // 외부에서 죽음 상태 확인용
    public bool IsDead => isDead;
    public int CurrentHp => currentHp;

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        // 이미 죽었으면 데미지 무시
        if (isDead)
            return;

        currentHp -= damage;

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 타겟팅에서 즉시 제외되도록 Collider 비활성화
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // NavMeshAgent 정지
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // EnemyMovement 스크립트 비활성화
        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        // TODO: 죽는 애니메이션이 있다면
        // Animator anim = GetComponent<Animator>();
        // if (anim != null)
        // {
        //     anim.SetTrigger("Die");
        // }

        // 2초 후 파괴 (애니메이션 재생 시간 고려)
        Destroy(gameObject, 2f);
    }
}