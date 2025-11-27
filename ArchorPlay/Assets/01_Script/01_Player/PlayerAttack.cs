using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 15f;
    public float fireRate = 4f;
    public int damage = 10;

    [Header("Layer Masks")]
    public LayerMask shootMask;          // Enemy + Obstacle

    [Header("Projectile")]
    public BulletType bulletType = BulletType.Pistol; 
    public float bulletSpeed = 30f;

    [Header("FX (Effect)")]
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public Animator anim;

    public LineRenderer laser;
    public bool useLaserSight = true;

    float nextFireTime = 0f;
    bool isAttacking = false;

    void Start()
    {
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    void Update()
    {
        AutoAttack();
        UpdateLaserSight();
    }

    void AutoAttack()
    {
        if (PlayerTargeting.Instance == null)
            return;

        Transform target = PlayerTargeting.Instance.currentTarget;
        if (target == null)
            return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attackRange)
            return;

        if (Time.time < nextFireTime)
            return;

        if (isAttacking)
            return;

        StartCoroutine(AttackRoutine(target));
    }

    IEnumerator AttackRoutine(Transform target)
    {
        isAttacking = true;

        var move = PlayerMovement.Instance;
        if (move != null)
        {
            move.isAiming = true;
            move.isAttacking = true;
        }

        // 에이밍 준비 시간
        yield return new WaitForSeconds(0.1f);

        // ★ 그 사이에 타겟이 죽었을 수도 있으니 체크
        if (target == null)
        {
            if (move != null)
                move.isAttacking = false;

            isAttacking = false;
            yield break;
        }

        Shoot(target);

        nextFireTime = Time.time + (1f / fireRate);

        if (move != null)
            move.isAttacking = false;

        isAttacking = false;
    }

    void Shoot(Transform target)
    {
        if (target == null)
            return;

        if (anim != null)
            anim.SetTrigger("Attack");

        if (muzzleFlash != null)
            muzzleFlash.Play();

        Vector3 origin = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * 1f;

        Vector3 dir = (target.position + Vector3.up * 1f - origin).normalized;

        GameObject bulletObj = BulletPool.Instance.Spawn(
            bulletType,
            origin,
            Quaternion.LookRotation(dir)
        );

        if (bulletObj == null)
            return;

        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dir * bulletSpeed;

        ProjectileBullet proj = bulletObj.GetComponent<ProjectileBullet>();
        if (proj != null)
        {
            proj.type = bulletType;
            proj.damage = damage;
            proj.hitMask = shootMask;
        }
    }

    void UpdateLaserSight()
    {
        if (!useLaserSight || laser == null || firePoint == null)
        {
            if (laser != null) laser.enabled = false;
            return;
        }

        // 타겟이 없으면 레이저 끄기
        Transform target = PlayerTargeting.Instance != null
            ? PlayerTargeting.Instance.currentTarget
            : null;

        if (target == null)
        {
            laser.enabled = false;
            return;
        }

        laser.enabled = true;

        // 포인트 개수는 2개로 고정
        if (laser.positionCount != 2)
            laser.positionCount = 2;

        // 총구 → 적까지 선
        Vector3 start = firePoint.position;
        Vector3 end = target.position + Vector3.up * 1f;

        laser.SetPosition(0, start);
        laser.SetPosition(1, end);
    }

    void LateUpdate()
    {
        TestLaser();
    }
    void TestLaser()
    {
        if (laser == null || firePoint == null)
            return;

        laser.enabled = true;
        if (laser.positionCount != 2)
            laser.positionCount = 2;

        Vector3 start = firePoint.position;
        Vector3 end = firePoint.position + firePoint.forward * 5f + Vector3.up * 0.1f;

        laser.SetPosition(0, start);
        laser.SetPosition(1, end);
    }
}
