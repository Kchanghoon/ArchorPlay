using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;

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

        // ⭐ NavMeshAgent 즉시 비활성화 (가장 먼저 처리)
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
            Debug.Log("✅ NavMeshAgent disabled in Awake");
        }
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

    [SerializeField] private HPBar healtbar;
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

    public bool IsMoving
    {
        get
        {
            if (rb == null) return false;
            // Y축을 제외한 수평 이동만 체크
            float horizontalSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).sqrMagnitude;
            return horizontalSpeed > movementThreshold;
        }
    }
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

        // HP 테스트 키 (디버깅용)
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Insert))
        {
            TakeDamage(100);
            Debug.Log("테스트: 데미지 -100");
        }
        if (Input.GetKeyDown(KeyCode.Home))
        {
            Heal(100);
            Debug.Log("테스트: 회복 +100");
        }
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            IncreaseMaxHealth(250);
            Debug.Log("테스트: 최대HP +250");
        }
#endif
    }

    private void FixedUpdate()
    {
        if (currentState == PlayerState.Dead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        HandleMovement();

        // 안전장치: Y축 속도가 비정상적으로 생기면 제거
        if (Mathf.Abs(rb.linearVelocity.y) > 0.01f)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0,
                rb.linearVelocity.z
            );
        }
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Debug.Log($"✅ RB Init - Kinematic: {rb.isKinematic}, Pos: {transform.position}");
        }
        else
        {
            Debug.LogError("❌ Rigidbody component not found!");
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (targeting == null)
            targeting = GetComponent<PlayerTargeting>();

        if (joystick == null)
        {
            joystick = JoyStickMovement.Instance;
            if (joystick == null)
            {
                Debug.LogError("❌ JoyStickMovement.Instance is NULL!");
            }
        }
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
        Debug.Log("Die() 호출됨");
        SetState(PlayerState.Dead);
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        if (joystick == null)
        {
            Debug.LogWarning("Joystick is null!");
            return;
        }

        Vector3 input = joystick.joyVec;

        // 공격 중에는 이동 불가
        if (currentState == PlayerState.Attacking)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // 현재 Y 위치 저장 (높이 고정용)
        float currentY = transform.position.y;

        // 입력이 없을 때
        if (input.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;

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

        // Y축 속도는 항상 0으로
        rb.linearVelocity = new Vector3(
            moveDir.x * moveSpeed,
            0,
            moveDir.z * moveSpeed
        );

        SetState(PlayerState.Moving);

        // 회전 처리
        HandleRotation(moveDir);

        // 애니메이션 업데이트
        float horizontalSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        UpdateAnimation(horizontalSpeed, false);

        // Y 위치 강제 보정 (벽 충돌 시 떠오름 방지)
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.y - currentY) > 0.05f)
        {
            pos.y = currentY;
            transform.position = pos;
        }
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

    public void Teleport(Vector3 position)
    {
        Debug.Log($"🚀 Teleport called: {position}");

        if (rb != null)
        {
            // 물리 초기화
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Kinematic으로 임시 변경
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;

            // 위치 설정
            transform.position = position;

            // 한 프레임 후 Kinematic 복원
            StartCoroutine(RestoreKinematicAfterFrame(wasKinematic));
        }
        else
        {
            transform.position = position;
        }

        Debug.Log($"✅ Teleported to: {transform.position}");
    }

    private IEnumerator RestoreKinematicAfterFrame(bool wasKinematic)
    {
        yield return new WaitForFixedUpdate();
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
 


        if (healtbar != null)
        {
            healtbar.TakeDamage(damage);

            // HP가 0이 되면 사망 처리
            if (healtbar.IsDead)
            {
                Die();
            }
        }
    }

    // 회복 메서드 추가
    public void Heal(int amount)
    {
        if (IsDead) return;

        if (healtbar != null)
        {
            healtbar.Heal(amount);
        }
    }

    // 최대 HP 증가 메서드 추가
    public void IncreaseMaxHealth(int amount)
    {
        if (healtbar != null)
        {
            healtbar.IncreaseMaxHp(amount);
        }
    }

}