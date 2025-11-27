using UnityEngine;

public class PlayerTargeting : MonoBehaviour
{
    // Singleton
    public static PlayerTargeting Instance { get; private set; }

    [Header("Targeting Settings")]
    public float searchRadius = 10f;
    public LayerMask enemyLayer;
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
        FindClosestEnemy();
        HandleAimingState();
        AimToTarget();
    }

    void FindClosestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);

        float closestDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                nearest = enemy.transform;
            }
        }

        currentTarget = nearest;
    }

    // 여기서 PlayerMovement와 실제로 연결
    void HandleAimingState()
    {
        if (PlayerMovement.Instance == null)
            return;

        // 타겟이 있을 때만 조준 상태 ON
        if (currentTarget != null)
            PlayerMovement.Instance.isAiming = true;
        else
            PlayerMovement.Instance.isAiming = false;
    }

    void AimToTarget()
    {
        if (currentTarget == null)
            return;

        Vector3 dir = (currentTarget.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
    }
}
