using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public float detectionRadius = 10f;   // 플레이어 감지 거리

    NavMeshAgent agent;
    Transform player;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // 플레이어 Transform 가져오기
        player = PlayerMovement.Instance.transform;
        // 또는 player = GameObject.FindWithTag("Player").transform;

        // 스크립트에서 따로 사용할 거라면 여기서도 설정 가능
        // agent.stoppingDistance = 1.5f;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 감지 범위 밖이면 멈춤(Idle 상태)
        if (distance > detectionRadius)
        {
            agent.isStopped = true;
            return;
        }

        // 감지 범위 안 → 추적 시작
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }
}
