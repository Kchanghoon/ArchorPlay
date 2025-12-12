using System.Linq;
using UnityEngine;

public class StageClearDetector : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Collider clearPointTrigger;   // 공용 트리거 콜라이더

    [Header("Enemy Detection")]
    [SerializeField] private bool filterByParent = true;   // true면 enemyRoot 하위 적만 카운트
    [SerializeField] private Transform enemyRoot;          // 비우면 this.transform
    [SerializeField] private float checkInterval = 1f;

    private float nextCheckTime;
    private bool enemiesCleared = false;
    private bool stageCleared = false;

    private void Reset()
    {
        enemyRoot = transform;
    }

    private void Awake()
    {
        if (clearPointTrigger == null)
            clearPointTrigger = GetComponent<Collider>();
        if (enemyRoot == null)
            enemyRoot = transform;
    }

    private void OnEnable()
    {
        enemiesCleared = false;
        stageCleared = false;
    }

    private void Update()
    {
        if (stageCleared || enemiesCleared) return;
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;
        CheckEnemies();
    }

    private void CheckEnemies()
    {
        EnemyHealth[] enemies = filterByParent
            ? enemyRoot.GetComponentsInChildren<EnemyHealth>(false)
            : FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        int aliveCount = enemies.Count(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy);
        Debug.Log($"[CheckEnemies] root={(enemyRoot ? enemyRoot.name : "null")} total={enemies.Length} alive={aliveCount}");

        if (aliveCount == 0) enemiesCleared = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (stageCleared) return;

        // 자식 콜라이더로 들어오는 경우가 많아서 root로 판정합니다
        if (!other.transform.root.CompareTag(playerTag)) return;

        if (!enemiesCleared)
        {
            Debug.Log("Cannot clear: enemies remain.");
            return;
        }
        Debug.Log($"Trigger Enter: other={other.name}, tag={other.tag}, root={other.transform.root.name}, rootTag={other.transform.root.tag}");

        OnStageClear();
    }

    private void OnStageClear()
    {
        stageCleared = true;
        Debug.Log("Stage Clear (enemies dead + clear point)!");

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageClear();
        else
            Debug.LogError("StageManager not found!");
    }

    public void ResetForNewStage()
    {
        enemiesCleared = false;
        stageCleared = false;
        nextCheckTime = 0f;
    }

    public void SetEnemyRoot(Transform newRoot)
    {
        enemyRoot = newRoot != null ? newRoot : transform;
        Debug.Log($"[StageClearDetector] enemyRoot set to: {enemyRoot.name}");
    }

}