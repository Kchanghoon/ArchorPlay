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
        public Transform spawnPoint;
        public Transform clearPoint; // 스테이지별 클리어 포인트 위치
        public Transform enemyRoot;  // [MOD] 스테이지별 적 루트(전멸 체크용)
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
    #endregion

    #region Private Fields
    private int currentStage = 0;
    private Queue<StageType> pendingStageTypes = new Queue<StageType>();

    private List<int> usedLv1Normal = new List<int>();
    private List<int> usedLv2Normal = new List<int>();
    private List<int> usedAngel = new List<int>();
    private List<int> usedBoss = new List<int>();
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
                bool isLv1 = currentStage <= 10;
                selectedStage = GetRandomStage(
                    isLv1 ? lv1NormalStages : lv2NormalStages,
                    isLv1 ? usedLv1Normal : usedLv2Normal);
                break;
            case StageType.Angel:
                selectedStage = GetRandomStage(angelStages, usedAngel);
                break;
            case StageType.Boss:
                selectedStage = GetRandomStage(bossStages, usedBoss);
                break;
        }

        if (selectedStage != null && selectedStage.spawnPoint != null)
        {
            TeleportPlayer(selectedStage.spawnPoint.position);
            PositionClearPointTrigger(selectedStage.clearPoint, selectedStage.enemyRoot); // [MOD] enemyRoot 전달
            Debug.Log($"Stage {currentStage}/{totalStages}: {selectedStage.stageName} ({type})");
        }
        else
        {
            Debug.LogError($"Failed to load stage! Type: {type}");
        }
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
        {
            if (!usedIndices.Contains(i))
                availableIndices.Add(i);
        }

        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        usedIndices.Add(randomIndex);
        return stageList[randomIndex];
    }
    #endregion

    #region Clear Point Placement
    private void PositionClearPointTrigger(Transform clearPoint, Transform enemyRoot)
    {
        clearPointTrigger.gameObject.SetActive(false);
        clearPointTrigger.ResetForNewStage();

        clearPointTrigger.SetEnemyRoot(enemyRoot);
        Debug.Log($"[StageManager] Inject enemyRoot={(enemyRoot ? enemyRoot.name : "NULL")}");

        if (clearPoint != null)
        {
            clearPointTrigger.transform.SetPositionAndRotation(clearPoint.position, clearPoint.rotation);
            clearPointTrigger.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Player Movement
    private void TeleportPlayer(Vector3 position)
    {
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        player.position = position;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (agent != null) agent.enabled = true;

        Debug.Log($"Player teleported to: {position}");
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
            if (s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 0.5f);

        foreach (var s in lv2NormalStages)
            if (s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 0.5f);

        Gizmos.color = Color.cyan;
        foreach (var s in angelStages)
            if (s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 0.7f);

        Gizmos.color = Color.red;
        foreach (var s in bossStages)
            if (s.spawnPoint) Gizmos.DrawWireSphere(s.spawnPoint.position, 1f);
    }
    #endregion
}