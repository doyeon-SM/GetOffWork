using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모니터 메인 화면 패널
/// - ID 입력 / 조회 / PrintSelect / MobileSelect / 결과 표시 / 닫기
/// </summary>
public class UIMonitorMainPanel : MonoBehaviour
{
    [Header("검색")]
    [SerializeField] private TMP_InputField idInputField;

    [Header("조회 결과")]
    [SerializeField] private Image    portraitImage;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;

    private UIMonitorController controller;

    public void Init(UIMonitorController ctrl)
    {
        controller = ctrl;
    }

    // ── 버튼 핸들러 (Inspector에서 연결) ─────────────────────────────────

    public void OnClickSearch()
    {
        if (controller == null || idInputField == null) return;
        controller.OnSearch(idInputField.text);
    }

    public void OnClickSelectPrint()
    {
        controller?.OnSelectPrint();
    }

    public void OnClickSelectMobile()
    {
        controller?.OnSelectMobile();
    }

    public void OnClickClose()
    {
        controller?.Close();
    }

    // ── 뷰 갱신 (Controller가 호출) ──────────────────────────────────────

    public void RefreshView(UserRecordData record)
    {
        if (record == null) 
        {
            if (idText != null) idText.text = "정보없음";
            if (nameText != null) nameText.text = "정보없음";
            if (addressText != null) addressText.text = "정보없음";

            //ClearView(); 
            return; 
        }

        if (portraitImage != null) portraitImage.sprite = record.portrait;
        if (idText        != null) idText.text          = record.recordId;
        if (nameText      != null) nameText.text        = record.fullName;
        if (addressText   != null) addressText.text     = record.address;
    }

    public void ClearView()
    {
        if (idInputField  != null) idInputField.text    = string.Empty;
        if (portraitImage != null) portraitImage.sprite = null;
        if (idText        != null) idText.text          = string.Empty;
        if (nameText      != null) nameText.text        = string.Empty;
        if (addressText   != null) addressText.text     = string.Empty;
    }


/// <summary>
    /// Address 버튼 클릭 → 주소 변경 패널으로 전환 (AddressChange 메뉴얼에서 사용)
    /// </summary>
    public void OnClickAddress()
    {
        controller?.GoToAddress();
    }

    public void OnClickID()
    {
        controller?.GoToIdTab();
    }
}
