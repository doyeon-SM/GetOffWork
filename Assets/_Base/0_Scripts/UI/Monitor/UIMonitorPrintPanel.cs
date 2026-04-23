using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모니터 출력 선택 화면 패널
/// - 조회 결과 표시 / Print 버튼 / Back / 닫기
/// </summary>
public class UIMonitorPrintPanel : MonoBehaviour
{
    [Header("조회 결과")]
    [SerializeField] private Image    portraitImage;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text birthText;

    [Header("SFX")]
    [SerializeField] private AudioClip printSFX;

    private UIMonitorController controller;

    public void Init(UIMonitorController ctrl)
    {
        controller = ctrl;
    }

    // ── 버튼 핸들러 (Inspector에서 연결) ─────────────────────────────────

    public void OnClickPrint()
    {
        if (printSFX != null && SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.PlaySfxOneShot(printSFX);
        controller?.OnPrint();
    }

    public void OnClickRejectAddressMismatch()
    {
        controller?.OnRejectAddressMismatch();
    }

    public void OnClickBack()
    {
        controller?.GoToMain();
    }

    public void OnClickClose()
    {
        controller?.Close();
    }

    // ── 뷰 갱신 (Controller가 호출) ──────────────────────────────────────

    public void RefreshView(UserRecordData record)
    {
        if (record == null) { ClearView(); return; }

        if (portraitImage != null) portraitImage.sprite = record.portrait;
        if (idText        != null) idText.text          = record.recordId;
        if (nameText      != null) nameText.text        = record.fullName;
        if (addressText   != null) addressText.text     = record.address;
        if (birthText     != null) birthText.text       = record.birthDate;
    }

    public void ClearView()
    {
        if (portraitImage != null) portraitImage.sprite = null;
        if (idText        != null) idText.text          = string.Empty;
        if (nameText      != null) nameText.text        = string.Empty;
        if (addressText   != null) addressText.text     = string.Empty;
        if (birthText     != null) birthText.text       = string.Empty;
    }
}
