using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주소 변경 메뉴얼 — 본인 신청 전용.
///
/// [절차 흐름]
/// 1. 민원인 입장 대사: "주소이전하러 왔습니다"  (OpeningLineTable에서 처리)
/// 2. AskSubmitId        — 신분증 제출 요청 (질문 버튼)
/// 3. SearchRecordByInput — ID 조회 및 mismatch 확인 (모니터)
/// 4. AskCurrentAddress  — 현재 주소 요청 (질문 버튼) → requestedNewAddress로 답변
/// 5. OpenAddressPanel   — 모니터 Address 버튼 → AddressPanel 열기
/// 6. SubmitNewAddress   — AddressPanel input 확정 → 런타임 UserData 수정
/// 7. PrintNewIdCard     — 새 ID카드 프린터 출력
/// 8. ReturnNewIdCard    — 새 ID카드 TakeZone 반납 → 응대 종료
///
/// [비정상 반려]
/// - 요청한 주소와 다른 내용의 ID카드 반납
/// - 기존 ID카드(원본) 반납 (단독 또는 새 카드와 함께)
///
/// [반려사항 놓침]
/// - FullID와 동일: 주소/ID/사진 불일치 상태에서 정상 처리 완료
/// </summary>
public class M_AddressChange : Manual
{
    private readonly UserRecordDatabase userDatabase;

    public ManualDataSO manualData;

    // ── 런타임 주소 변경 상태 ───────────────────────────────────────────
    /// <summary>
    /// 런타임에만 적용되는 변경된 주소.
    /// SubmitNewAddress 처리 시 설정되며, UserRecordData 원본은 수정하지 않는다.
    /// </summary>
    private string _runtimeNewAddress;

    public M_AddressChange(UserRecordDatabase database)
    {
        userDatabase = database;
    }

    protected override ManualDataSO GetManualDataSO() => manualData;

    public override string GetManualTitle() => "주소 변경 (본인)";

    // ── 절차 정의 ────────────────────────────────────────────────────────
    protected override void BuildSteps()
    {
        if (manualData != null)
        {
            requiredSteps = manualData.ToStepEntries();
            return;
        }

        Debug.LogWarning("[M_AddressChange] manualData SO가 연결되지 않았습니다. 하드코딩 기본값을 사용합니다.");
        requiredSteps = new List<ManualStepEntry>
        {
            new ManualStepEntry(ManualCommandIds.AskSubmitId,        true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.SearchRecordByInput, true,  new StepPenalty(reliability: 1), new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.AskCurrentAddress,   true,  new StepPenalty(kindness: 1),    new StepPenalty(kindness: 1)),
            new ManualStepEntry(ManualCommandIds.SubmitNewAddress,    true,  new StepPenalty(reliability: 1), new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.PrintNewIdCard,      true,  new StepPenalty(kindness: 1),    new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.ReturnNewIdCard,     true,  new StepPenalty(reliability: 1), default),
        };
    }

    /// <summary>
    /// 반납 필수 항목:
    /// - NewIDCard (새 주소가 인쇄된 카드) — TakeZone에 반납해야 함
    /// - IDCard (기존 원본) — 반납하면 안 됨 (응대 종료 시 자동 삭제)
    /// ObjectManagerBox의 TryFinishAndReturn이 NewIDCard 반납 여부를 검사한다.
    /// </summary>
    protected override void BuildReturnItems()
    {
        // 새 ID카드를 필수 반납 항목으로 등록 (PrintNewIdCard 이후 추가됨)
        // 초기에는 비워두고, PrintNewIdCard 처리 시 AddRequiredReturnItem 호출
    }

