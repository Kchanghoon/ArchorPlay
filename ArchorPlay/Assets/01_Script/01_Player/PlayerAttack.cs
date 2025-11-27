using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 15f;      // 총 사거리
    public float fireRate = 4f;          // 초당 발사 수 (예: 4면 0.25초마다 발사)
    public int damage = 10;              // 한 발당 데미지[Header("Layer Masks")]
    public LayerMask shootMask;          // 총알이 맞을 수 있는 레이어 (적 + 벽 둘 다)

    [Header("FX (선택 사항)")]
    public Transform firePoint;          // 총구 위치 (없으면 플레이어 위치 사용)
    public ParticleSystem muzzleFlash;   // 총구 이펙트
    public Animator anim;                // 공격 애니메이션용

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
    }

    void AutoAttack()
    {
        if (PlayerTargeting.Instance == null)
            return;

        Transform target = PlayerTargeting.Instance.currentTarget;
        if (target == null)
            return;

        // 사거리 체크
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > attackRange)
            return;

        // 쿨타임 체크
        if (Time.time < nextFireTime)
            return;

        // 이미 공격 중이면 중복 실행 방지
        if (isAttacking)
            return;

        StartCoroutine(AttackRoutine(target));
    }

    IEnumerator AttackRoutine(Transform target)
    {
        isAttacking = true;

        // 이동 멈추고 조준 상태로 전환
        var move = PlayerMovement.Instance;
        if (move != null)
        {
            move.isAiming = true;       // 조준 플래그
            move.isAttacking = true;    // 이동 잠금
        }

        // 살짝 에이밍 포즈 잡는 시간 (원하는 만큼 조절)
        yield return new WaitForSeconds(0.1f);

        // 실제 발사
        Shoot(target);

        // 다음 발사 시간 설정
        nextFireTime = Time.time + (1f / fireRate);

        // 공격 종료 후 이동 잠금 해제
        if (move != null)
        {
            move.isAttacking = false;
            // 타겟이 계속 있다면 isAiming 유지하고 싶으면 그대로 두고,
            // 공격 후 바로 에이밍 해제하고 싶다면 아래 줄도 추가
            // move.isAiming = false;
        }

        isAttacking = false;
    }

    void Shoot(Transform target)
    {
        // 공격 애니메이션
        if (anim != null)
            anim.SetTrigger("Attack");

        // 총구 이펙트
        if (muzzleFlash != null)
            muzzleFlash.Play();

        // 레이캐스트 시작 위치
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
        Vector3 dir = (target.position + Vector3.up * 1f - origin).normalized;

        RaycastHit hit;

        // ★ Enemy + Wall(장애물)이 포함된 shootMask로 Raycast
        if (Physics.Raycast(origin, dir, out hit, attackRange, shootMask))
        {
            // 1) 먼저 맞은 게 Enemy인지 확인
            EnemyHealth hp = hit.collider.GetComponent<EnemyHealth>();
            if (hp != null)
            {
                // 벽에 가려지지 않고 적이 “첫 번째로” 맞았다 → 데미지
                hp.TakeDamage(damage);
            }
            else
            {
                // hp == null 이면 벽/기둥 같은 장애물에 먼저 맞은 것 → 그냥 막힌 것
                // 필요하면 여기서 스파크 이펙트 등 재생 가능
                // Debug.Log("벽에 막혀서 못 맞춤 : " + hit.collider.name);
            }
        }
    }
}
