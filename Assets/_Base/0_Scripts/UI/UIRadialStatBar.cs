using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 원형(Radial) 스탯 바.
/// Image.Type = Filled + FillMethod = Vertical + FillOrigin = Bottom 설정된
/// Image 컴포넌트의 fillAmount를 0~1로 제어한다.
///
/// Inspector에서:
///   fillImage  : Type=Filled, FillMethod=Vertical, FillOrigin=Bottom으로 설정한 Image
///   valueText  : 퍼센트 텍스트 (선택, 기존 텍스트 재활용 가능)
/// </summary>
public class UIRadialStatBar : MonoBehaviour
{
    [Header("채움 이미지 (Type=Filled, Vertical, Bottom)")]
    [SerializeField] private Image fillImage;

    [Header("수치 텍스트 (선택)")]
    [SerializeField] private TMP_Text valueText;

    // ── 갱신 ─────────────────────────────────────────────────────────────

    /// <summary>0~1 사이의 값으로 fillAmount와 텍스트를 갱신한다.</summary>
    public void SetValue(float value01)
    {
        float clamped = Mathf.Clamp01(value01);

        if (fillImage != null)
            fillImage.fillAmount = clamped;

        if (valueText != null)
            valueText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }
}
