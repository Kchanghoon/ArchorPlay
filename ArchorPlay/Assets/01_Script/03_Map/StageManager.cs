using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 관리 및 랜덤 이동 시스템
/// </summary>
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
        public string stageName;              // 스테이지 이름
        public Transform spawnPoint;          // 플레이어 스폰 위치
        public StageType type;                // 스테이지 타입
    }

    public enum StageType
    {
        Normal,     // 일반 스테이지
        Angel,      // 천사 스테이지 (특수)
        Boss        // 보스 스테이지
    }
    #endregion

    #region Serialized Fields
    [Header("Stage Configuration")]
    [SerializeField] private List<StageInfo> normalStages = new List<StageInfo>();
    [SerializeField] private List<StageInfo> angelStages = new List<StageInfo>();
    [SerializeField] private List<StageInfo> bossStages = new List<StageInfo>();

    [Header("Stage Settings")]
    [SerializeField] private int totalStages = 20;           // 전체 스테이지 수
    [SerializeField] private int angelStageInterval = 10;    // 천사 스테이지 주기 (10, 20, 30...)
    [SerializeField] private int lastStageIndex = 20;        // 마지막 보스 스테이지

    [Header("References")]
    [SerializeField] private Transform player;
    #endregion

    #region Private Fields
    private int currentStage = 0;
    private List<int> usedNormalStageIndices = new List<int>();
    private List<int> usedAngelStageIndices = new List<int>();
    private List<int> usedBossStageIndices = new List<int>();
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

        // 첫 스테이지 시작
        MoveToNextStage();
    }
    #endregion

    #region Initialization
    private void InitializePlayer()
    {
        if (player == null)
        {
            if (PlayerMovement.Instance != null)
            {
                player = PlayerMovement.Instance.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
        }
    }
    #endregion

    #region Stage Progression
    /// <summary>
    /// 다음 스테이지로 이동
    /// </summary>
    public void MoveToNextStage()
    {
        currentStage++;

        // 마지막 스테이지 체크
        if (currentStage > lastStageIndex)
        {
            Debug.Log("Game Clear!");
            OnGameClear();
            return;
        }

        // 스테이지 타입 결정
        StageType nextStageType = DetermineStageType();

        // 스테이지 이동
        MoveToStage(nextStageType);
    }

    /// <summary>
    /// 스테이지 타입 결정
    /// </summary>
    private StageType DetermineStageType()
    {
        // 마지막 스테이지 = 보스
        if (currentStage == lastStageIndex)
        {
            return StageType.Boss;
        }

        // 10의 배수 = 천사 스테이지
        if (currentStage % angelStageInterval == 0)
        {
            return StageType.Angel;
        }

        // 나머지 = 일반 스테이지
        return StageType.Normal;
    }

    /// <summary>
    /// 특정 타입의 스테이지로 이동
    /// </summary>
    private void MoveToStage(StageType type)
    {
        StageInfo selectedStage = null;

        switch (type)
        {
            case StageType.Normal:
                selectedStage = GetRandomStage(normalStages, usedNormalStageIndices);
                break;

            case StageType.Angel:
                selectedStage = GetRandomStage(angelStages, usedAngelStageIndices);
                break;

            case StageType.Boss:
                selectedStage = GetRandomStage(bossStages, usedBossStageIndices);
                break;
        }

        if (selectedStage != null && selectedStage.spawnPoint != null)
        {
            TeleportPlayer(selectedStage.spawnPoint.position);
            Debug.Log($"Stage {currentStage}/{totalStages}: Moved to {selectedStage.stageName} ({type})");
        }
        else
        {
            Debug.LogError($"Failed to load stage! Type: {type}");
        }
    }

    /// <summary>
    /// 랜덤 스테이지 선택 (중복 방지)
    /// </summary>
    private StageInfo GetRandomStage(List<StageInfo> stageList, List<int> usedIndices)
    {
        if (stageList == null || stageList.Count == 0)
        {
            Debug.LogError("Stage list is empty!");
            return null;
        }

        // 모든 스테이지를 사용했으면 초기화
        if (usedIndices.Count >= stageList.Count)
        {
            usedIndices.Clear();
        }

        // 사용 가능한 인덱스 찾기
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < stageList.Count; i++)
        {
            if (!usedIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }

        // 랜덤 선택
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        usedIndices.Add(randomIndex);

        return stageList[randomIndex];
    }
    #endregion

    #region Player Movement
    /// <summary>
    /// 플레이어 텔레포트
    /// </summary>
    private void TeleportPlayer(Vector3 position)
    {
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        // NavMeshAgent가 있다면 일시적으로 비활성화
        UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 위치 이동
        player.position = position;

        // NavMeshAgent 재활성화
        if (agent != null)
        {
            agent.enabled = true;
        }

        Debug.Log($"Player teleported to: {position}");
    }
    #endregion

    #region Game Events
    /// <summary>
    /// 스테이지 클리어 (모든 적 처치 시 호출)
    /// </summary>
    public void OnStageClear()
    {
        Debug.Log($"Stage {currentStage} Clear!");

        // 다음 스테이지로 이동
        MoveToNextStage();
    }

    /// <summary>
    /// 게임 클리어
    /// </summary>
    private void OnGameClear()
    {
        Debug.Log("All stages cleared! Game Complete!");
        // TODO: 게임 클리어 UI 표시, 엔딩 등
    }
    #endregion

    #region Debug
    /// <summary>
    /// 특정 스테이지로 강제 이동 (디버그용)
    /// </summary>
    public void DebugMoveToStage(int stageNumber)
    {
        currentStage = stageNumber - 1;
        MoveToNextStage();
    }

    /// <summary>
    /// Gizmos로 스폰 포인트 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        // Normal stages
        Gizmos.color = Color.green;
        foreach (var stage in normalStages)
        {
            if (stage.spawnPoint != null)
            {
                Gizmos.DrawWireSphere(stage.spawnPoint.position, 0.5f);
            }
        }

        // Angel stages
        Gizmos.color = Color.cyan;
        foreach (var stage in angelStages)
        {
            if (stage.spawnPoint != null)
            {
                Gizmos.DrawWireSphere(stage.spawnPoint.position, 0.7f);
            }
        }

        // Boss stages
        Gizmos.color = Color.red;
        foreach (var stage in bossStages)
        {
            if (stage.spawnPoint != null)
            {
                Gizmos.DrawWireSphere(stage.spawnPoint.position, 1f);
            }
        }
    }
    #endregion
}