using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceDeskManager : MonoBehaviour
{
    private enum DeskState
    {
        Idle,
        ServingCustomer
    }

    [Header("플레이어")]
    [SerializeField] private PlayerBase playerBase;

    [Header("랜덤 손님 대기 시간")]
    [SerializeField] private float minCustomerDelay = 2f;
    [SerializeField] private float maxCustomerDelay = 6f;

    [Header("현재 민원")]
    [SerializeField] private ComplaintContext currentComplaint;


    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private readonly Queue<ComplaintContext> waitingQueue = new Queue<ComplaintContext>();

    private Manual currentManual;
    private bool isWorking;
    private float nextCustomerTimer;
    private DeskState deskState = DeskState.Idle;

    private int spawnedCustomerCountToday;

    public ComplaintContext CurrentComplaint => currentComplaint;
    public Manual CurrentManual => currentManual;
    public bool IsWorking => isWorking;
    public bool HasActiveCustomer => currentComplaint != null && currentManual != null;
    public int WaitingCount => waitingQueue.Count;

    // PlayerBase 실제 필드명에 맞게 수정
    public int MaxCustomerPerDay => playerBase != null ? playerBase.PlayerLevel * 3 : 0;
    public bool HasReachedDailyLimit => spawnedCustomerCountToday >= MaxCustomerPerDay;

    // UI 이벤트
    public event Action<int> OnWaitingQueueChanged;
    public event Action<ComplaintContext> OnCustomerCalled;
    public event Action OnCustomerCleared;
    public event Action<bool> OnWorkStateChanged;

    private void Awake()
    {
        ResolvePlayerBase();
    }

    private void Start()
    {
        ResolvePlayerBase();
        RaiseWaitingQueueChanged();
    }

    private void Update()
    {
        if (!isWorking)
            return;

        // 현재 민원 처리 중이어도 대기자 추가
        UpdateWaitingArrival();

        if (deskState == DeskState.ServingCustomer)
        {
            UpdateCurrentCustomerPatience();
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
        spawnedCustomerCountToday = 0;
        waitingQueue.Clear();

        ClearCurrentCustomerInternal();
        deskState = DeskState.Idle;

        ScheduleNextCustomerArrival();
        RaiseWaitingQueueChanged();
        OnWorkStateChanged?.Invoke(true);

        if (showDebugLog)
            Debug.Log($"업무 시작 / 하루 최대 민원인 수: {MaxCustomerPerDay}");
    }

    public void StopWorkPhase()
    {
        isWorking = false;
        waitingQueue.Clear();

        ClearCurrentCustomerInternal();
        deskState = DeskState.Idle;
        nextCustomerTimer = 0f;

        RaiseWaitingQueueChanged();
        OnCustomerCleared?.Invoke();
        OnWorkStateChanged?.Invoke(false);

        if (showDebugLog)
            Debug.Log("업무 중지");
    }

    private void UpdateWaitingArrival()
    {
        if (HasReachedDailyLimit)
            return;

        nextCustomerTimer -= Time.deltaTime;

        if (nextCustomerTimer <= 0f)
        {
            EnqueueNextCustomer();

            if (!HasReachedDailyLimit)
            {
                ScheduleNextCustomerArrival();
            }
            else if (showDebugLog)
            {
                Debug.Log("하루 최대 민원인 수에 도달하여 더 이상 대기자를 생성하지 않습니다.");
            }
        }
    }

    private void ScheduleNextCustomerArrival()
    {
        if (HasReachedDailyLimit)
        {
            nextCustomerTimer = 0f;
            return;
        }

        nextCustomerTimer = UnityEngine.Random.Range(minCustomerDelay, maxCustomerDelay);

        if (showDebugLog)
            Debug.Log($"다음 대기자 도착까지: {nextCustomerTimer:F1}초");
    }

    private void EnqueueNextCustomer()
    {
        if (HasReachedDailyLimit)
            return;

        ComplaintContext complaint = CreateRandomComplaint();
        complaint.ResetPatience();

        waitingQueue.Enqueue(complaint);
        spawnedCustomerCountToday++;

        RaiseWaitingQueueChanged();

        if (showDebugLog)
        {
            Debug.Log(
                $"대기열 추가 / 현재 대기자 수: {waitingQueue.Count} / " +
                $"오늘 생성 수: {spawnedCustomerCountToday}/{MaxCustomerPerDay}"
            );
        }
    }

    public void OnClickCallNextCustomer()
    {
        CallNextCustomer();
    }

    public bool CallNextCustomer()
    {
        if (!isWorking)
            return false;

        if (HasActiveCustomer)
        {
            if (showDebugLog)
                Debug.Log("이미 현재 민원 처리 중입니다.");
            return false;
        }

        if (waitingQueue.Count <= 0)
        {
            if (showDebugLog)
                Debug.Log("대기열이 비어 있습니다.");
            return false;
        }

        ComplaintContext nextComplaint = waitingQueue.Dequeue();
        RaiseWaitingQueueChanged();

        Manual manual = CreateManualByComplaint(nextComplaint);
        if (manual == null)
        {
            if (showDebugLog)
                Debug.LogWarning("민원 매뉴얼 생성 실패");
            return false;
        }

        currentComplaint = nextComplaint;
        currentManual = manual;
        currentManual.Initialize(currentComplaint);
        deskState = DeskState.ServingCustomer;

        OnCustomerCalled?.Invoke(currentComplaint);

        if (showDebugLog)
            Debug.Log($"호출 완료: {currentManual.GetManualTitle()} / {currentComplaint.applicantType}");

        return true;
    }

    private Manual CreateManualByComplaint(ComplaintContext complaint)
    {
        switch (complaint.complaintType)
        {
            case ComplaintContext.ComplaintType.FullID:
                return new M_ID();

            default:
                return null;
        }
    }

    private ComplaintContext CreateRandomComplaint()
    {
        ComplaintContext complaint = new ComplaintContext();

        complaint.complaintType = ComplaintContext.ComplaintType.FullID;
        complaint.applicantType = UnityEngine.Random.value > 0.5f
            ? ComplaintContext.ApplicantType.Self
            : ComplaintContext.ApplicantType.Proxy;

        complaint.deliveryType = UnityEngine.Random.value > 0.5f
            ? ComplaintContext.DeliveryType.Print
            : ComplaintContext.DeliveryType.Mobile;

        complaint.maxPatience = UnityEngine.Random.Range(20f, 40f);
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

        if (showDebugLog)
            Debug.Log(result.Message);

        if (result.IsCompleted)
        {
            FinishCurrentCustomer();
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

        ApplyStatDelta(Stat.Kindness, result.KindnessDelta);
        ApplyStatDelta(Stat.Stress, result.StressDelta);
        ApplyStatDelta(Stat.Reliability, result.ReliabilityDelta);

        if (result.PayDelta != 0)
            playerBase.AddPay(result.PayDelta);
    }

    private void ApplyStatDelta(Stat stat, int delta)
    {
        if (playerBase == null || delta == 0)
            return;

        playerBase.AddStat(stat, delta);
    }

    private void HandlePatienceExpired()
    {
        ResolvePlayerBase();

        if (showDebugLog)
            Debug.Log("현재 민원인 인내심 소진");

        if (playerBase != null)
        {
            playerBase.AddPerformance(-2);
            playerBase.AddStat(Stat.Stress, 2);
        }

        FinishCurrentCustomer();
    }

    private void FinishCurrentCustomer()
    {
        ClearCurrentCustomerInternal();
        deskState = DeskState.Idle;
        OnCustomerCleared?.Invoke();
    }

    private void ClearCurrentCustomerInternal()
    {
        currentComplaint = null;
        currentManual = null;
    }

    private void RaiseWaitingQueueChanged()
    {
        OnWaitingQueueChanged?.Invoke(waitingQueue.Count);
    }
}