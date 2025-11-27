using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    public BulletType type;      // 이 총알의 풀 타입
    public int damage;
    public LayerMask hitMask;    // Enemy + Obstacle
    public float lifeTime = 2f;

    float timer;

    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            BulletPool.Instance.Despawn(type, gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 맞아야 할 레이어인지 체크
        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        // Enemy면 데미지
        EnemyHealth hp = other.GetComponent<EnemyHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
        }

        // 벽이든 적이든 맞으면 풀로 복귀
        BulletPool.Instance.Despawn(type, gameObject);
    }
}
