using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoyStickMovement : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static JoyStickMovement Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<JoyStickMovement>();
                if (instance == null)
                {
                    var go = new GameObject("JoyStickMovement");
                    instance = go.AddComponent<JoyStickMovement>();
                }
            }
            return instance;
        }
    }
    private static JoyStickMovement instance;

    [Header("UI")]
    public RectTransform handle;   // 안쪽 흰색 원(핸들)

    // 외부에서 읽을 이동 벡터(-1 ~ 1)
    public Vector2 joyVec;
    public bool isPlayerMoving = false;

    RectTransform bg;              // 바깥 원 (이 스크립트가 붙어 있는 오브젝트)
    Canvas canvas;
    Camera uiCamera;
    float radius;
    Vector2 startBgPos;            // BG 원래 자리

    void Awake()
    {
        bg = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            uiCamera = null;
        else
            uiCamera = canvas.worldCamera;
    }

    void Start()
    {
        // 반지름 계산 (지름 / 2)
        radius = bg.sizeDelta.y * 0.5f;
        startBgPos = bg.anchoredPosition;

        // 처음엔 중앙에
        handle.anchoredPosition = Vector2.zero;
        joyVec = Vector2.zero;
    }

    // 눌렀을 때
    public void OnPointerDown(PointerEventData eventData)
    {
        // 조이스틱 배경을 누른 위치로 이동
        bg.position = eventData.position;
        handle.anchoredPosition = Vector2.zero;

        isPlayerMoving = true;
    }

    // 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        // 화면 좌표 → BG 로컬 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bg, eventData.position, uiCamera, out localPoint))
        {
            // 반지름 안쪽으로 클램프
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);

            // 핸들 위치 이동
            handle.anchoredPosition = clamped;

            // -1 ~ 1 범위의 입력 벡터
            joyVec = clamped / radius;
        }
    }

    // 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        joyVec = Vector2.zero;
        isPlayerMoving = false;

        // BG와 핸들을 원래 자리로
        bg.anchoredPosition = startBgPos;
        handle.anchoredPosition = Vector2.zero;
    }
}
