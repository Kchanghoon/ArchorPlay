using System.Collections;
using UnityEngine;

public class ThreeCushionAttack : MonoBehaviour
{
    [SerializeField] private LineRenderer warningLine;       // 경고 라인(예상 경로)
    [SerializeField] private GameObject projectilePrefab;    // 발사체 프리팹
    [SerializeField] private float warningDuration = 1.5f;   // 경고 표시 시간
    [SerializeField] private float projectileSpeed = 8f;     // 발사체 속도
    [SerializeField] private int maxBounces = 3;             // 최대 반사 횟수
    [SerializeField] private float rayDistance = 100f;       // 레이캐스트 거리
    [SerializeField] private LayerMask wallLayer;            // 벽 레이어(반사 대상)

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        warningLine.enabled = false;
    }

    public IEnumerator ExecuteAttack()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3[] points = CalculateBouncePoints(transform.position, direction);

        // 경고 표시(예상 궤적)
        warningLine.enabled = true;
        warningLine.positionCount = points.Length;
        warningLine.SetPositions(points);

        yield return new WaitForSeconds(warningDuration);
        warningLine.enabled = false;

        // 발사체 발사 후 경로 따라 이동
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        StartCoroutine(MoveProjectileAlongPath(projectile, points));
    }

    // 반사 포인트 계산
    Vector3[] CalculateBouncePoints(Vector3 startPos, Vector3 startDir)
    {
        Vector3[] points = new Vector3[maxBounces + 2];
        points[0] = startPos;

        Vector3 currentPos = startPos;
        Vector3 currentDir = startDir;
        int bounceCount = 0;

        for (int i = 1; i <= maxBounces + 1; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(currentPos, currentDir, out hit, rayDistance, wallLayer))
            {
                points[i] = hit.point;
                currentPos = hit.point + hit.normal * 0.01f;   // 벽에 붙어서 다시 맞는 것 방지
                currentDir = Vector3.Reflect(currentDir, hit.normal);
                bounceCount++;

                if (bounceCount >= maxBounces)
                {
                    System.Array.Resize(ref points, i + 1);
                    break;
                }
            }
            else
            {
                points[i] = currentPos + currentDir * rayDistance;
                System.Array.Resize(ref points, i + 1);
                break;
            }
        }

        return points;
    }

    // 계산된 포인트 경로를 따라 발사체 이동
    IEnumerator MoveProjectileAlongPath(GameObject projectile, Vector3[] path)
    {
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null) yield break;

        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 direction = (path[i + 1] - path[i]).normalized;
            rb.linearVelocity = direction * projectileSpeed;

            while (Vector3.Distance(projectile.transform.position, path[i + 1]) > 0.5f)
            {
                yield return null;
            }
        }

        Destroy(projectile, 2f);
    }
}