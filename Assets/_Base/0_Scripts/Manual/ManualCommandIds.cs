public static class ManualCommandIds
{
    // ── 질문 버튼 ────────────────────────────────────────────────────────
    public const string AskSubmitId          = "ask_submit_id";
    public const string AskSubmitProxyId     = "ask_submit_proxy_id";    // 대리인 신분증 요청
    public const string AskPrintOrMobile     = "ask_print_or_mobile";
    public const string AskMobileNumber      = "ask_mobile_number";
    public const string AskCurrentAddress = "ask_current_address";

    // ── 오브젝트/행동 ────────────────────────────────────────────────────
    public const string SpawnIdCard              = "spawn_idcard";
    public const string SpawnProxyIdCard         = "spawn_proxy_idcard";  // 대리인 신분증 스폰
    public const string OpenIdCardDetail         = "open_idcard_detail";
    public const string OpenMonitor              = "open_monitor";
    public const string SearchRecordByInput      = "search_record_by_input";
    public const string SelectPrint              = "select_print";
    public const string SelectMobile             = "select_mobile";
    public const string PrintDocument            = "print_document";
    public const string SendMobile               = "send_mobile";
    public const string MobileNumberByInput      = "mobile_number_by_input";
    public const string RejectAddressMismatch    = "reject_address_mismatch";

    // ── 반납 트리거 ──────────────────────────────────────────────────────
    public const string ReturnPrintedDoc        = "return_printed_doc";
    public const string ReturnNewIdCard         = "return_new_idcard";      // 새 ID카드(주소변경본) 반납 트리거

    // ── 주소 변경 전용 ────────────────────────────────────────────────────
    public const string SubmitNewAddress        = "submit_new_address";     // AddressPanel input 확정 → UserData 수정
    public const string PrintNewIdCard          = "print_new_idcard";       // 새 ID카드 프린터 출력
    public const string SpawnNewIdCard          = "spawn_new_idcard";       // 새 ID카드 오브젝트 스폰(내부용)

    // ── 시스템 전용 (불필요 절차 집계에서 제외) ──────────────────────────
    public const string CallDisplay = "call_display";

        // ── 주민 등록 (NewID 메뉴얼 전용) ──────────────────────────────────
    // SearchNewId    삭제 — SearchRecordByInput 재사용
    // GoToIdTab      삭제 — UI가 직접 처리, 기록 불필요
    // InputNewIdInfo 삭제 — RegisterNewUser payload에 통합
    // ReturnIdCard   삭제 — requiredReturnItems(IDCard)로 이미 처리
    public const string GoToNewIdTab          = "go_to_new_id_tab";         // 내부용: 등록/수정 분기 시 context.isEditMode 세팅
    public const string RegisterNewIdPortrait = "register_new_id_portrait"; // NewID탭: 초상화 사진 버튼
    public const string RegisterNewUser       = "register_new_user";         // NewID탭: 등록 버튼 (이름|주소 payload)return_id_card";            // 신분증 반납 후 응대 종료;
}
