using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 이동 및 추적 컴포넌트
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;      // 플레이어 감지 거리
    [SerializeField] private float attackRange = 2f;           // 공격 범위 (이 거리까지만 접근)
    [SerializeField] private float attackRangeTolerance = 0.5f; // 공격 범위 여유값

    private NavMeshAgent agent;
    private Transform player;
    private bool isChasing = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        InitializePlayer();
        ConfigureAgent();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        UpdateChaseState(distanceToPlayer);
    }

    /// <summary>
    /// 플레이어 Transform 초기화
    /// </summary>
    private void InitializePlayer()
    {
        if (PlayerMovement.Instance != null)
        {
            player = PlayerMovement.Instance.transform;
        }
        else
        {
            // Fallback: Tag로 찾기
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("Player not found! Make sure Player has 'Player' tag or PlayerMovement.Instance exists.");
            }
        }
    }

    /// <summary>
    /// NavMeshAgent 설정
    /// </summary>
    private void ConfigureAgent()
    {
        if (agent == null) return;

        // 공격 범위만큼 떨어진 거리에서 멈춤
        agent.stoppingDistance = attackRange;

        // 회전 속도 (선택사항)
        agent.angularSpeed = 120f;

        // 가속도 (선택사항)
        agent.acceleration = 8f;
    }

    /// <summary>
    /// 추적 상태 업데이트
    /// </summary>
    private void UpdateChaseState(float distanceToPlayer)
    {
        // 감지 범위 밖 → 멈춤 (Idle)
        if (distanceToPlayer > detectionRadius)
        {
            if (isChasing)
            {
                StopChasing();
            }
            return;
        }

        // 감지 범위 안 → 추적
        if (!isChasing)
        {
            StartChasing();
        }

        // 공격 범위 체크
        if (distanceToPlayer <= attackRange + attackRangeTolerance)
        {
            // 공격 범위 안 → 멈춤 (공격 준비)
            agent.isStopped = true;

            // 플레이어를 바라봄
            LookAtPlayer();
        }
        else
        {
            // 공격 범위 밖 → 계속 추적
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    /// <summary>
    /// 추적 시작
    /// </summary>
    private void StartChasing()
    {
        isChasing = true;
        agent.isStopped = false;
    }

    /// <summary>
    /// 추적 중지
    /// </summary>
    private void StopChasing()
    {
        isChasing = false;
        agent.isStopped = true;
    }

    /// <summary>
    /// 플레이어를 바라봄
    /// </summary>
    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position);
        direction.y = 0f; // 수평 방향만

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }

    /// <summary>
    /// Gizmos로 범위 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 감지 범위 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 공격 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}