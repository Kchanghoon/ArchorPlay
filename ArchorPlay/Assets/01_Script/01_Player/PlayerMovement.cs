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

    Rigidbody rb;
    public float moveSpeed = 5f;
    public Animator Anim;

    public float walkThreshold = 0.4f;
    public float backDotThreshold = -0.3f;
    public bool isDead = false;
    public bool isAiming = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Anim = GetComponent<Animator>();
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

        // 입력 없을 때
        if (input.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;

            if (isAiming)
                SetAnimAim();
            else
                SetAnimIdle();

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
            // 여기서는 회전하지 않음
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

    void UpdateMoveAnimation(Vector3 moveDir, float inputMag)
    {
        ResetMoveBools();

        if (inputMag < walkThreshold)
            Anim.SetBool("Walk", true);
        else
            Anim.SetBool("Run", true);
    }

    void SetAnimIdle()
    {
        ResetMoveBools();
        Anim.SetBool("Idle", true);
    }

    void SetAnimDead()
    {
        ResetMoveBools();
        Anim.SetBool("Dead", true);
    }

    void ResetMoveBools()
    {
        Anim.SetBool("Idle", false);
        Anim.SetBool("Run", false);
        Anim.SetBool("Dead", false);
        Anim.SetBool("Walk", false);
        Anim.SetBool("Aiming", false);
    }

    void SetAnimAim()
    {
        ResetMoveBools();
        Anim.SetBool("Aiming", true);
    }
}
