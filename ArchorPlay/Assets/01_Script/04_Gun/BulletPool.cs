using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [System.Serializable]
    public class PoolConfig
    {
        public BulletType type;       // 권총/쌍권총/저격총
        public GameObject prefab;     // 해당 타입의 총알 프리팹
        public int size = 20;         // 초기 생성 개수

        [HideInInspector] public Queue<GameObject> queue;
    }

    public PoolConfig[] pools;

    Dictionary<BulletType, PoolConfig> poolDict;

    void Awake()
    {
        Instance = this;
        poolDict = new Dictionary<BulletType, PoolConfig>();

        foreach (var p in pools)
        {
            p.queue = new Queue<GameObject>();

            for (int i = 0; i < p.size; i++)
            {
                GameObject obj = Instantiate(p.prefab);
                obj.SetActive(false);
                p.queue.Enqueue(obj);
            }

            poolDict[p.type] = p;
        }
    }

    public GameObject Spawn(BulletType type, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.TryGetValue(type, out var pool))
        {
            Debug.LogWarning($"[BulletPool] 풀에 {type} 타입이 없습니다.");
            return null;
        }

        GameObject obj;
        if (pool.queue.Count > 0)
        {
            obj = pool.queue.Dequeue();
        }
        else
        {
            // 부족하면 추가 생성 (필요 없으면 이 부분 제거 가능)
            obj = Instantiate(pool.prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Despawn(BulletType type, GameObject obj)
    {
        if (!poolDict.TryGetValue(type, out var pool))
        {
            Debug.LogWarning($"[BulletPool] 반환 시 {type} 타입을 찾을 수 없습니다.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        pool.queue.Enqueue(obj);
    }
}
