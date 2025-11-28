using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 플레이어 이동 및 무기 관리 컴포넌트
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Singleton
    public static PlayerMovement Instance { get; private set; }

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

    #region Serialized Fields
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationDuration = 0.15f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private Transform rightGunBone;
    [SerializeField] private Transform leftGunBone;
    [SerializeField] private Animator animator;
    [SerializeField] private Arsenal[] arsenals;

    [Header("Dependencies")]
    [SerializeField] private PlayerTargeting targeting;
    [SerializeField] private JoyStickMovement joystick;
    #endregion

    #region Private Fields
    private Rigidbody rb;
    private PlayerState currentState = PlayerState.Idle;

    // Animation Parameter IDs (성능 최적화)
    private int speedParamID;
    private int aimingParamID;
    private int deathParamID;
    private int attackParamID;
    #endregion

    #region Properties
    public PlayerState CurrentState => currentState;

    public bool IsMoving => rb != null && rb.linearVelocity.sqrMagnitude > movementThreshold;

    public bool IsDead => currentState == PlayerState.Dead;

    public bool IsAiming => currentState == PlayerState.Aiming || currentState == PlayerState.Attacking;
    #endregion

    #region Events
    public event Action<WeaponType> OnWeaponChanged;
    public event Action<PlayerState, PlayerState> OnStateChanged;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeComponents();
        CacheAnimationParameters();
        InitializeWeapon();
    }

    private void Update()
    {
        HandleWeaponSwitchInput();
    }

    private void FixedUpdate()
    {
        if (currentState == PlayerState.Dead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        HandleMovement();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (targeting == null)
            targeting = GetComponent<PlayerTargeting>();

        if (joystick == null)
            joystick = JoyStickMovement.Instance;
    }

    private void CacheAnimationParameters()
    {
        if (animator == null) return;

        speedParamID = Animator.StringToHash("Speed");
        aimingParamID = Animator.StringToHash("Aiming");
        deathParamID = Animator.StringToHash("Death");
        attackParamID = Animator.StringToHash("Attack");
    }

    private void InitializeWeapon()
    {
        if (arsenals == null || arsenals.Length == 0)
        {
            Debug.LogError("⚠️ Arsenals array is not set up! Please configure weapons in Inspector.");
            return;
        }

        // 기본 무기를 Hand(맨손)로 설정
        SetArsenal(WeaponType.Hand);
    }
    #endregion

    #region State Management
    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        PlayerState previousState = currentState;
        OnStateExit(currentState);
        currentState = newState;
        OnStateEnter(newState);

        OnStateChanged?.Invoke(previousState, newState);
    }

    private void OnStateEnter(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                UpdateAnimation(0f, false);
                break;

            case PlayerState.Moving:
                break;

            case PlayerState.Aiming:
                UpdateAnimation(0f, true);
                break;

            case PlayerState.Attacking:
                UpdateAnimation(0f, true);
                break;

            case PlayerState.Dead:
                UpdateAnimation(0f, false);
                if (animator != null)
                    animator.SetBool(deathParamID, true);
                break;
        }
    }

    private void OnStateExit(PlayerState state)
    {
        // 필요시 상태 종료 처리
    }

    public void Die()
    {
        SetState(PlayerState.Dead);
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        if (joystick == null) return;

        Vector3 input = joystick.joyVec;

        // 공격 중에는 이동 불가
        if (currentState == PlayerState.Attacking)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // 입력이 없을 때
        if (input.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;

            // 타겟이 있으면 조준 상태, 없으면 Idle
            if (targeting != null && targeting.CurrentTarget != null)
            {
                SetState(PlayerState.Aiming);
            }
            else
            {
                SetState(PlayerState.Idle);
            }
            return;
        }

        // 이동 처리
        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;
        rb.linearVelocity = moveDir * moveSpeed;

        SetState(PlayerState.Moving);

        // 회전 처리
        HandleRotation(moveDir);

        // 애니메이션 업데이트
        UpdateAnimation(rb.linearVelocity.magnitude, false);
    }

    private void HandleRotation(Vector3 moveDir)
    {
        // 조준 중이고 타겟이 있으면 타겟팅 클래스에서 회전 처리
        if (IsAiming && targeting != null && targeting.CurrentTarget != null)
        {
            // PlayerTargeting에서 처리
            return;
        }

        // 이동 방향으로 회전
        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.DOKill();
            transform.DOLookAt(transform.position + moveDir, rotationDuration)
                     .SetEase(Ease.OutQuad);
        }
    }
    #endregion

    #region Animation
    private void UpdateAnimation(float speed, bool isAiming)
    {
        if (animator == null) return;

        animator.SetFloat(speedParamID, speed);
        animator.SetBool(aimingParamID, isAiming);
    }

    public void TriggerAttackAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(attackParamID);
    }
    #endregion

    #region Weapon Management
    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetArsenal(WeaponType.Hand);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetArsenal(WeaponType.Pistol);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetArsenal(WeaponType.DualPistol);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetArsenal(WeaponType.Sniper);
        }
    }

    public void SetArsenal(WeaponType weaponType)
    {
        int index = (int)weaponType;

        // 배열 유효성 체크
        if (arsenals == null || arsenals.Length == 0)
        {
            Debug.LogError("Arsenals array is empty! Please set up weapons in Inspector.");
            return;
        }

        if (index < 0 || index >= arsenals.Length)
        {
            Debug.LogWarning($"Arsenal index {index} ({weaponType}) out of range! Available: 0-{arsenals.Length - 1}");
            return;
        }

        Arsenal arsenal = arsenals[index];

        // 무기 이름 확인
        if (string.IsNullOrEmpty(arsenal.name))
        {
            Debug.LogWarning($"Arsenal at index {index} has no name!");
        }

        Debug.Log($"Switching to weapon: {weaponType} (Index: {index})");

        // 기존 무기 제거
        ClearWeapons();

        // 새 무기 장착
        EquipWeapon(arsenal);

        // 애니메이터 컨트롤러 교체
        if (arsenal.controller != null && animator != null)
        {
            animator.runtimeAnimatorController = arsenal.controller;
            Debug.Log($"Animator controller changed to: {arsenal.controller.name}");
        }
        else if (arsenal.controller == null)
        {
            Debug.LogWarning($"No animator controller assigned for {weaponType}!");
        }

        // 이벤트 발생
        OnWeaponChanged?.Invoke(weaponType);
    }

    private void ClearWeapons()
    {
        if (rightGunBone != null && rightGunBone.childCount > 0)
            Destroy(rightGunBone.GetChild(0).gameObject);

        if (leftGunBone != null && leftGunBone.childCount > 0)
            Destroy(leftGunBone.GetChild(0).gameObject);
    }

    private void EquipWeapon(Arsenal arsenal)
    {
        // 오른손 무기
        if (arsenal.rightGun != null && rightGunBone != null)
        {
            GameObject newRightGun = Instantiate(arsenal.rightGun, rightGunBone);
            newRightGun.transform.localPosition = Vector3.zero;
            newRightGun.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        // 왼손 무기
        if (arsenal.leftGun != null && leftGunBone != null)
        {
            GameObject newLeftGun = Instantiate(arsenal.leftGun, leftGunBone);
            newLeftGun.transform.localPosition = Vector3.zero;
            newLeftGun.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
    }
    #endregion

    #region Data Structures
    [System.Serializable]
    public struct Arsenal
    {
        public string name;
        public GameObject rightGun;
        public GameObject leftGun;
        public RuntimeAnimatorController controller;
    }
    #endregion
}