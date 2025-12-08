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
            ? enemyRoot.GetComponentsInChildren<EnemyHealth>(includeInactive: false)
            : FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        var alive = enemies.Where(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy).ToArray();
        if (alive.Length == 0)
            enemiesCleared = true; // 이제 클리어 포인트로 가면 클리어
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stageCleared) return;
        if (clearPointTrigger == null) return;
        if (!other.CompareTag(playerTag)) return;

        if (!enemiesCleared)
        {
            Debug.Log("Cannot clear: enemies remain.");
            return; // 적이 남아 있으면 클리어 불가
        }

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
}