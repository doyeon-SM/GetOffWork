using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모니터 ID 탭 패널.
/// 방문객의 ID를 입력해 DB 조회 후 미등록이면 등록 버튼, 오타 재등록이면 수정 버튼을 활성화한다.
///
/// [버튼 구성]
/// - 조회 버튼     : OnClickSearch()   — ID 입력 후 DB 조회
/// - 등록 버튼     : OnClickRegister() — 미등록 ID → NewID 탭으로 이동 (빈 폼)
/// - 수정 버튼     : OnClickEdit()     — 기존 등록 ID → NewID 탭으로 이동 (기존 정보 prefill)
/// - 뒤로가기 버튼  : OnClickBack()    — Main 패널로 이동
/// - 닫기 버튼     : OnClickClose()   — 모니터 닫기
/// </summary>
public class UIMonitorIdPanel : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private TMP_InputField idInputField;

    [Header("결과 표시")]
    [SerializeField] private TMP_Text resultText;

    [Header("버튼")]
    [SerializeField] private Button registerButton;
    [SerializeField] private Button editButton;

    private UIMonitorController controller;
    private bool   _isUnregistered;
    private string _searchedId;

    // ── 초기화 ────────────────────────────────────────────────────────────

    public void Init(UIMonitorController ctrl)
    {
        controller = ctrl;
        registerButton?.gameObject.SetActive(false);
        editButton?.gameObject.SetActive(false);
        if (resultText != null) resultText.text = string.Empty;
    }

    // ── 조회 결과 갱신 (UIMonitorController.OnSearchNewId 이후 호출) ───────

    /// <summary>
    /// 조회 결과에 따라 UI를 갱신한다.
    /// ServiceDeskManager.TryGetResidentRecord 결과를 UIMonitorController가 전달한다.
    /// </summary>
    public void RefreshSearchResult(string inputId)
    {
        _searchedId = inputId;

        // ID 형식 검증: 8자리 숫자
        if (string.IsNullOrWhiteSpace(inputId) || inputId.Length != 8 || !System.Text.RegularExpressions.Regex.IsMatch(inputId, @"^\d{8}$"))
        {
            SetResult("올바른 ID 형식이 아닙니다. (8자리 숫자)", false);
            return;
        }

        var deskMgr = FindFirstObjectByType<ServiceDeskManager>();
        if (deskMgr == null) return;

        bool found = deskMgr.TryGetResidentRecord(inputId, out _);
        _isUnregistered = !found;

        if (_isUnregistered)
        {
            SetResult("미등록 ID입니다.", isUnregistered: true);
        }
        else
        {
            SetResult("이미 등록된 ID입니다.", isUnregistered: false);
        }
    }

    private void SetResult(string message, bool isUnregistered)
    {
        if (resultText != null) resultText.text = message;
        registerButton?.gameObject.SetActive(isUnregistered);
        editButton?.gameObject.SetActive(!isUnregistered);
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────────────────

    public void OnClickSearch()
    {
        if (controller == null || idInputField == null) return;
        controller.OnSearchNewId(idInputField.text);
    }

    /// <summary>등록 버튼 — 미등록 ID이므로 NewID 탭을 빈 폼으로 열기</summary>
public void OnClickRegister()
    {
        if (controller == null) return;
        controller.ExecuteGoToNewIdTab("register");
    }

    /// <summary>수정 버튼 — 기존 등록 ID이므로 NewID 탭에 기존 정보 prefill</summary>
public void OnClickEdit()
    {
        if (controller == null) return;
        // 기존 등록 ID의 이름/주소를 payload로 전달
        var deskMgr = FindFirstObjectByType<ServiceDeskManager>();
        string prefillName    = string.Empty;
        string prefillAddress = string.Empty;
        if (deskMgr != null && deskMgr.TryGetResidentRecord(_searchedId, out var rec))
        {
            prefillName    = rec.fullName;
            prefillAddress = rec.address;
        }
        // payload 형식: "edit|prefillName|prefillAddress"
        string payload = $"edit|{prefillName}|{prefillAddress}";
        controller.ExecuteGoToNewIdTab(payload);
    }

    public void OnClickBack()  => controller?.GoToMain();
    public void OnClickClose() => controller?.Close();
}
