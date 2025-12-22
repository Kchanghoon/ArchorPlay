using System.Collections;
using UnityEngine;

public class StraightAttack : MonoBehaviour
{
    [SerializeField] private LineRenderer warningLine;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float maxDistance = 50f;

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        warningLine.positionCount = 2;
        warningLine.enabled = false;
    }

    public IEnumerator ExecuteAttack()
    {
        Vector3 direction = (player.position - transform.position).normalized;

        // 경고선 표시
        warningLine.enabled = true;
        warningLine.SetPosition(0, transform.position);
        warningLine.SetPosition(1, transform.position + direction * maxDistance);

        yield return new WaitForSeconds(warningDuration);
        warningLine.enabled = false;

        // 발사체 발사
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }

        Destroy(projectile, 5f);
    }
}