using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerMovement>();
                if (instance == null)
                {
                    var instanceContainer = new GameObject("PlayerMovement");
                    instance = instanceContainer.AddComponent<PlayerMovement>();
                }
            }
            return instance;
        }
    }
    private static PlayerMovement instance;

    [Header("Weapon / Arsenal")]
    public Transform rightGunBone;
    public Transform leftGunBone;
    public Arsenal[] arsenal;

    Rigidbody rb;
    public float moveSpeed = 5f;
    public Animator Anim;

    [HideInInspector] public bool isAttacking = false;
    // 필요하면 나중에 사용할 값들
    public float walkThreshold = 0.4f;
    public float backDotThreshold = -0.3f;

    public bool isDead = false;
    public bool isAiming = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Anim = GetComponent<Animator>();

        if (arsenal != null && arsenal.Length > 0)
        {
            SetArsenal(arsenal[0].name);
        }
    }

    // 임시 확인용 키보드 1~4
    void Update()
    {
        HandleWeaponSwitchInput();
    }

    void HandleWeaponSwitchInput()
    {
        // 같은 오브젝트에 붙어 있는 PlayerAttack 가져오기
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (arsenal.Length > 0)
            {
                SetArsenal(arsenal[0].name);
                attack.bulletType = BulletType.Pistol;        // 권총
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (arsenal.Length > 1)
            {
                SetArsenal(arsenal[1].name);
                attack.bulletType = BulletType.DualPistol;    // 쌍권총
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (arsenal.Length > 2)
            {
                SetArsenal(arsenal[2].name);
                attack.bulletType = BulletType.Sniper;        // 저격총
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (arsenal.Length > 3)
            {
                SetArsenal(arsenal[3].name);
                // 4번은 아직 타입 미정이면 일단 권총으로 두거나, 새 타입 추가해서 쓰면 됩니다.
                attack.bulletType = BulletType.Pistol;
            }
        }
    }



    void FixedUpdate()
    {
        Vector3 input = JoyStickMovement.Instance.joyVec;

        // 죽었을 때
        if (isDead)
        {
            rb.linearVelocity = Vector3.zero;
            SetAnimDead();
            return;
        }
        // 공격 중
        if (isAttacking)
        {
            rb.linearVelocity = Vector3.zero;
            SetAnimAim();        // Speed 0 + Aiming true
            return;
        }
        // 입력 없을 때
        if (input.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;

            if (isAiming)
                SetAnimAim();   // 조준 + Speed 0
            else
                SetAnimIdle();  // Idle (Speed 0)

            return;
        }

        // ─────────────────────────────────────
        //  이동 방향 계산
        // ─────────────────────────────────────
        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;

        // 물리 이동
        rb.linearVelocity = moveDir * moveSpeed;

        // ─────────────────────────────────────
        //  회전 처리
        // ─────────────────────────────────────
        if (isAiming &&
            PlayerTargeting.Instance != null &&
            PlayerTargeting.Instance.currentTarget != null)
        {
            // 에이밍 중 + 타겟 존재 → 회전은 PlayerTargeting에서 처리
        }
        else
        {
            // 에이밍이 아니면 이동 방향을 향해 회전
            transform.DOKill();
            transform.DOLookAt(transform.position + moveDir, 0.15f)
                     .SetEase(Ease.OutQuad);
        }

        // 애니메이션
        UpdateMoveAnimation(moveDir, input.magnitude);
    }

    [System.Serializable]
    public struct Arsenal
    {
        public string name;
        public GameObject rightGun;
        public GameObject leftGun;
        public RuntimeAnimatorController controller;
    }

    /// <summary>
    /// 이동 관련 애니메이션 업데이트
    /// Speed(float), Aiming(bool)만 사용
    /// </summary>
    void UpdateMoveAnimation(Vector3 moveDir, float inputMag)
    {
        // 현재 속도 크기를 Speed 파라미터로 전달
        float speedValue = rb.linearVelocity.magnitude;
        Anim.SetFloat("Speed", speedValue);

        // 조준 여부
        Anim.SetBool("Aiming", isAiming);
    }

    void SetAnimIdle()
    {
        Anim.SetFloat("Speed", 0f);     // 멈춤 → Idle
        Anim.SetBool("Aiming", false);  // 조준 해제
    }

    void SetAnimDead()
    {
        Anim.SetFloat("Speed", 0f);
        Anim.SetBool("Death", true);    // Animator에 있는 Death(bool)
    }

    void SetAnimAim()
    {
        Anim.SetFloat("Speed", 0f);     // 제자리 조준
        Anim.SetBool("Aiming", true);
    }

    public void SetArsenal(string name)
    {
        foreach (Arsenal hand in arsenal)
        {
            if (hand.name == name)
            {
                // 기존 무기 제거
                if (rightGunBone != null && rightGunBone.childCount > 0)
                    Destroy(rightGunBone.GetChild(0).gameObject);

                if (leftGunBone != null && leftGunBone.childCount > 0)
                    Destroy(leftGunBone.GetChild(0).gameObject);

                // 오른손 무기
                if (hand.rightGun != null && rightGunBone != null)
                {
                    GameObject newRightGun = Instantiate(hand.rightGun);
                    newRightGun.transform.SetParent(rightGunBone);
                    newRightGun.transform.localPosition = Vector3.zero;
                    newRightGun.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }

                // 왼손 무기
                if (hand.leftGun != null && leftGunBone != null)
                {
                    GameObject newLeftGun = Instantiate(hand.leftGun);
                    newLeftGun.transform.SetParent(leftGunBone);
                    newLeftGun.transform.localPosition = Vector3.zero;
                    newLeftGun.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }

                // 애니메이터 컨트롤러 교체
                if (hand.controller != null && Anim != null)
                {
                    Anim.runtimeAnimatorController = hand.controller;
                }

                return;
            }
        }
    }
}
