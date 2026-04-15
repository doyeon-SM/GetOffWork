using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주민 등록 메뉴얼.
///
/// [절차 흐름]
/// 1. 방문객 입장 → 런타임 UserRecordData 즉시 생성 (Initialize)
/// 2. AskSubmitId           — 신분증 제출 요청
/// 3. SearchRecordByInput   — ID 탭에서 방문객 ID 조회 → 미등록이면 등록/수정 버튼 활성화
/// 4. GoToNewIdTab          — (내부) 등록시 빈 폼, 수정시 prefill 폼 → context.isEditMode 세팅
/// 5. RegisterNewIdPortrait — 사진 버튼 → 방문객 portrait sprite 등록
/// 6. RegisterNewUser       — 등록 버튼 → 이름|주소 payload → DB에 런타임 레코드 저장
/// 7. 신분증 자동 반납 (requiredReturnItems: IDCard) → 응대 종료
///
/// [정상 응대 조건]
/// - 기존 신분증 반납 (필수, requiredReturnItems 처리)
/// - 주민 데이터 등록 성공 (DB RegisterRuntimeRecord true)
/// - 등록 데이터와 방문객 데이터(런타임) 이름/주소 일치
///
/// [비정상 반려 조건]
/// - 주민 데이터 등록 실패 (오타, ID 중복 포함)
/// - 등록 데이터와 방문객 데이터 불일치
///
/// [반려사항 놓침 조건]
/// - 방문객 초상화가 fakePortrait였는데 그대로 등록 (isPortraitMismatch=true)
/// - 방문객 ID가 DB에 이미 있는데 그냥 처리 (isDuplicateId=true)
/// </summary>
public class M_NewID : Manual
{
    private readonly UserRecordDatabase _userDatabase;
    private readonly PortraitListSO     _portraitList;
    private readonly AddressListSO      _addressList;
    private readonly AddressListSO      _fakeAddressList;

    public ManualDataSO manualData;

    // ── 런타임 방문객 데이터 ──────────────────────────────────────────────
    private UserRecordData _runtimeData;
    private bool           _isDuplicateId;   // 생성 시 ID가 DB와 중복
    private bool           _registeredToDb;  // RegisterRuntimeRecord 성공 여부
    private string         _enteredName;
    private string         _enteredAddress;

    // ── 생성자 ────────────────────────────────────────────────────────────

    public M_NewID(
        UserRecordDatabase database,
        PortraitListSO     portraitList,
        AddressListSO      addressList,
        AddressListSO      fakeAddressList)
    {
        _userDatabase    = database;
        _portraitList    = portraitList;
        _addressList     = addressList;
        _fakeAddressList = fakeAddressList;
    }

    protected override ManualDataSO GetManualDataSO() => manualData;
    public override string GetManualTitle() => "주민 등록";


    // ── 초기화 ────────────────────────────────────────────────────────────

public override void Initialize(ComplaintContext newContext)
    {
        base.Initialize(newContext);

        _runtimeData = RuntimeUserGenerator.Generate(
            _userDatabase,
            _portraitList,
            _addressList,
            _fakeAddressList,
            out _isDuplicateId);

        context.applicantRecordId = _runtimeData.recordId;

        // 실제 IdCard에 fake가 적용됐는지를 기준으로 mismatch 세팅
        // (단순히 fakePortrait가 존재하는지가 아니라 IdCardPortrait가 fake인지 확인)
        context.isPortraitMismatch = _runtimeData.IdCardPortrait != null
                                     && _runtimeData.IdCardPortrait != _runtimeData.portrait;
        context.isIdMismatch       = _runtimeData.IdCardId != _runtimeData.recordId;
        context.isAddressMismatch  = _runtimeData.IdCardAddress != _runtimeData.address;

        if (_isDuplicateId)
            context.wasUnregistered = false;

        context.runtimeUserData = _runtimeData;

        Debug.Log($"[M_NewID] 방문객 생성 — ID:{_runtimeData.recordId} / 이름:{_runtimeData.fullName}" +
                  $" / 중복:{_isDuplicateId} / idMismatch:{context.isIdMismatch}" +
                  $" / addrMismatch:{context.isAddressMismatch} / portraitMismatch:{context.isPortraitMismatch}");
    }

