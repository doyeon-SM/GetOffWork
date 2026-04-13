using UnityEngine;

public class ServiceDataManager : MonoBehaviour
{
    public static ServiceDataManager Instance { get; private set; }

    [Header("주민 데이터")]
    [SerializeField] private UserRecordDatabase userDatabase;

    [Header("입장 대사 데이터")]
    [SerializeField] private ComplaintOpeningLineTable openingLineTable;

    [Header("퇴장 대사 데이터")]
    [SerializeField] private ComplaintClosingLineTable closingLineTable;

    [Header("FullID 메뉴얼 SO")]
    [SerializeField] private ManualDataSO fullIDSelfManualData_Print;
    [SerializeField] private ManualDataSO fullIDSelfManualData_Mobile;
    [SerializeField] private ManualDataSO fullIDProxyManualData_Print;
    [SerializeField] private ManualDataSO fullIDProxyManualData_Mobile;

    [Header("진상 민원인 설정")]
    [SerializeField] private NuisanceTypeSO nuisanceTypeSettings;
    [SerializeField] private MismatchSettingSO mismatchSetting;

    [Header("질문 설정")]
    [SerializeField] private QuestionDataList questionDataList;

    // ── 프로퍼티 ─────────────────────────────────────────────────────
    public UserRecordDatabase        UserDatabase      => userDatabase;
    public ComplaintOpeningLineTable OpeningLineTable  => openingLineTable;
    public ComplaintClosingLineTable ClosingLineTable  => closingLineTable;
    public ManualDataSO FullSelf_Print   => fullIDSelfManualData_Print;
    public ManualDataSO FullSelf_Mobile  => fullIDSelfManualData_Mobile;
    public ManualDataSO FullProxy_Print  => fullIDProxyManualData_Print;
    public ManualDataSO Fullproxy_Mobile => fullIDProxyManualData_Mobile;

    /// <summary>NuisanceTypeSO. 없으면 null — 호웉 측에서 null 체크 필요</summary>
    /// <summary>NuisanceTypeSO. 없으면 null — 호출 측에서 null 체크 필요</summary>
    public NuisanceTypeSO NuisanceSettings => nuisanceTypeSettings;

    /// <summary>주소불일치 케이스 설정. 없으면 null</summary>
    public MismatchSettingSO MismatchSetting => mismatchSetting;

    public QuestionDataList QuestionList => questionDataList;

private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
