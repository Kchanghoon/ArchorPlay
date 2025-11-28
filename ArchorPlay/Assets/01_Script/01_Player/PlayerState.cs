using UnityEngine;

/// <summary>
/// 플레이어 상태 정의
/// </summary>
public enum PlayerState
{
    Idle,       // 대기
    Moving,     // 이동 중
    Aiming,     // 조준 중
    Attacking,  // 공격 중
    Dead        // 사망
}

/// <summary>
/// 무기 타입 정의
/// </summary>
public enum WeaponType
{
    Hand,
    Pistol,
    DualPistol,
    Sniper
}

/// <summary>
/// 탄환 타입 정의
/// </summary>
public enum BulletType
{
    Hand,
    Pistol,
    DualPistol,
    Sniper
}