using UnityEngine;

public class ServiceDeskManager : MonoBehaviour
{
    private enum DeskState
    {
        Idle,
        WaitingForNextCustomer,
        ServingCustomer
    }

    [Header("플레이어")]
    [SerializeField] private PlayerBase playerBase;

    [Header("랜덤 손님 대기 시간")]
    [SerializeField] private float minCustomerDelay = 2f;
    [SerializeField] private float maxCustomerDelay = 6f;

    [Header("현재 민원")]
    [SerializeField] private ComplaintContext currentComplaint;

    private Manual currentManual;
    private bool isWorking;
    private float nextCustomerTimer;
    private DeskState deskState = DeskState.Idle;

    public ComplaintContext CurrentComplaint => currentComplaint;
    public Manual CurrentManual => currentManual;
    public bool IsWorking => isWorking;
    public bool HasActiveCustomer => currentComplaint != null && currentManual != null;

    private void Awake()
    {
        ResolvePlayerBase();
    }

    private void Start()
    {
        ResolvePlayerBase();
    }

    private void Update()
    {
        if (!isWorking)
            return;

        switch (deskState)
        {
            case DeskState.WaitingForNextCustomer:
                nextCustomerTimer -= Time.deltaTime;

                if (nextCustomerTimer <= 0f)
                {
                    SpawnNextComplaint();
                }
                break;

            case DeskState.ServingCustomer:
                UpdateCurrentCustomerPatience();
                break;
        }
    }

    public void SetPlayerBase(PlayerBase player)
    {
        playerBase = player;
    }

    private void ResolvePlayerBase()
    {
        if (playerBase != null)
            return;

        playerBase = PlayerBase.Instance;

        if (playerBase == null)
        {
            Debug.LogError("PlayerBase Instance가 없습니다!");
        }
    }

    public void BeginWorkPhase()
    {
        ResolvePlayerBase();

        isWorking = true;
        ClearCurrentCustomer();
        ScheduleNextCustomer();

        Debug.Log("업무 시작: 다음 민원인을 기다리는 중");
    }

    public void StopWorkPhase()
    {
        isWorking = false;
        ClearCurrentCustomer();
        deskState = DeskState.Idle;
        nextCustomerTimer = 0f;

        Debug.Log("업무 중지");
    }

    private void ScheduleNextCustomer()
    {
        currentComplaint = null;
        currentManual = null;
        deskState = DeskState.WaitingForNextCustomer;
        nextCustomerTimer = Random.Range(minCustomerDelay, maxCustomerDelay);

        Debug.Log($"다음 민원인 도착까지: {nextCustomerTimer:F1}초");
    }

    private void SpawnNextComplaint()
    {
        currentComplaint = CreateRandomComplaint();
        currentComplaint.ResetPatience();

        switch (currentComplaint.complaintType)
        {
            case ComplaintContext.ComplaintType.FullID:
                currentManual = new M_ID();
                break;
            default:
                currentManual = null;
                break;
        }

        if (currentManual != null)
        {
            currentManual.Initialize(currentComplaint);
            deskState = DeskState.ServingCustomer;

            Debug.Log($"새 민원 시작: {currentManual.GetManualTitle()} / {currentComplaint.applicantType}");
        }
        else
        {
            ScheduleNextCustomer();
        }
    }

    private ComplaintContext CreateRandomComplaint()
    {
        ComplaintContext complaint = new ComplaintContext();

        complaint.complaintType = ComplaintContext.ComplaintType.FullID;
        complaint.applicantType = Random.value > 0.5f
            ? ComplaintContext.ApplicantType.Self
            : ComplaintContext.ApplicantType.Proxy;

        complaint.deliveryType = Random.value > 0.5f
            ? ComplaintContext.DeliveryType.Print
            : ComplaintContext.DeliveryType.Mobile;

        complaint.maxPatience = Random.Range(20f, 40f);
        complaint.currentPatience = complaint.maxPatience;

        return complaint;
    }

    public void SubmitQuestion(string questionId)
    {
        if (!isWorking)
            return;

        if (deskState != DeskState.ServingCustomer)
            return;

        if (currentManual == null || currentComplaint == null)
            return;

        ResponseResult result = currentManual.AskQuestion(questionId);
        ApplyResponseResult(result);

        Debug.Log(result.Message);

        if (result.IsCompleted)
        {
            ScheduleNextCustomer();
        }
    }

    private void UpdateCurrentCustomerPatience()
    {
        if (currentComplaint == null)
            return;

        currentComplaint.currentPatience -= Time.deltaTime;

        if (currentComplaint.currentPatience <= 0f)
        {
            HandlePatienceExpired();
        }
    }

    private void ApplyResponseResult(ResponseResult result)
    {
        ResolvePlayerBase();

        if (playerBase == null)
            return;

        if (result.PerformanceDelta != 0)
            playerBase.AddPerformance(result.PerformanceDelta);

        ApplyStatDelta(PlayerBase.PlayerStat.Kindness, result.KindnessDelta);
        ApplyStatDelta(PlayerBase.PlayerStat.Stress, result.StressDelta);
        ApplyStatDelta(PlayerBase.PlayerStat.Reliability, result.ReliabilityDelta);

        if (result.PayDelta != 0)
            playerBase.AddPay(result.PayDelta);
    }

    private void ApplyStatDelta(PlayerBase.PlayerStat stat, int delta)
    {
        if (playerBase == null || delta == 0)
            return;

        if (delta > 0)
            playerBase.AddStat(stat, delta);
        else
            playerBase.SubtractStat(stat, Mathf.Abs(delta));
    }

    private void HandlePatienceExpired()
    {
        ResolvePlayerBase();

        Debug.Log("민원인 인내심 소진");

        if (playerBase != null)
        {
            playerBase.AddPerformance(-2);
            playerBase.AddStat(PlayerBase.PlayerStat.Stress, 2);
            playerBase.SubtractStat(PlayerBase.PlayerStat.Kindness, 1);
        }

        ScheduleNextCustomer();
    }

    private void ClearCurrentCustomer()
    {
        currentComplaint = null;
        currentManual = null;
    }
}