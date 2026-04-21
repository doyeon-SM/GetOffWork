using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIServiceDesk : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("대기자 UI")]
    [SerializeField] private TMP_Text waitingCountText;

    [Header("방문객 UI")]
    [SerializeField] private GameObject customerImageRoot;

    [Header("등장/퇴장 애니메이션")]
    [SerializeField] private float enterExitDuration = 1f;
    [SerializeField] private float enterStartY = -1f;
    [SerializeField] private float enterEndY   =  0f;

    private SpriteRenderer _customerSR;
    private Vector3        _baseLocalPos;
    private Coroutine      _animCoroutine;

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        if (customerImageRoot != null)
        {
            _customerSR   = customerImageRoot.GetComponent<SpriteRenderer>();
            _baseLocalPos = customerImageRoot.transform.localPosition;
        }

        HideCustomerImageImmediate();
        SetWaitingCount(0);
    }

    private void OnEnable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnWaitingQueueChanged += HandleWaitingQueueChanged;
        serviceDeskManager.OnCustomerCalled      += HandleCustomerCalled;
        serviceDeskManager.OnCustomerCleared     += HandleCustomerCleared;
        serviceDeskManager.OnWorkStateChanged    += HandleWorkStateChanged;
    }

    private void OnDisable()
    {
        if (serviceDeskManager == null) return;
        serviceDeskManager.OnWaitingQueueChanged -= HandleWaitingQueueChanged;
        serviceDeskManager.OnCustomerCalled      -= HandleCustomerCalled;
        serviceDeskManager.OnCustomerCleared     -= HandleCustomerCleared;
        serviceDeskManager.OnWorkStateChanged    -= HandleWorkStateChanged;
    }

    private void HandleWaitingQueueChanged(int waitingCount) => SetWaitingCount(waitingCount);

    private void HandleCustomerCalled(ComplaintContext complaint)
    {
        ShowCustomerImage(GetCustomerPortrait(complaint));
    }

    private void HandleCustomerCleared()
    {
        HideCustomerImage();
    }

    private void HandleWorkStateChanged(bool isWorking)
    {
        if (!isWorking)
        {
            SetWaitingCount(0);
            HideCustomerImageImmediate();
        }
    }

    private void SetWaitingCount(int count)
    {
        if (waitingCountText != null)
            waitingCountText.text = "대기 : " + count.ToString() + "명";
    }

    // ── 등장 (y: enterStartY→enterEndY, alpha: 0→1) ──────────────────────
    private void ShowCustomerImage(Sprite sprite)
    {
        if (customerImageRoot == null || _customerSR == null) return;

        _customerSR.sprite  = sprite;
        _customerSR.enabled = sprite != null;

        if (sprite == null)
        {
            customerImageRoot.SetActive(false);
            return;
        }

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        customerImageRoot.SetActive(true);
        _animCoroutine = StartCoroutine(AnimateEnter());
    }

    // ── 퇴장 (y: enterEndY→enterStartY, alpha: 1→0) ───────────────────────
    private void HideCustomerImage()
    {
        if (customerImageRoot == null || !customerImageRoot.activeSelf) return;
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateExit());
    }

    // ── 즉시 숨김 (씬 초기화·근무 종료 등) ───────────────────────────────
    private void HideCustomerImageImmediate()
    {
        if (customerImageRoot == null) return;
        if (_animCoroutine != null) { StopCoroutine(_animCoroutine); _animCoroutine = null; }
        if (_customerSR != null)
        {
            SetAlpha(_customerSR, 0f);
            _customerSR.sprite  = null;
            _customerSR.enabled = false;
        }
        SetLocalY(enterStartY);
        customerImageRoot.SetActive(false);
    }

    // ── 코루틴 ────────────────────────────────────────────────────────────
    private IEnumerator AnimateEnter()
    {
        float elapsed = 0f;
        SetLocalY(enterStartY);
        SetAlpha(_customerSR, 0f);

        while (elapsed < enterExitDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / enterExitDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            SetLocalY(Mathf.Lerp(enterStartY, enterEndY, smoothT));
            SetAlpha(_customerSR, smoothT);
            yield return null;
        }

        SetLocalY(enterEndY);
        SetAlpha(_customerSR, 1f);
        _animCoroutine = null;
    }

    private IEnumerator AnimateExit()
    {
        float elapsed = 0f;
        SetLocalY(enterEndY);
        SetAlpha(_customerSR, 1f);

        while (elapsed < enterExitDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / enterExitDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            SetLocalY(Mathf.Lerp(enterEndY, enterStartY, smoothT));
            SetAlpha(_customerSR, 1f - smoothT);
            yield return null;
        }

        SetAlpha(_customerSR, 0f);
        _customerSR.sprite  = null;
        _customerSR.enabled = false;
        customerImageRoot.SetActive(false);
        _animCoroutine = null;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────
    private void SetLocalY(float y)
    {
        if (customerImageRoot == null) return;
        var pos = _baseLocalPos;
        pos.y = _baseLocalPos.y + y;
        customerImageRoot.transform.localPosition = pos;
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }

    private Sprite GetCustomerPortrait(ComplaintContext complaint)
    {
        if (serviceDeskManager == null || complaint == null) return null;

        if (serviceDeskManager.TryGetResidentRecord(complaint.applicantRecordId, out UserRecordData record))
            return record != null ? record.portrait : null;

        if (serviceDeskManager.CurrentManual is M_NewID newIdManual)
            return newIdManual.GetVisitorPortrait();

        return null;
    }
}