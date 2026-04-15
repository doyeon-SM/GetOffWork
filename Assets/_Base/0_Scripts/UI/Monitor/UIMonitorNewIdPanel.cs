using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모니터 NewID 탭 패널.
/// 신규 주민 등록 정보(이름, 주소, 초상화)를 입력하고 등록 버튼으로 DB에 등록한다.
///
/// [isEditMode = false] 신규 등록: 모든 input 비워둠
/// [isEditMode = true]  오타 수정: 기존 이름/주소 prefill (ID 오타의 경우 새 레코드 생성)
///
/// [버튼 구성]
/// - 사진 버튼 : OnClickPortrait() — 방문객 런타임 sprite 등록
/// - 등록 버튼 : OnClickRegister() — DB에 주민 등록
/// - 뒤로가기 : OnClickBack()     — ID 탭으로 이동
/// - 닫기     : OnClickClose()    — 모니터 닫기
/// </summary>
public class UIMonitorNewIdPanel : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField addressInputField;

    [Header("초상화 표시")]
    [SerializeField] private Image portraitImage;

    [Header("버튼")]
    [SerializeField] private Button registerButton;

    private UIMonitorController controller;
    private bool _isEditMode;

    // ── 초기화 ────────────────────────────────────────────────────────────

    /// <summary>
    /// GoToNewIdTab에서 호출된다.
    /// isEditMode=true이면 prefillName/Address로 input을 채운다.
    /// </summary>
public void Init(UIMonitorController ctrl, bool isEditMode,
                     string prefillName = "", string prefillAddress = "", Sprite prefillPortrait = null)
    {
        controller  = ctrl;
        _isEditMode = isEditMode;

        if (nameInputField    != null) nameInputField.text    = prefillName;
        if (addressInputField != null) addressInputField.text = prefillAddress;

        if (isEditMode && prefillPortrait != null)
        {
            // edit 모드: 기존 portrait를 자동 세팅 → 사진 버튼 불필요
            if (portraitImage != null) portraitImage.sprite = prefillPortrait;
            if (registerButton != null) registerButton.interactable = true;
        }
        else
        {
            // 신규 등록: 초상화 비우고 사진 버튼으로만 등록
            if (portraitImage != null) portraitImage.sprite = null;
            if (registerButton != null) registerButton.interactable = false;
        }
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────────────────

    /// <summary>
    /// 사진 버튼 — ManualCommandIds.RegisterNewIdPortrait 커맨드 실행.
    /// M_NewID가 방문객의 portrait sprite를 받아 portraitImage에 세팅한다.
    /// </summary>
    public void OnClickPortrait()
    {
        controller?.OnRegisterPortrait();
    }

    /// <summary>
    /// 등록 버튼 — 이름|주소 payload로 RegisterNewUser 커맨드 실행.
    /// </summary>
    public void OnClickRegister()
    {
        if (controller == null) return;
        string name    = nameInputField?.text    ?? string.Empty;
        string address = addressInputField?.text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        {
            Debug.LogWarning("[UIMonitorNewIdPanel] 이름 또는 주소가 비어있습니다.");
            return;
        }
        controller.OnRegisterNewUser(name, address);
    }

    public void OnClickBack()  => controller?.GoToIdTab();
    public void OnClickClose() => controller?.Close();

    // ── M_NewID가 사진 등록 후 호출하는 메서드 ────────────────────────────

    /// <summary>M_NewID.HandleRegisterPortrait 성공 후 UIMonitorController를 통해 호출됨.</summary>
    public void SetPortrait(Sprite portrait)
    {
        if (portraitImage != null) portraitImage.sprite = portrait;
        if (registerButton != null) registerButton.interactable = portrait != null;
    }
}
