using UnityEngine;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Info")]
    public string stageName;
    public StageType stageType;

    [Header("Map Settings")]
    [Tooltip("맵의 프리팹을 여기에 넣으세요")]
    public GameObject mapPrefab;

    [Tooltip("맵 내에서 플레이어가 스폰될 위치 (맵 원점 기준 오프셋)")]
    public Vector3 spawnOffset = new Vector3(0, 1, 0);
}

// StageType은 StageManager에서 여기로 옮기거나, 별도 파일로 관리해도 됩니다.
public enum StageType
{
    Normal,
    Angel,
    Boss
}