public static class ManualCommandIds
{
    // ── 질문 버튼 ────────────────────────────────────────────────────────
    public const string AskSubmitId          = "ask_submit_id";
    public const string AskSubmitProxyId     = "ask_submit_proxy_id";    // 대리인 신분증 요청
    public const string AskPrintOrMobile     = "ask_print_or_mobile";
    public const string AskMobileNumber      = "ask_mobile_number";

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
    public const string ReturnPrintedDoc = "return_printed_doc";

    // ── 시스템 전용 (불필요 절차 집계에서 제외) ──────────────────────────
    public const string CallDisplay = "call_display";
}
