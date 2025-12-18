using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HPBar : MonoBehaviour
{
    [Header("HP 설정")]
    [SerializeField] private int maxHp = 1000;
    [SerializeField] private int currentHp = 1000;
    [SerializeField] private int hpPerCell = 250;

    [Header("UI 설정")]
    [SerializeField] private CellUI cellPrefab;          // 칸 프리팹 (Fill Image 포함)
    [SerializeField] private RectTransform containerRect; // HP바 전체 폭 고정용
    [SerializeField] private GridLayoutGroup grid;        // 1행 고정 + cellSize 자동계산
    [SerializeField] private float spacingX = 10f;
    [SerializeField] private float cellHeight = 60f;
    [Header("Text설정")]
    [SerializeField] private TextMeshProUGUI hpText;

    private readonly List<CellUI> cells = new();

    private void Awake()
    {
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            grid.spacing = new Vector2(spacingX, grid.spacing.y);
        }
    }

    private void Start()
    {
        RebuildCellsIfNeeded();
        UpdateFillOnly();
    }

    // HP 감소: "칸 개수 변경 없음", 내부 fill만 감소
    public void TakeDamage(int damage)
    {
        currentHp = Mathf.Max(0, currentHp - damage);
        UpdateFillOnly();
    }

    // HP 회복: "칸 개수 변경 없음", 내부 fill만 증가
    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        UpdateFillOnly();
    }

    // 최대 HP 증가: 칸 개수가 늘어날 수 있음 (폭은 유지, 칸 폭만 재계산)
    public void IncreaseMaxHp(int amount)
    {
        maxHp += Mathf.Max(0, amount);
        currentHp = Mathf.Min(maxHp, currentHp + amount); // 원하시는 정책에 맞게 조정 가능
        RebuildCellsIfNeeded();
        UpdateFillOnly();
    }

    // 최대 HP 감소: 칸 개수가 줄어들 수 있음
    public void DecreaseMaxHp(int amount)
    {
        maxHp = Mathf.Max(hpPerCell, maxHp - Mathf.Max(0, amount));
        currentHp = Mathf.Min(currentHp, maxHp);
        RebuildCellsIfNeeded();
        UpdateFillOnly();
    }

    private int RequiredCellCount()
    {
        return Mathf.CeilToInt(maxHp / (float)hpPerCell);
    }

    private void RebuildCellsIfNeeded()
    {
        int need = RequiredCellCount();

        while (cells.Count < need)
        {
            var cell = Instantiate(cellPrefab, grid.transform);
            cells.Add(cell);
        }

        while (cells.Count > need)
        {
            int last = cells.Count - 1;
            Destroy(cells[last].gameObject);
            cells.RemoveAt(last);
        }

        UpdateCellSize(need);
    }

    // "HP바 크기 동일"을 위한 핵심: 컨테이너 폭 안에 칸들을 꽉 채우도록 칸 폭 자동 계산
    private void UpdateCellSize(int cellCount)
    {
        if (grid == null || containerRect == null || cellCount <= 0) return;

        float totalWidth = containerRect.rect.width;
        float totalSpacing = spacingX * Mathf.Max(0, cellCount - 1);
        float cellWidth = (totalWidth - totalSpacing) / cellCount;

        grid.cellSize = new Vector2(Mathf.Max(1f, cellWidth), cellHeight);
    }

    // 감소/회복은 여기만 호출: "칸 내부 체력만" 줄어듦
    private void UpdateFillOnly()
    {
        if (cells.Count == 0) return;

        for (int i = 0; i < cells.Count; i++)
        {
            int start = i * hpPerCell;
            int end = (i + 1) * hpPerCell;

            float fill;
            if (currentHp <= start) fill = 0f;
            else if (currentHp >= end) fill = 1f;
            else fill = (currentHp - start) / (float)hpPerCell;

            cells[i].SetFill(fill);
        }
        UpdateHpText();
    }

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    private void UpdateHpText()
    {
        if (hpText == null) return;
        hpText.text = $"{currentHp} / {maxHp}";
    }

}

