using UnityEngine;
using UnityEngine.UI;

public class CellUI : MonoBehaviour
{
    [SerializeField] private Image fillimage;

    public void SetFill(float v)
    {
        fillimage.fillAmount = Mathf.Clamp01(v);
    }
}
