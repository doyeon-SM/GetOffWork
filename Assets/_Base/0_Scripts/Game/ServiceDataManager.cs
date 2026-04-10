using UnityEngine;

public class ServiceDataManager : MonoBehaviour
{
    public static ServiceDataManager Instance { get; private set; }

    [Header("주민 데이터")]
    [SerializeField] private UserRecordDatabase userDatabase;

    [Header("입장 대사 데이터")]
    [SerializeField] private ComplaintOpeningLineTable openingLineTable;

    [Header("FullID 메뉴얼 SO")]
    [SerializeField] private ManualDataSO fullIDSelfManualData_Print;
    [SerializeField] private ManualDataSO fullIDSelfManualData_Mobile;
    [SerializeField] private ManualDataSO fullIDProxyManualData_Print;
    [SerializeField] private ManualDataSO fullIDProxyManualData_Mobile;

    public UserRecordDatabase UserDatabase => userDatabase;

    public ComplaintOpeningLineTable OpeningLineTable => openingLineTable;

    public ManualDataSO FullSelf_Print => fullIDSelfManualData_Print;
    public ManualDataSO FullSelf_Mobile => fullIDSelfManualData_Mobile;
    public ManualDataSO FullProxy_Print => fullIDProxyManualData_Print;
    public ManualDataSO Fullproxy_Mobile => fullIDProxyManualData_Mobile;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
