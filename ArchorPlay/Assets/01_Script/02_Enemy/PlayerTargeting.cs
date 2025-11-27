using UnityEngine;

public class PlayerTargeting : MonoBehaviour
{
    public static PlayerTargeting Instance { get; private set; }

    [Header("Targeting Settings")]
    public float searchRadius = 10f;
    public LayerMask enemyLayer;      // 적 레이어
    public LayerMask visibilityMask;  // 적 + 장애물(벽) 레이어 모두 포함
    public float eyeHeight = 1.2f;    // 플레이어 눈높이

    public Transform currentTarget;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        FindClosestVisibleEnemy();
        HandleAimingState();
        AimToTarget();
    }

    void FindClosestVisibleEnemy()
    {
        Collider[] enemies =
            Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);

        float closestDistance = Mathf.Infinity;
        Transform nearest = null;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        foreach (var enemyCol in enemies)
        {
            Transform enemy = enemyCol.transform;
            Vector3 targetPos = enemyCol.bounds.center; // 적 중심 쪽으로

            Vector3 dir = (targetPos - eyePos).normalized;
            float dist = Vector3.Distance(eyePos, targetPos);

            // 시야 체크: 적 + 장애물 레이어로 Raycast
            if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist, visibilityMask))
            {
                // 첫 번째로 맞은 게 적이 아니면 → 벽/장애물에 가려진 것
                if (!hit.collider.transform.IsChildOf(enemy))
                {
                    continue;
                }
            }

            // 여기까지 왔으면 “보이는 적”
            if (dist < closestDistance)
            {
                closestDistance = dist;
                nearest = enemy;
            }
        }

        currentTarget = nearest;
    }

    void HandleAimingState()
    {
        if (PlayerMovement.Instance == null)
            return;

        PlayerMovement.Instance.isAiming = (currentTarget != null);
    }

    void AimToTarget()
    {
        if (currentTarget == null)
            return;

        Vector3 dir = (currentTarget.position - transform.position).normalized;
        dir.y = 0f;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRot,
            Time.deltaTime * 10f
        );
    }
}
