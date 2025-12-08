using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CameraMovement>();
                if (instance == null)
                {
                    var go = new GameObject("CameraMovement");
                    instance = go.AddComponent<CameraMovement>();
                }
            }
            return instance;
        }
    }
    private static CameraMovement instance;

    [Header("Follow Target")]
    [SerializeField] private Transform player;

    [Header("Top-Down Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 45f, -40f); // 위에서 내려보는 위치

    [Header("Follow Settings")]
    [SerializeField] private float followLerp = 10f; // 0이면 순간 이동, 높을수록 부드럽게

    [Header("Camera Rotation (Fixed)")]
    [SerializeField] private Vector3 eulerAngles = new Vector3(60f, 0f, 0f); // 탑뷰 각도 고정

    private void Awake()
    {
        // 싱글톤 유지
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        // 초기 회전 고정
        transform.rotation = Quaternion.Euler(eulerAngles);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + offset;
        if (followLerp <= 0f)
            transform.position = targetPos;
        else
            transform.position = Vector3.Lerp(transform.position, targetPos, followLerp * Time.deltaTime);

        // 회전 고정 유지
        transform.rotation = Quaternion.Euler(eulerAngles);
    }

    /// <summary>
    /// 외부에서 플레이어 할당 (예: StageManager에서 스폰 후)
    /// </summary>
    public void SetPlayer(Transform target, bool snapNow = true)
    {
        player = target;
        if (snapNow) SnapToPlayer();
    }

    /// <summary>
    /// 맵/스테이지 전환 시 카메라를 즉시 맞추기
    /// </summary>
    public void SnapToPlayer()
    {
        if (player == null) return;
        transform.position = player.position + offset;
        transform.rotation = Quaternion.Euler(eulerAngles);
    }
}