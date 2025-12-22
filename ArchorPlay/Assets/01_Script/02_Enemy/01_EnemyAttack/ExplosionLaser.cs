using System.Collections;
using UnityEngine;

// 3. 폭발 레이저 공격
public class ExplosionLaser : MonoBehaviour
{
    [SerializeField] private LineRenderer warningLine;       // 경고 라인
    [SerializeField] private LineRenderer laserLine;         // 실제 레이저 라인
    [SerializeField] private ParticleSystem laserParticle;   // 레이저 파티클
    [SerializeField] private ParticleSystem explosionParticle; // 폭발 파티클
    [SerializeField] private float warningDuration = 1.5f;   // 경고 표시 시간
    [SerializeField] private float lineDisappearSpeed = 2f;  // 경고 라인 축소 속도
    [SerializeField] private float laserDuration = 0.3f;     // 레이저 지속 시간
    [SerializeField] private float explosionRadius = 5f;     // 폭발 범위
    [SerializeField] private float explosionDamage = 50f;    // 폭발 기본 데미지
    [SerializeField] private LayerMask wallLayer;            // 레이저 충돌(벽) 레이어
    [SerializeField] private LayerMask damageLayer;          // 피해 적용 레이어

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        warningLine.enabled = false;
        laserLine.enabled = false;
    }

    public IEnumerator ExecuteAttack()
    {
        Vector3 direction = (player.position - transform.position).normalized;

        // 레이캐스트로 충돌 지점(벽) 계산
        RaycastHit hit;
        Vector3 endPoint;
        if (Physics.Raycast(transform.position, direction, out hit, 100f, wallLayer))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = transform.position + direction * 100f;
        }

        // 경고 표시
        warningLine.enabled = true;
        warningLine.positionCount = 2;
        warningLine.SetPosition(0, transform.position);
        warningLine.SetPosition(1, endPoint);

        yield return new WaitForSeconds(warningDuration);

        // 경고 라인을 줄이며 사라지게 연출
        yield return StartCoroutine(ShrinkWarningLine());

        // 실제 레이저 발사
        laserLine.enabled = true;
        laserLine.positionCount = 2;
        laserLine.SetPosition(0, transform.position);
        laserLine.SetPosition(1, endPoint);

        if (laserParticle != null)
        {
            laserParticle.transform.position = transform.position;
            laserParticle.transform.LookAt(endPoint);
            laserParticle.Play();
        }

        yield return new WaitForSeconds(laserDuration);

        // 폭발
        if (explosionParticle != null)
        {
            explosionParticle.transform.position = endPoint;
            explosionParticle.Play();
        }

        ApplyExplosionDamage(endPoint);

        laserLine.enabled = false;
        yield return new WaitForSeconds(1f);
    }

    // 경고 라인을 시작점까지 수축시키는 연출
    IEnumerator ShrinkWarningLine()
    {
        Vector3 startPos = warningLine.GetPosition(0);
        Vector3 endPos = warningLine.GetPosition(1);

        float distance = Vector3.Distance(startPos, endPos);
        float shrinkTime = distance / lineDisappearSpeed;
        float elapsed = 0f;

        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkTime;
            warningLine.SetPosition(1, Vector3.Lerp(endPos, startPos, t));
            yield return null;
        }

        warningLine.enabled = false;
    }

    // 폭발 범위 내 대상에게 데미지 적용
    void ApplyExplosionDamage(Vector3 explosionPos)
    {
        Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius, damageLayer);

        foreach (Collider col in colliders)
        {
            // 데미지 처리 대상(IDamageable)만 적용
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(explosionPos, col.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                damageable.TakeDamage(explosionDamage * damageMultiplier);
            }
        }
    }
}