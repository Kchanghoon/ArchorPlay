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

    // 애니메이션용 설정값
    public float walkThreshold = 0.4f;   // 이 값보다 작으면 Walk, 크면 Run
    public float backDotThreshold = -0.3f; // -1 ~ 1 / 이 값보다 작으면 BackWalk
    public bool isDead = false;          // 죽었을 때 외부에서 true로 세팅

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
            SetAnimIdle();
            return;
        }

        // 이동 방향
        Vector3 moveDir = new Vector3(input.x, 0, input.y);

        // 물리 이동
        rb.linearVelocity = moveDir * moveSpeed;

        // 회전
        transform.DOKill();
        transform.DOLookAt(transform.position + moveDir, 0.15f)
                 .SetEase(Ease.OutQuad);

        // 애니메이션 업데이트
        UpdateMoveAnimation(moveDir, input.magnitude);
    }

    void UpdateMoveAnimation(Vector3 moveDir, float inputMag)
    {
        // 방향 비교 (캐릭터 앞쪽 기준)
        Vector3 forward = transform.forward;
        Vector3 dirNorm = moveDir.normalized;

        float dot = Vector3.Dot(forward, dirNorm);
        // dot: 1 = 정면, 0 = 옆, -1 = 정반대(뒤)

        ResetMoveBools();


        // 앞/옆으로 움직이는데 속도에 따라 Walk / Run 구분
        if (inputMag < walkThreshold)
        {
            Anim.SetBool("Walk", true);
        }
        else
        {
            Anim.SetBool("Run", true);
        }
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
    }
}
