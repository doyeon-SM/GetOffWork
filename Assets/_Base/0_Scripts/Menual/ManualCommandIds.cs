public static class ManualCommandIds
{
    // ── 질문 버튼 ────────────────────────────────────────────────────────
    public const string AskSubmitId       = "ask_submit_id";
    public const string AskPrintOrMobile  = "ask_print_or_mobile";
    public const string AskMobileNumber = "ask_mobile_number";

    // ── 오브젝트/행동 ────────────────────────────────────────────────────
    public const string SpawnIdCard           = "spawn_idcard";
    public const string OpenIdCardDetail      = "open_idcard_detail";
    public const string OpenMonitor           = "open_monitor";
    public const string SearchRecordByInput   = "search_record_by_input";
    public const string SelectPrint           = "select_print";
    public const string SelectMobile          = "select_mobile";
    public const string PrintDocument         = "print_document";
    public const string SendMobile            = "send_mobile";
    public const string MobileNumberByInput = "mobile_number_by_input";
    public const string RejectAddressMismatch = "reject_address_mismatch";

    // ── 반납 트리거 ──────────────────────────────────────────────────────
    // Paper를 TakeZone에 드롭할 때 발행. 평가에서는 제외(시스템 전용).
    public const string ReturnPrintedDoc = "return_printed_doc";

    // ── 시스템 전용 (불필요 절차 집계에서 제외) ──────────────────────────
    // CallDisplay 클릭은 응대종료/반려 트리거이므로 불필요 절차로 카운트하지 않는다.
    public const string CallDisplay = "call_display";
}
