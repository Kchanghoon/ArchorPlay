using UnityEngine;
using System.Collections;

// 공격 타입 열거형
public enum AttackType
{
    Straight,        // 직선 공격
    ThreeCushion,    // 3쿠션(반사) 공격
    ExplosionLaser   // 폭발 레이저 공격
}

// EnemyAttackManager - 적의 공격을 관리
public class EnemyAttackManager : MonoBehaviour
{
    [Header("Attack Selection")]
    [SerializeField] private AttackType[] attackPattern; // 공격 패턴 배열
    [SerializeField] private float attackCooldown = 3f;  // 공격 간 쿨다운

    [Header("Attack Components")]
    [SerializeField] private StraightAttack straightAttack;
    [SerializeField] private ThreeCushionAttack threeCushionAttack;
    [SerializeField] private ExplosionLaser explosionLaser;

    private int currentAttackIndex = 0;
    private bool isAttacking = false;

    void Start()
    {
        // 컴포넌트 자동 할당(Inspector에 없을 때)
        if (straightAttack == null) straightAttack = GetComponent<StraightAttack>();
        if (threeCushionAttack == null) threeCushionAttack = GetComponent<ThreeCushionAttack>();
        if (explosionLaser == null) explosionLaser = GetComponent<ExplosionLaser>();
    }

void Update()
{
    if (Input.GetKeyDown(KeyCode.Alpha4))
        StartCoroutine(ExecuteAttackByIndex(0));
    if (Input.GetKeyDown(KeyCode.Alpha5))
        StartCoroutine(ExecuteAttackByIndex(1));
    if (Input.GetKeyDown(KeyCode.Alpha6))
        StartCoroutine(ExecuteAttackByIndex(2));
}

    // 다음 공격 실행(패턴 순환)
    public IEnumerator ExecuteNextAttack()
    {
        if (attackPattern == null || attackPattern.Length == 0)
        {
            Debug.LogWarning("공격 패턴이 설정되지 않았습니다!");
            yield break;
        }

        isAttacking = true;

        AttackType currentAttack = attackPattern[currentAttackIndex];
        yield return StartCoroutine(ExecuteAttack(currentAttack));

        // 다음 공격 인덱스로 이동(순환)
        currentAttackIndex = (currentAttackIndex + 1) % attackPattern.Length;

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    // 특정 공격 타입 실행
    public IEnumerator ExecuteAttack(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Straight:
                if (straightAttack != null)
                {
                    yield return StartCoroutine(straightAttack.ExecuteAttack());
                }
                else
                {
                    Debug.LogError("StraightAttack 컴포넌트가 없습니다!");
                }
                break;

            case AttackType.ThreeCushion:
                if (threeCushionAttack != null)
                {
                    yield return StartCoroutine(threeCushionAttack.ExecuteAttack());
                }
                else
                {
                    Debug.LogError("ThreeCushionAttack 컴포넌트가 없습니다!");
                }
                break;

            case AttackType.ExplosionLaser:
                if (explosionLaser != null)
                {
                    yield return StartCoroutine(explosionLaser.ExecuteAttack());
                }
                else
                {
                    Debug.LogError("ExplosionLaser 컴포넌트가 없습니다!");
                }
                break;
        }
    }

    // 특정 인덱스의 공격 실행
    public IEnumerator ExecuteAttackByIndex(int index)
    {
        if (attackPattern == null || attackPattern.Length == 0)
        {
            Debug.LogWarning("공격 패턴이 설정되지 않았습니다!");
            yield break;
        }

        if (index < 0 || index >= attackPattern.Length)
        {
            Debug.LogError($"잘못된 인덱스: {index}");
            yield break;
        }

        isAttacking = true;
        yield return StartCoroutine(ExecuteAttack(attackPattern[index]));
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    // 랜덤 공격 실행
    public IEnumerator ExecuteRandomAttack()
    {
        if (attackPattern == null || attackPattern.Length == 0)
        {
            Debug.LogWarning("공격 패턴이 설정되지 않았습니다!");
            yield break;
        }

        isAttacking = true;
        int randomIndex = Random.Range(0, attackPattern.Length);
        yield return StartCoroutine(ExecuteAttack(attackPattern[randomIndex]));
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    // 현재 공격 중인지 확인
    public bool IsAttacking()
    {
        return isAttacking;
    }
}



// 데미지를 받을 수 있는 대상 인터페이스(예시)
public interface IDamageable
{
    void TakeDamage(float damage);
}
