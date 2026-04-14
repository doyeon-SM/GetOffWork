using TMPro;
using UnityEngine;

/// <summary>
/// 주소 변경 패널.
/// UIMonitorController.GoToAddress()로 전환되어 표시된다.
///
/// [버튼 구성]
/// - 확정+출력 버튼 : OnClickSubmitAndPrint() — SubmitNewAddress + PrintNewIdCard 연속 실행
/// - 뒤로가기 버튼  : OnClickBack()          — GoToMain()
/// - 닫기 버튼      : OnClickClose()         — Close()
///
/// [절차 흐름]
/// 6. inputField에 새 주소 입력
/// 7. 확정+출력 버튼 클릭 → UIMonitorController.OnSubmitAndPrintNewIdCard(address) 호출
///    → SubmitNewAddress(주소 런타임 수정) + PrintNewIdCard(새 ID카드 프린터 출력) 순서 실행
/// </summary>
public class UIMonitorAddressPanel : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private TMP_InputField addressInputField;

    [Header("현재 주소 표시 (참고용, 선택)")]
    [SerializeField] private TMP_Text currentAddressText;

    private UIMonitorController controller;

    // ── 초기화 ────────────────────────────────────────────────────────────

    /// <summary>
    /// UIMonitorController.GoToAddress()에서 패널 생성 직후 호출된다.
    /// currentRecord의 현재 주소를 참고용으로 표시한다.
    /// </summary>
    public void Init(UIMonitorController ctrl, UserRecordData currentRecord)
    {
        controller = ctrl;

        // 현재 주소 표시 (있으면)
        if (currentAddressText != null && currentRecord != null)
            currentAddressText.text = currentRecord.address;

        if (addressInputField != null)
            addressInputField.text = string.Empty;
    }

    // ── 버튼 핸들러 (Inspector에서 연결) ─────────────────────────────────

    /// <summary>
    /// 확정+출력 버튼.
    /// 절차 6(SubmitNewAddress) → 절차 7(PrintNewIdCard)을 연속 실행한다.
    /// </summary>
    public void OnClickSubmitAndPrint()
    {
        if (controller == null || addressInputField == null) return;
        string inputAddress = addressInputField.text;
        if (string.IsNullOrWhiteSpace(inputAddress))
        {
            Debug.LogWarning("[UIMonitorAddressPanel] 주소가 비어있습니다.");
            return;
        }
        controller.OnSubmitAndPrintNewIdCard(inputAddress);
    }

    /// <summary>뒤로가기 버튼 → Main 패널으로 전환</summary>
    public void OnClickBack()
    {
        controller?.GoToMain();
    }

    /// <summary>닫기 버튼 → 모니터 닫기</summary>
    public void OnClickClose()
    {
        controller?.Close();
    }
}
