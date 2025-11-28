using UnityEngine;
using System;

/// <summary>
/// 플레이어 타겟팅 시스템
/// </summary>
public class PlayerTargeting : MonoBehaviour
{
    #region Constants
    private const float TARGET_LOCK_SPEED = 10f;
    #endregion

    #region Singleton
    public static PlayerTargeting Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    #region Serialized Fields
    [Header("Targeting Settings")]
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private float targetUpdateInterval = 0.1f;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask visibilityMask;

    [Header("Dependencies")]
    [SerializeField] private PlayerMovement movement;
    #endregion

    #region Private Fields
    private Transform currentTarget;
    private float nextTargetUpdateTime;
    #endregion

    #region Properties
    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    #endregion

    #region Events
    public event Action<Transform> OnTargetAcquired;
    public event Action<Transform> OnTargetLost;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        UpdateTargeting();
        RotateToTarget();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
    }
    #endregion

    #region Targeting
    private void UpdateTargeting()
    {
        // 프레임마다 검색하지 않고 일정 간격으로 검색 (최적화)
        if (Time.time < nextTargetUpdateTime)
            return;

        nextTargetUpdateTime = Time.time + targetUpdateInterval;

        Transform previousTarget = currentTarget;
        currentTarget = FindClosestVisibleEnemy();

        // 타겟 변경 이벤트
        if (currentTarget != previousTarget)
        {
            if (previousTarget != null)
            {
                OnTargetLost?.Invoke(previousTarget);
            }

            if (currentTarget != null)
            {
                OnTargetAcquired?.Invoke(currentTarget);
            }
        }
    }

    private Transform FindClosestVisibleEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            enemyLayer
        );

        if (enemies.Length == 0)
            return null;

        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        Vector3 eyePosition = GetEyePosition();

        foreach (Collider enemyCollider in enemies)
        {
            // 디버그: 감지된 적 출력
            Debug.Log($"Detected enemy: {enemyCollider.name}, Active: {enemyCollider.gameObject.activeInHierarchy}");

            if (!IsEnemyVisible(enemyCollider, eyePosition, out float distance))
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemyCollider.transform;
            }
        }

        if (closestEnemy != null)
        {
            Debug.Log($"Current target: {closestEnemy.name}");
        }

        return closestEnemy;
    }

    private bool IsEnemyVisible(Collider enemyCollider, Vector3 eyePosition, out float distance)
    {
        Transform enemy = enemyCollider.transform;
        Vector3 targetPosition = enemyCollider.bounds.center;
        Vector3 direction = (targetPosition - eyePosition).normalized;
        distance = Vector3.Distance(eyePosition, targetPosition);

        // 적이 죽었는지 먼저 체크
        if (!IsEnemyAlive(enemy))
        {
            return false;
        }

        // Raycast로 시야 체크
        if (Physics.Raycast(eyePosition, direction, out RaycastHit hit, distance, visibilityMask))
        {
            // 첫 번째로 맞은 것이 적인지 확인
            // 적이거나 적의 자식 오브젝트면 보임
            if (hit.collider.transform == enemy || hit.collider.transform.IsChildOf(enemy))
            {
                return true;
            }
            else
            {
                // 장애물에 가려짐
                return false;
            }
        }

        // Raycast가 아무것도 안 맞았다 = visibilityMask에 해당하는 레이어가 없음
        // 이 경우 적이 visibilityMask에 포함되지 않았거나, 거리가 너무 짧을 수 있음
        // 안전하게 false 반환
        return false;
    }

    /// <summary>
    /// 적이 살아있는지 확인
    /// </summary>
    private bool IsEnemyAlive(Transform enemy)
    {
        if (enemy == null)
            return false;

        // GameObject가 비활성화되었는지 체크
        if (!enemy.gameObject.activeInHierarchy)
            return false;

        // 적의 EnemyHealth 컴포넌트 체크
        var enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            return !enemyHealth.IsDead;
        }

        // EnemyHealth 컴포넌트가 없으면 살아있다고 가정
        return true;
    }

    private Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * eyeHeight;
    }
    #endregion

    #region Rotation
    private void RotateToTarget()
    {
        if (currentTarget == null)
            return;

        if (movement == null)
            return;

        // 이동 중에는 회전하지 않음
        if (movement.IsMoving)
            return;

        // 공격 상태이거나 조준 상태일 때만 회전
        if (!movement.IsAiming)
            return;

        Vector3 direction = (currentTarget.position - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * TARGET_LOCK_SPEED
        );
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 타겟을 강제로 설정
    /// </summary>
    public void SetTarget(Transform target)
    {
        Transform previousTarget = currentTarget;
        currentTarget = target;

        if (currentTarget != previousTarget)
        {
            if (previousTarget != null)
            {
                OnTargetLost?.Invoke(previousTarget);
            }

            if (currentTarget != null)
            {
                OnTargetAcquired?.Invoke(currentTarget);
            }
        }
    }

    /// <summary>
    /// 타겟 해제
    /// </summary>
    public void ClearTarget()
    {
        if (currentTarget != null)
        {
            Transform previousTarget = currentTarget;
            currentTarget = null;
            OnTargetLost?.Invoke(previousTarget);
        }
    }

    /// <summary>
    /// 특정 적이 사거리 내에 있는지 확인
    /// </summary>
    public bool IsInRange(Transform target, float range)
    {
        if (target == null)
            return false;

        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= range;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        // 탐색 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        // 현재 타겟 시각화
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(GetEyePosition(), currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
    #endregion
}