    // ── 절차 정의 ────────────────────────────────────────────────────────

protected override void BuildSteps()
    {
        if (manualData != null)
        {
            requiredSteps = manualData.ToStepEntries();
            return;
        }

        Debug.LogWarning("[M_NewID] manualData SO가 연결되지 않았습니다. 하드코딩 기본값을 사용합니다.");
        requiredSteps = new System.Collections.Generic.List<ManualStepEntry>
        {
            new ManualStepEntry(ManualCommandIds.AskSubmitId,           true, new StepPenalty(kindness: 1)),
            new ManualStepEntry(ManualCommandIds.SearchRecordByInput,   true, new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.GoToNewIdTab,          true, new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.RegisterNewIdPortrait, true, new StepPenalty(reliability: 1)),
            new ManualStepEntry(ManualCommandIds.RegisterNewUser,       true, new StepPenalty(reliability: 2)),
        };
    }

    protected override void BuildReturnItems()
    {
        // 신분증은 AskSubmitId 이후 반납 필수 항목으로 추가
        // (AskSubmitId 처리 시 AddRequiredReturnItem 호출)
    }

    // ── Execute ──────────────────────────────────────────────────────────

public override ResponseResult Execute(string commandId, string payload = null)
    {
        if (isCompleted && commandId != ManualCommandIds.OpenMonitor)
            return WrongOrder("이미 처리가 완료된 민원입니다.");

        switch (commandId)
        {
            case ManualCommandIds.AskSubmitId:           return HandleAskSubmitId();
            case ManualCommandIds.SpawnIdCard:           return HandleSpawnIdCard();
            case ManualCommandIds.OpenMonitor:           return HandleOpenMonitor();
            case ManualCommandIds.OpenIdCardDetail:      return HandleOpenIdCardDetail();
            case ManualCommandIds.SearchRecordByInput:   return HandleSearchRecordByInput(payload);
            case ManualCommandIds.SearchNewId:           return HandleSearchNewId(payload);
            case ManualCommandIds.GoToNewIdTab:          return HandleGoToNewIdTab(payload);
            case ManualCommandIds.RegisterNewIdPortrait: return HandleRegisterPortrait();
            case ManualCommandIds.RegisterNewUser:       return HandleRegisterNewUser(payload);
            default:                                     return WrongOrder("알 수 없는 명령입니다.");
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
        AddRequiredReturnItem(DeskObjectType.IDCard);
        return CorrectResponse();
    }

    private ResponseResult HandleOpenMonitor()
    {
        return CorrectResponse(shouldOpenMonitor: true);
    }

    private ResponseResult HandleOpenIdCardDetail()
    {
        if (!context.idCardSpawned)
            return WrongOrder("신분증을 아직 드리지 않았는데요.");
        context.idCardInspected = true;
        return CorrectResponse(shouldOpenIdCardDetail: true);
    }
    private ResponseResult HandleSearchRecordByInput(string inputId)
    {
        context.searchedInputId = inputId;
        context.searchedByInputId = true;
        // isAddressMismatch는 민원 생성 시(CreateRandomComplaint)에 미리 결정됨.
        // Search는 모니터 표시를 트리거하는 용도로만 사용한다.
        RecordAction(ManualCommandIds.SearchRecordByInput);
        return CorrectResponse(customerMessage: "", shouldRefreshMonitorData: true);
    }

    private ResponseResult HandleSearchNewId(string inputId)
    {
        if (!context.searchedByInputId)
            return WrongOrder("ID를 먼저 조회해 주세요");
        if (string.IsNullOrWhiteSpace(inputId))
            return WrongOrder("ID를 입력해 주세요.");

        RecordAction(ManualCommandIds.SearchNewId);
        context.enteredNewId    = inputId;
        context.newIdSearched   = true;

        // DB에 없으면 미등록
        bool found = _userDatabase != null && _userDatabase.TryGetRecord(inputId, out _);
        context.wasUnregistered = !found;

        return CorrectResponse(shouldRefreshMonitorData: true);
    }

private ResponseResult HandleGoToNewIdTab(string payload)
    {
        if (!context.newIdSearched)
            return WrongOrder("먼저 ID를 입력해 주세요.");

        RecordAction(ManualCommandIds.GoToNewIdTab);

        // payload 형식: "register" 또는 "edit|prefillName|prefillAddress"
        bool   isEditMode    = false;
        string prefillName   = string.Empty;
        string prefillAddr   = string.Empty;

        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split('|');
            isEditMode = parts[0].Trim().ToLower() == "edit";
            if (isEditMode && parts.Length >= 3)
            {
                prefillName = parts[1].Trim();
                prefillAddr = parts[2].Trim();
            }
        }

        context.isEditMode = isEditMode;

        // M_NewID가 직접 UI 전환을 트리거
        var monitorCtrl = Object.FindFirstObjectByType<UIMonitorController>();
        monitorCtrl?.GoToNewIdTab(isEditMode, prefillName, prefillAddr);

        return CorrectResponse();
    }