    // ── Execute ──────────────────────────────────────────────────────────
    public override ResponseResult Execute(string commandId, string payload = null)
    {
        if (isCompleted && commandId != ManualCommandIds.OpenMonitor)
            return WrongOrderFromSO(commandId, "이미 처리가 완료된 민원입니다.");

        switch (commandId)
        {
            case ManualCommandIds.AskSubmitId:         return HandleAskSubmitId();
            case ManualCommandIds.SpawnIdCard:         return HandleSpawnIdCard();
            case ManualCommandIds.OpenIdCardDetail:    return HandleOpenIdCardDetail();
            case ManualCommandIds.OpenMonitor:         return HandleOpenMonitor();
            case ManualCommandIds.SearchRecordByInput: return HandleSearchRecordByInput(payload);
            case ManualCommandIds.AskCurrentAddress:   return HandleAskCurrentAddress();
            
            case ManualCommandIds.SubmitNewAddress:    return HandleSubmitNewAddress(payload);
            case ManualCommandIds.PrintNewIdCard:      return HandlePrintNewIdCard();
            case ManualCommandIds.SpawnNewIdCard:      return HandleSpawnNewIdCard();
            case ManualCommandIds.ReturnNewIdCard:     return HandleReturnNewIdCard();
            default:                                   return WrongOrder("알 수 없는 명령입니다.");
        }
    }

    // ── 핸들러 ───────────────────────────────────────────────────────────

    private ResponseResult HandleAskSubmitId()
    {
        if (context.idCardSpawned)
            return WrongOrderFromSO(ManualCommandIds.AskSubmitId, "이미 제출했습니다.");
        RecordAction(ManualCommandIds.AskSubmitId);
        return CorrectResponseFromSO(ManualCommandIds.AskSubmitId,
            fallback: "네, 여기 있습니다.",
            shouldSpawnIdCard: true);
    }

    private ResponseResult HandleSpawnIdCard()
    {
        context.idCardSpawned = true;
        // 기존 ID카드는 응대 종료 시 자동 삭제 — RequiredReturnItems에 등록하지 않음
        return CorrectResponse();
    }

    private ResponseResult HandleOpenIdCardDetail()
    {
        if (!context.idCardSpawned)
            return WrongOrderFromSO(ManualCommandIds.OpenIdCardDetail, "신분증을 아직 드리지 않았는데요.");
        context.idCardInspected = true;
        return CorrectResponse(shouldOpenIdCardDetail: true);
    }

