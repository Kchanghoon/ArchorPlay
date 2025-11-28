using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 자동 공격 컴포넌트
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    #region Constants
    private const float DEFAULT_EYE_HEIGHT = 1f;
    private const float AIM_PREPARE_DURATION = 0.1f;
    #endregion

    #region Serialized Fields
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireRate = 4f;
    [SerializeField] private int damage = 10;
    [SerializeField] private BulletType bulletType = BulletType.Pistol;
    [SerializeField] private float bulletSpeed = 30f;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask shootMask;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Dependencies")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerTargeting targeting;
    [SerializeField] private BulletPool bulletPool;
    #endregion

    #region Private Fields
    private float nextFireTime = 0f;
    private Coroutine attackCoroutine;
    #endregion

    #region Properties
    public BulletType CurrentBulletType
    {
        get => bulletType;
        set => bulletType = value;
    }

    public bool CanAttack => Time.time >= nextFireTime && attackCoroutine == null;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeComponents();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        ProcessAutoAttack();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (targeting == null)
            targeting = GetComponent<PlayerTargeting>();

        if (bulletPool == null)
            bulletPool = BulletPool.Instance;
    }

    private void SubscribeToEvents()
    {
        if (movement != null)
        {
            movement.OnWeaponChanged += HandleWeaponChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (movement != null)
        {
            movement.OnWeaponChanged -= HandleWeaponChanged;
        }
    }
    #endregion

    #region Event Handlers
    private void HandleWeaponChanged(WeaponType weaponType)
    {
        // 무기 타입에 따라 탄환 타입 변경
        bulletType = weaponType switch
        {
            WeaponType.Hand => BulletType.Hand,
            WeaponType.Pistol => BulletType.Pistol,
            WeaponType.DualPistol => BulletType.DualPistol,
            WeaponType.Sniper => BulletType.Sniper,
            _ => BulletType.Pistol
        };
    }
    #endregion

    #region Auto Attack
    private void ProcessAutoAttack()
    {
        // 의존성 체크
        if (targeting == null || movement == null)
            return;

        // 이동 중에는 공격 불가
        if (movement.IsMoving)
            return;

        // 사망 상태에서는 공격 불가
        if (movement.IsDead)
            return;

        // 타겟 확인
        Transform target = targeting.CurrentTarget;
        if (target == null)
            return;

        // 사거리 체크
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange)
            return;

        // 공격 가능 여부 체크
        if (!CanAttack)
            return;

        // 공격 시작
        StartAttackSequence(target);
    }

    private void StartAttackSequence(Transform target)
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine = StartCoroutine(AttackRoutine(target));
    }

    public void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (movement != null)
        {
            movement.SetState(PlayerState.Idle);
        }
    }
    #endregion

    #region Attack Coroutine
    private IEnumerator AttackRoutine(Transform target)
    {
        // 공격 상태로 전환
        if (movement != null)
        {
            movement.SetState(PlayerState.Attacking);
        }

        // 에이밍 준비 시간
        yield return new WaitForSeconds(AIM_PREPARE_DURATION);

        // 타겟 유효성 재확인
        if (!IsTargetValid(target))
        {
            FinishAttack();
            yield break;
        }

        // 발사
        ExecuteShoot(target);

        // 다음 발사 시간 설정
        nextFireTime = Time.time + (1f / fireRate);

        // 공격 완료
        FinishAttack();
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null)
            return false;

        // GameObject가 비활성화되었는지 체크
        if (!target.gameObject.activeInHierarchy)
            return false;

        // 적의 Health 컴포넌트 체크 (죽었는지 확인)
        var enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.IsDead)
            return false;

        // 다른 Health 컴포넌트 이름이 있다면 추가
        // 예: var health = target.GetComponent<Health>();
        // if (health != null && health.CurrentHealth <= 0)
        //     return false;

        return true;
    }

    private void FinishAttack()
    {
        if (movement != null)
        {
            movement.SetState(PlayerState.Idle);
        }

        attackCoroutine = null;
    }
    #endregion

    #region Shooting
    private void ExecuteShoot(Transform target)
    {
        if (target == null)
            return;

        // 애니메이션 트리거
        if (movement != null)
        {
            movement.TriggerAttackAnimation();
        }

        // 이펙트 재생
        PlayMuzzleFlash();

        // 발사 위치 및 방향 계산
        Vector3 origin = GetFireOrigin();
        Vector3 direction = CalculateShootDirection(target, origin);

        // 탄환 생성
        SpawnProjectile(origin, direction);
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.position + Vector3.up * DEFAULT_EYE_HEIGHT;
    }

    private Vector3 CalculateShootDirection(Transform target, Vector3 origin)
    {
        Vector3 targetPosition = target.position + Vector3.up * DEFAULT_EYE_HEIGHT;
        return (targetPosition - origin).normalized;
    }

    private void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        if (bulletPool == null)
        {
            Debug.LogWarning("BulletPool is not assigned!");
            return;
        }

        GameObject bulletObj = bulletPool.Spawn(
            bulletType,
            origin,
            Quaternion.LookRotation(direction)
        );

        if (bulletObj == null)
            return;

        // 물리 속도 설정
        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;
        }

        // 발사체 설정
        ProjectileBullet projectile = bulletObj.GetComponent<ProjectileBullet>();
        if (projectile != null)
        {
            projectile.type = bulletType;
            projectile.damage = damage;
            projectile.hitMask = shootMask;
        }
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion
}