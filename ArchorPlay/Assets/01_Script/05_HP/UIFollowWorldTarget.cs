using UnityEngine;

public class UIFollowWorldTarget : MonoBehaviour
{
    [SerializeField] private Transform target;         // 플레이어
    [SerializeField] private Vector3 worldOffset;      // 머리 위로 띄우기 (예: (0, 2.0f, 0))
    [SerializeField] private Vector2 screenOffset;     // 화면 픽셀 오프셋 (예: (0, 30))
    [SerializeField] private Canvas canvas;            // Overlay Canvas
    [SerializeField] private Camera worldCamera;       // MainCamera
    [SerializeField] private bool hideWhenBehind = true;

    private RectTransform rect;

    private void Awake()
    {
        rect = (RectTransform)transform;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null || canvas == null || worldCamera == null) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        if (hideWhenBehind)
        {
            bool behind = screenPos.z < 0f;
            if (behind)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            else
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
            }
        }

        // Overlay Canvas는 ScreenPoint가 그대로 UI 좌표가 아닙니다.
        // RectTransformUtility로 캔버스 로컬 좌표로 변환합니다.
        RectTransform canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPoint
        );

        rect.anchoredPosition = localPoint + screenOffset;
    }
}