    private ResponseResult HandleRegisterPortrait()
    {
        if (_runtimeData == null)
            return WrongOrder("방문객 데이터가 없습니다.");

        RecordAction(ManualCommandIds.RegisterNewIdPortrait);
        context.portraitRegistered = true;

        // 방문객의 실제 portrait (신분증에 있는 사진이 아닌 DB 기준 사진)을 UI에 전달
        // UIMonitorController.NotifyPortraitRegistered가 NewIdPanel.SetPortrait를 호출
        var monitorCtrl = Object.FindFirstObjectByType<UIMonitorController>();
        monitorCtrl?.NotifyPortraitRegistered(_runtimeData.portrait);

        return CorrectResponse();
    }

    private ResponseResult HandleRegisterNewUser(string payload)
    {
        // payload 형식: "이름|주소" (UIMonitorController.OnRegisterNewUser에서 전달)
        if (!string.IsNullOrWhiteSpace(payload))
        {
            var parts = payload.Split('|');
            if (parts.Length >= 2)
            {
                _enteredName    = parts[0].Trim();
                _enteredAddress = parts[1].Trim();
                context.newNameEntered    = true;
                context.newAddressEntered = true;
            }
        }

        if (!context.portraitRegistered)
            return WrongOrder("초상화를 먼저 등록해 주세요.");
        if (string.IsNullOrWhiteSpace(_enteredName) || string.IsNullOrWhiteSpace(_enteredAddress))
            return WrongOrder("이름과 주소를 입력해 주세요.");

        // 중복 ID 반려 사항
        if (_isDuplicateId)
        {
            Debug.LogWarning("[M_NewID] ID 중복 — 정상 반려 사항.");
            // 등록 시도는 하지 않고 실패로 처리
            return WrongOrder("해당 ID는 이미 등록된 ID입니다. 반려 처리가 필요합니다.");
        }

        // 방문객 런타임 데이터에 입력값 적용
        _runtimeData.fullName = _enteredName;
        _runtimeData.address  = _enteredAddress;

        // DB에 런타임 등록
        bool success = _userDatabase?.RegisterRuntimeRecord(_runtimeData) ?? false;
        if (!success)
        {
            Debug.LogWarning("[M_NewID] DB 등록 실패.");
            return WrongOrder("등록에 실패했습니다. ID가 이미 존재합니다.");
        }

        RecordAction(ManualCommandIds.RegisterNewUser);
        _registeredToDb           = true;
        context.newUserRegistered = true;

        // 데이터 일치 여부 판별: 입력값 vs 생성된 방문객 데이터
        bool nameMismatch    = _enteredName    != _runtimeData.fullName;
        bool addressMismatch = _enteredAddress != _runtimeData.address;
        if (nameMismatch || addressMismatch)
        {
            // 오타 → 비정상 반려 대상 (플레이어가 확인 후 반려해야 함)
            Debug.LogWarning($"[M_NewID] 데이터 불일치 — name:{nameMismatch} addr:{addressMismatch}");
        }

        return CorrectResponseFromSO(ManualCommandIds.RegisterNewUser, fallback: "등록되었습니다.");
    }



    // ── 공개 헬퍼 ─────────────────────────────────────────────────────────

    /// <summary>씬의 UserImage SpriteRenderer에 방문객 portrait를 표시하기 위해 외부에서 호출.</summary>
    public Sprite GetVisitorPortrait() => _runtimeData?.portrait;

    /// <summary>신분증에 표시할 portrait: fakePortrait가 있으면 fake 반환.</summary>
    public Sprite GetIdCardPortrait()
    {
        if (_runtimeData == null) return null;
        return _runtimeData.fakePortrait != null ? _runtimeData.fakePortrait : _runtimeData.portrait;
    }

    /// <summary>런타임 데이터 참조 (외부 UI 연동용).</summary>
    public UserRecordData RuntimeData => _runtimeData;
}
