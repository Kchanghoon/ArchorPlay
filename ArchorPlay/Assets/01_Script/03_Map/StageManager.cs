using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StageManager : MonoBehaviour
{
    #region Singleton
    public static StageManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    #region Stage Data
    [System.Serializable]
    public class StageInfo
    {
        public string stageName;

        [Header("Room Root (B안)")]
        public Transform roomRoot;                 // 방 전체 루트(맵+EnemyRoot 포함). 여기만 활성화

        [Header("Player / Clear")]
        public Transform spawnPoint;
        public Transform clearPoint;

        [Header("Enemy")]
        public Transform enemyRoot;                // 이 방의 적 부모(전멸 체크/스폰 부모)
        public Transform[] enemySpawnPoints;       // 적 스폰 포인트 여러 개

        public StageType type;
    }

    public enum StageType { Normal, Angel, Boss }
    #endregion

    #region Serialized Fields
    [Header("Stage Configuration")]
    [SerializeField] private List<StageInfo> lv1NormalStages = new List<StageInfo>(); // 1~10
    [SerializeField] private List<StageInfo> lv2NormalStages = new List<StageInfo>(); // 11~20
    [SerializeField] private List<StageInfo> angelStages = new List<StageInfo>();
    [SerializeField] private List<StageInfo> bossStages = new List<StageInfo>();

    [Header("Stage Settings")]
    [SerializeField] private int totalStages = 20;
    [SerializeField] private int lastStageIndex = 20;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private StageClearDetector clearPointTrigger; // 공용 트리거

    [Header("Enemy Spawn")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int minEnemyCount = 3;
    [SerializeField] private int maxEnemyCount = 6;
    [SerializeField] private bool clearOldEnemiesInThisRoot = true;
    [SerializeField] private bool uniqueSpawnPointUntilExhausted = true; // 스폰포인트를 섞고 순환 사용
    #endregion

    #region Private Fields
    private int currentStage = 0;
    private Queue<StageType> pendingStageTypes = new Queue<StageType>();

    private List<int> usedLv1Normal = new List<int>();
    private List<int> usedLv2Normal = new List<int>();
    private List<int> usedAngel = new List<int>();
    private List<int> usedBoss = new List<int>();

    private StageInfo currentStageInfo; // 현재 활성 스테이지(방)
    #endregion

    #region Properties
    public int CurrentStage => currentStage;
    public int TotalStages => totalStages;
    public bool IsLastStage => currentStage >= lastStageIndex;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializePlayer();
        MoveToNextStage();
    }
    #endregion

    #region Initialization
    private void InitializePlayer()
    {
        if (player != null) return;

        if (PlayerMovement.Instance != null)
        {
            player = PlayerMovement.Instance.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }
    #endregion

    #region Stage Progression
    public void MoveToNextStage()
    {
        // 큐가 비었을 때만 스테이지 증가 및 큐 생성
        if (pendingStageTypes.Count == 0)
        {
            currentStage++;

            if (currentStage > lastStageIndex)
            {
                Debug.Log("Game Clear!");
                OnGameClear();
                return;
            }

            EnqueueStageTypesForCurrentStage();
        }

        StageType nextStageType = pendingStageTypes.Dequeue();
        MoveToStage(nextStageType);
    }

    private void EnqueueStageTypesForCurrentStage()
    {
        // Angel: 5, 10, 15
        if (currentStage == 5 || currentStage == 10 || currentStage == 15)
            pendingStageTypes.Enqueue(StageType.Angel);

        // Boss: 10, 20
        if (currentStage == 10 || currentStage == 20)
            pendingStageTypes.Enqueue(StageType.Boss);

        // 아무 것도 없으면 Normal
        if (pendingStageTypes.Count == 0)
            pendingStageTypes.Enqueue(StageType.Normal);
    }

    private void MoveToStage(StageType type)
    {
        StageInfo selectedStage = null;

        switch (type)
        {
            case StageType.Normal:
                {
                    bool isLv1 = currentStage <= 10;
                    selectedStage = GetRandomStage(
                        isLv1 ? lv1NormalStages : lv2NormalStages,
                        isLv1 ? usedLv1Normal : usedLv2Normal
                    );
                    break;
                }
            case StageType.Angel:
                selectedStage = GetRandomStage(angelStages, usedAngel);
                break;

            case StageType.Boss:
                selectedStage = GetRandomStage(bossStages, usedBoss);
                break;
        }

        if (selectedStage == null)
        {
            Debug.LogError($"Failed to load stage! Type: {type}");
            return;
        }

        if (selectedStage.spawnPoint == null)
        {
            Debug.LogError($"[StageManager] spawnPoint is null: {selectedStage.stageName}");
            return;
        }

        // 1) 현재 방만 활성화 (B안)
        currentStageInfo = selectedStage;
        SetActiveOnly(currentStageInfo);

        // 2) 코루틴으로 플레이어 이동 (한 프레임 대기 후)
        StartCoroutine(TeleportPlayerDelayed(currentStageInfo.spawnPoint.position));

        // 3) 클리어 트리거 위치 + 전멸 체크 루트 주입
        PositionClearPointTrigger(currentStageInfo.clearPoint, currentStageInfo.enemyRoot);

        // 4) 적 스폰 (현재 enemyRoot 아래만)
        SpawnEnemiesForStage(currentStageInfo);

        Debug.Log($"Stage {currentStage}/{totalStages}: {currentStageInfo.stageName} ({type})");
    }

    // 새로운 코루틴 메서드
    private IEnumerator TeleportPlayerDelayed(Vector3 position)
    {
        yield return null;

        if (player == null)
        {
            Debug.LogError("❌ Player is null!");
            yield break;
        }

        // PlayerMovement의 Teleport 메서드 호출
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.Teleport(position);
        }
        else
        {
            Debug.LogError("❌ PlayerMovement component not found!");
        }

        yield return new WaitForFixedUpdate();
    }

    private StageInfo GetRandomStage(List<StageInfo> stageList, List<int> usedIndices)
    {
        if (stageList == null || stageList.Count == 0)
        {
            Debug.LogError("Stage list is empty!");
            return null;
        }

        if (usedIndices.Count >= stageList.Count)
            usedIndices.Clear();

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < stageList.Count; i++)
            if (!usedIndices.Contains(i))
                availableIndices.Add(i);

        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        usedIndices.Add(randomIndex);
        return stageList[randomIndex];
    }
    #endregion

    #region B안: Room 활성화
    private void SetActiveOnly(StageInfo current)
    {
        void Apply(List<StageInfo> list)
        {
            foreach (var s in list)
            {
                if (s == null) continue;
                if (s.roomRoot == null) continue;

                bool active = (s == current);
                if (s.roomRoot.gameObject.activeSelf != active)
                    s.roomRoot.gameObject.SetActive(active);
            }
        }

        Apply(lv1NormalStages);
        Apply(lv2NormalStages);
        Apply(angelStages);
        Apply(bossStages);
    }
    #endregion

    #region Enemy Spawn
    private void SpawnEnemiesForStage(StageInfo stage)
    {
        if (stage.enemyRoot == null)
        {
            Debug.LogError($"[StageManager] enemyRoot is null: {stage.stageName}");
            return;
        }

        if (stage.enemySpawnPoints == null || stage.enemySpawnPoints.Length == 0)
        {
            Debug.LogError($"[StageManager] enemySpawnPoints is empty: {stage.stageName}");
            return;
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("[StageManager] enemyPrefabs is empty");
            return;
        }

        if (minEnemyCount > maxEnemyCount)
            (minEnemyCount, maxEnemyCount) = (maxEnemyCount, minEnemyCount);

        if (clearOldEnemiesInThisRoot)
            ClearChildren(stage.enemyRoot);

        int count = Random.Range(minEnemyCount, maxEnemyCount + 1);

        // 스폰 포인트 섞기 (중복 최소화 용)
        var points = new List<Transform>(stage.enemySpawnPoints);
        Shuffle(points);

        for (int i = 0; i < count; i++)
        {
            Transform pt = uniqueSpawnPointUntilExhausted
                ? points[i % points.Count]
                : stage.enemySpawnPoints[Random.Range(0, stage.enemySpawnPoints.Length)];

            if (pt == null) continue;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null) continue;

            Instantiate(prefab, pt.position, pt.rotation, stage.enemyRoot);
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void ClearChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }
    #endregion

    #region Clear Point Placement
    private void PositionClearPointTrigger(Transform clearPoint, Transform enemyRoot)
    {
        if (clearPointTrigger == null)
        {
            Debug.LogError("[StageManager] clearPointTrigger not assigned");
            return;
        }

        clearPointTrigger.gameObject.SetActive(false);
        clearPointTrigger.ResetForNewStage();

        // 전멸 체크는 이 enemyRoot만 보게
        clearPointTrigger.SetEnemyRoot(enemyRoot);
        Debug.Log($"[StageManager] Inject enemyRoot={(enemyRoot ? enemyRoot.name : "NULL")}");

        if (clearPoint != null)
        {
            clearPointTrigger.transform.SetPositionAndRotation(clearPoint.position, clearPoint.rotation);
            clearPointTrigger.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[StageManager] clearPoint is null: {currentStageInfo?.stageName}");
        }
    }
    #endregion

    #region Player Movement
    private void TeleportPlayer(Vector3 position)
    {
        if (player == null)
        {
            Debug.LogError("❌ Player not found!");
            return;
        }

        Debug.Log($"🚀 Attempting teleport to: {position}");

        Rigidbody rb = player.GetComponent<Rigidbody>();

        // 1) Rigidbody 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2) 직접 Transform으로 이동
        player.position = position;

        Debug.Log($"✅ Player teleported! New position: {player.position}");
    }

    private System.Collections.IEnumerator WarpAfterFrame(NavMeshAgent agent, Vector3 position)
    {
        yield return null; // 한 프레임 대기

        if (agent != null && agent.enabled)
        {
            agent.Warp(position);
            agent.ResetPath();
        }
    }

    #endregion

    #region Game Events
    public void OnStageClear()
    {
        Debug.Log($"Stage {currentStage} Clear!");
        MoveToNextStage();
    }

    private void OnGameClear()
    {
        Debug.Log("All stages cleared! Game Complete!");
        // TODO: 게임 클리어 UI 등
    }
    #endregion

    #region Debug
    public void DebugMoveToStage(int stageNumber)
    {
        currentStage = stageNumber - 1;
        pendingStageTypes.Clear();
        MoveToNextStage();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var s in lv1NormalStages)
            if (s != null && s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 0.5f);

        foreach (var s in lv2NormalStages)
            if (s != null && s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 0.5f);

        Gizmos.color = Color.cyan;
        foreach (var s in angelStages)
            if (s != null && s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 0.7f);

        Gizmos.color = Color.red;
        foreach (var s in bossStages)
            if (s != null && s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 1f);
    }
    #endregion
}
