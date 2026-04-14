using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원형 타이머 UI.
///
/// [구성 요소 (Inspector 연결)]
/// - trailImage     : Image (Type=Filled, FillMethod=Radial360, FillOrigin=Top, Clockwise=true)
///                    시침이 지나간 자리 색상. handImage와 동일 설정.
/// - clockText      : TMP_Text — "MM:SS" 숫자 표시 (선택)
/// - lunchLabel     : TMP_Text — "점심시간" 레이블 (선택)
///
/// [동작 원리]
/// - 남은 시간 / 최대 시간 = ratio (1.0 → 0.0)
/// - handImage.fillAmount  = ratio          (시침 위치 = 아직 남은 시간)
/// - trailImage.fillAmount = 1 - ratio      (지나간 자리 = 경과한 시간)
/// - 시침은 12시 방향(Top)에서 시계방향으로 회전하므로
///   ratio=1.0일 때 시침이 꽉 차있고(= 시작), ratio=0.0일 때 완전히 비워짐(= 종료).
/// </summary>
public class UIClockTimer : MonoBehaviour
{

    [Tooltip("시침이 지나간 자리 색상 이미지. handImage와 동일 설정.")]
    [SerializeField] private Image trailImage;

    [Header("색상 설정")]
    [Tooltip("시침 색상")]
    [SerializeField] private Color handColor   = Color.white;
    [Tooltip("시침이 지나간 자리 색상")]
    [SerializeField] private Color trailColor  = new Color(0.2f, 0.6f, 1f, 1f);
    [Tooltip("점심시간 색상")]
    [SerializeField] private Color lunchColor  = new Color(1f, 0.8f, 0.2f, 1f);
    [Tooltip("종료 색상")]
    [SerializeField] private Color finishColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("텍스트 (선택)")]
    [Tooltip("MM:SS 텍스트. 없으면 표시 안 함.")]
    [SerializeField] private TMP_Text clockText;
    [Tooltip("점심시간 레이블. 없으면 표시 안 함.")]
    [SerializeField] private TMP_Text lunchLabel;

    // ── 초기화 ────────────────────────────────────────────────────────────

    private void Awake()
    {
        SetImageColors(handColor, trailColor);
        ShowLunchLabel(false);
    }

    // ── 공개 API (WorkDayManager가 호출) ─────────────────────────────────

    /// <summary>
    /// 매 프레임 WorkDayManager.UpdateClockUI()에서 호출.
    /// </summary>
    /// <param name="remaining">남은 시간(초)</param>
    /// <param name="total">페이즈 최대 시간(초)</param>
    public void Tick(float remaining, float total)
    {
        ShowLunchLabel(false);
        SetImageColors(handColor, trailColor);

        float ratio = (total > 0f) ? Mathf.Clamp01(remaining / total) : 0f;

        // trailImage: 경과 시간 비율 (0→1 증가)
        if (trailImage != null) trailImage.fillAmount = 1f - ratio;

        // 텍스트 갱신
        if (clockText != null)
            clockText.text = FormatTime(remaining);
    }

    /// <summary>점심시간 상태로 전환.</summary>
    public void SetLunchBreak()
    {
        ShowLunchLabel(true);
        SetImageColors(lunchColor, lunchColor);
        if (trailImage != null) trailImage.fillAmount = 0f;
        if (clockText  != null) clockText.text        = string.Empty;
    }

    /// <summary>종료 상태로 전환.</summary>
    public void SetFinished()
    {
        ShowLunchLabel(false);
        SetImageColors(finishColor, finishColor);
        if (trailImage != null) trailImage.fillAmount = 1f;
        if (clockText  != null) clockText.text        = "00:00";
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────

    private void SetImageColors(Color hand, Color trail)
    {
        if (trailImage != null) trailImage.color = trail;
    }

    private void ShowLunchLabel(bool show)
    {
        if (lunchLabel != null) lunchLabel.gameObject.SetActive(show);
    }

    private static string FormatTime(float time)
    {
        int total = Mathf.CeilToInt(time);
        if (total < 0) total = 0;
        return $"{total / 60:00}:{total % 60:00}";
    }
}