    private ResponseResult HandleOpenMonitor()
    {
        return CorrectResponse(shouldOpenMonitor: true);
    }

    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        context.searchedInputId   = inputId;
        context.searchedByInputId = true;
        RecordAction(ManualCommandIds.SearchRecordByInput);
        return CorrectResponse(shouldRefreshMonitorData: true);
    }

    /// <summary>
    /// 절차 4: 현재 주소 요청.
    /// requestedNewAddress(ComplaintFactory가 큐에서 꺼낸 값)를 민원인 답변으로 사용한다.
    /// </summary>
    private ResponseResult HandleAskCurrentAddress()
    {
        if (!context.searchedByInputId)
            return WrongOrder("먼저 ID를 확인해 주세요.");

        RecordAction(ManualCommandIds.AskCurrentAddress);
        context.newAddressAsked = true;

        string newAddr  = context.requestedNewAddress ?? "(주소 없음)";
        string raw      = GetCorrectLine(ManualCommandIds.AskCurrentAddress,
                              $"{newAddr} 으로 변경해 주세요.");
        string line     = ResolvePlaceholders(raw,
                              new Dictionary<string, string> { { "address", newAddr } });
        return CorrectResponse(customerMessage: line);
    }

    /// <summary>
    /// 절차 5: 모니터의 Address 버튼 → AddressPanel 열기.
    /// UI 측에서 OpenAddressPanel 이벤트를 수신해 패널을 표시한다.
    /// </summary>


    /// <summary>
    /// 절차 6: AddressPanel의 input 확정.
    /// payload = 입력된 주소 문자열.
    /// UserRecordData 원본은 건드리지 않고 런타임 필드만 수정한다.
    /// </summary>
    private ResponseResult HandleSubmitNewAddress(string inputAddress)
    {
        if (!context.newAddressAsked)
            return WrongOrder("먼저 주소를 확인해 주세요.");
        if (string.IsNullOrWhiteSpace(inputAddress))
            return WrongOrder("주소를 입력해 주세요.");

        RecordAction(ManualCommandIds.SubmitNewAddress);
        context.enteredAddress          = inputAddress;
        context.isAddressChangeCommitted = true;
        _runtimeNewAddress              = inputAddress;

        // 런타임 UserData 수정 — 원본 ScriptableObject는 건드리지 않음
        ApplyRuntimeAddressChange(inputAddress);

        return CorrectResponse();
    }

    /// <summary>
    /// 절차 7: 새 ID카드 프린터 출력.
    /// ObjectManagerBox가 SpawnNewIdCard 이벤트를 수신해 새 카드를 스폰한다.
    /// </summary>
    private ResponseResult HandlePrintNewIdCard()
    {
        if (string.IsNullOrEmpty(context.enteredAddress))
            return WrongOrder("먼저 주소를 입력해 주세요.");

        RecordAction(ManualCommandIds.PrintNewIdCard);
        context.newIdCardPrinted = true;

        // 새 ID카드를 필수 반납 항목으로 등록
        AddRequiredReturnItem(DeskObjectType.NewIDCard);

        return CorrectResponseFromSO(ManualCommandIds.PrintNewIdCard,
            fallback: "새 신분증이 출력되었습니다.");
    }

    /// <summary>SpawnNewIdCard — ObjectManagerBox가 새 카드 오브젝트를 스폰할 때 호출.</summary>
    private ResponseResult HandleSpawnNewIdCard()
    {
        context.newIdCardPrinted = true;
        return CorrectResponse();
    }

    /// <summary>
    /// 절차 8: 새 ID카드 TakeZone 반납.
    /// ObjectManagerBox의 TryFinishAndReturn이 NewIDCard 반납 여부를 검사한다.
    /// 비정상 판정은 ObjectManagerBox 또는 ServiceEvaluator에서 처리.
    /// </summary>
    private ResponseResult HandleReturnNewIdCard()
    {
        if (!context.newIdCardPrinted)
            return WrongOrder("새 신분증이 아직 출력되지 않았습니다.");

        RecordAction(ManualCommandIds.ReturnNewIdCard);
        context.newIdCardReturned = true;
        isCompleted               = true;
        context.completed         = true;

        return CorrectResponseFromSO(ManualCommandIds.ReturnNewIdCard,
            fallback: "감사합니다. 주소 변경이 완료되었습니다.",
            completeNow: true);
    }

    // ── 런타임 주소 적용 ─────────────────────────────────────────────────

    /// <summary>
    /// UserRecordData의 address를 런타임에만 변경한다.
    /// ScriptableObject 원본은 수정하지 않으므로 게임 재시작 시 원상복구된다.
    /// </summary>
    private void ApplyRuntimeAddressChange(string newAddress)
    {
        if (userDatabase == null) return;
        string recordId = context.applicantRecordId;
        if (string.IsNullOrEmpty(recordId)) return;

        if (userDatabase.TryGetRecord(recordId, out UserRecordData record))
        {
            record.address = newAddress;
            // ID카드 표시용 주소도 갱신 (불일치 없음 상태로 설정)
            record.IdCardAddress = newAddress;
            Debug.Log($"[M_AddressChange] 런타임 주소 변경 완료: {recordId} → {newAddress}");
        }
        else
        {
            Debug.LogWarning($"[M_AddressChange] 레코드를 찾을 수 없습니다: {recordId}");
        }
    }

    // ── 유틸리티 ─────────────────────────────────────────────────────────

    /// <summary>런타임에 설정된 새 주소를 반환한다.</summary>
    public string GetRuntimeNewAddress() => _runtimeNewAddress;
}
