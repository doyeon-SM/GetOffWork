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

    [Header("메뉴얼 데이터 매니저")]
    [SerializeField] private ManualDataManager manualDataManager;

    [Header("진상 민원인 설정")]
    [SerializeField] private NuisanceTypeSO nuisanceTypeSettings;
    [SerializeField] private MismatchSettingSO mismatchSetting;

    [Header("주소 변경 요청 주소 리스트")]
    [SerializeField] private AddressListSO addressListSO;

    [Header("주민 등록 메뉴얼 설정")]
    [SerializeField] private PortraitListSO portraitListSO;
    [SerializeField] private AddressListSO fakeAddressListSO;

    [Header("질문 설정")]
    [SerializeField] private QuestionDataList questionDataList;

    // ── 프로퍼티 ─────────────────────────────────────────────────────
    public UserRecordDatabase        UserDatabase      => userDatabase;
    public ComplaintOpeningLineTable OpeningLineTable  => openingLineTable;
    public ComplaintClosingLineTable ClosingLineTable  => closingLineTable;
    public ManualDataManager         ManualDataManager => manualDataManager;

    /// <summary>NuisanceTypeSO. 없으면 null — 호출 측에서 null 체크 필요</summary>
    public NuisanceTypeSO    NuisanceSettings => nuisanceTypeSettings;
    /// <summary>불일치 케이스 설정. 없으면 null</summary>
    public MismatchSettingSO MismatchSetting  => mismatchSetting;

    public QuestionDataList QuestionList      => questionDataList;
    public AddressListSO    AddressListSO     => addressListSO;
    public PortraitListSO   PortraitList      => portraitListSO;
    public AddressListSO    FakeAddressListSO => fakeAddressListSO;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

