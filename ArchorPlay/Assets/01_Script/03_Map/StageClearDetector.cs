using UnityEngine;
using System.Linq;

/// <summary>
/// 스테이지 클리어 조건 감지 (모든 적 처치)
/// </summary>
public class StageClearDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float checkInterval = 1f;  // 체크 주기

    private float nextCheckTime;
    private bool stageCleared = false;

    private void Update()
    {
        // 이미 클리어했으면 체크 안함
        if (stageCleared)
            return;

        // 일정 간격으로 체크
        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;

        // 적이 모두 죽었는지 체크
        CheckEnemies();
    }

    /// <summary>
    /// 살아있는 적 확인
    /// </summary>
    private void CheckEnemies()
    {
        // Scene의 모든 EnemyHealth 컴포넌트 찾기
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        // 살아있는 적 필터링
        var aliveEnemies = enemies.Where(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy).ToArray();

        Debug.Log($"Alive enemies: {aliveEnemies.Length}");

        // 모든 적이 죽었으면 스테이지 클리어
        if (aliveEnemies.Length == 0)
        {
            OnStageClear();
        }
    }

    /// <summary>
    /// 스테이지 클리어 처리
    /// </summary>
    private void OnStageClear()
    {
        stageCleared = true;
        Debug.Log("Stage Clear!");

        // StageManager에 알림
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageClear();
        }
    }

    /// <summary>
    /// 새 스테이지 시작 시 리셋
    /// </summary>
    public void ResetForNewStage()
    {
        stageCleared = false;
    }
}