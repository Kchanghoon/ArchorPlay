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

    [SerializeField] private int damageAmount = 100;
    [SerializeField] private float attackCooldown = 1f;
    private Animator animator;
    private float lastAttackTime;

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
        if (player != null && !player.IsDead)
        {
            player.TakeDamage(damageAmount);
            lastAttackTime = Time.time;
            Debug.Log($"플레이어에게 {damageAmount} 데미지!");
        }
    }

    void Awake()
    {
        currentHp = maxHp;
        animator = GetComponent<Animator>();
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

        // 콜라이더 비활성화
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // NavMeshAgent 정지
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 이동 로직 중단
        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null)
            movement.enabled = false;

        // ⭐ 죽음 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 애니메이션 길이만큼 기다렸다가 제거
        Destroy(gameObject, 2f);
    }

}