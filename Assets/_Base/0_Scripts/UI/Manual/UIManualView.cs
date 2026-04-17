using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴얼 UI 뷰.
/// 일반 탭: 여러 가이드 이미지 그룹을 탭 버튼으로 전환하고 페이지 이동.
/// SOS 탭 : 별도 버튼으로 진입. 페이지별 guideImage + AntiNuisanceManual 1:1 대응.
///          공유 발동 버튼 1개로 현재 페이지의 메뉴얼을 실행한다.
/// </summary>
public class UIManualView : MonoBehaviour
{
    // ── 일반 메뉴얼 Inspector 필드 ──────────────────────────────────────────

    [Header("패널 루트 (메뉴얼 UI 전체 패널)")]
    [SerializeField] private GameObject panelRoot;

    [Header("메뉴얼 탭 버튼 목록 (ManualImageGroup 순서와 동일)")]
    [SerializeField] private List<Button> tabButtons = new();

    [Header("각 메뉴얼의 가이드 이미지 그룹 (탭 순서와 동일)")]
    [SerializeField] private List<ManualImageGroup> manualGroups = new();

    [Header("가이드 이미지를 표시할 Image 컴포넌트")]
    [SerializeField] private Image guideImage;

    [Header("페이지 이동 버튼 (일반/SOS 탭 공유)")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    // ── SOS 탭 Inspector 필드 ───────────────────────────────────────────────

    [Header("── SOS 탭 ──")]
    [SerializeField] private Button             sosTabButton;
    [SerializeField] private SOSManualGroup     sosManualGroup;

    [Tooltip("SOS 탭에서 현재 페이지의 진상퇴치 메뉴얼을 발동하는 공유 버튼")]
    [SerializeField] private Button             sosActionButton;

    [Tooltip("진상퇴치 메뉴얼 발동에 사용할 ServiceDeskManager")]
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    // ── 내부 상태 ────────────────────────────────────────────────────────────

    private int  _currentTabIndex  = 0;
    private int  _currentPageIndex = 0;
    private bool _isSOSTabActive   = false;

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        // 일반 탭 버튼
        for (int i = 0; i < tabButtons.Count; i++)
        {
            int captured = i;
            if (tabButtons[i] != null)
                tabButtons[i].onClick.AddListener(() => OnNormalTabClicked(captured));
        }

        // 페이지 이동 (일반/SOS 공유)
        if (prevButton  != null) prevButton.onClick.AddListener(OnPrevPage);
        if (nextButton  != null) nextButton.onClick.AddListener(OnNextPage);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // SOS 탭 버튼
        if (sosTabButton    != null) sosTabButton.onClick.AddListener(OnSOSTabClicked);

        // SOS 발동 버튼
        if (sosActionButton != null) sosActionButton.onClick.AddListener(OnSOSActionClicked);

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ── 공개 API ────────────────────────────────────────────────────────────

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        ShowNormalTab(0);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) Close();
        else Open();
    }

    // ── 일반 탭 전환 ────────────────────────────────────────────────────────

    private void OnNormalTabClicked(int index) => ShowNormalTab(index);

    private void ShowNormalTab(int index)
    {
        if (manualGroups == null || manualGroups.Count == 0) return;
        _isSOSTabActive   = false;
        index             = Mathf.Clamp(index, 0, manualGroups.Count - 1);
        _currentTabIndex  = index;
        _currentPageIndex = 0;
        RefreshTabHighlight();
        RefreshPage();
    }

    private void RefreshTabHighlight()
    {
        // 일반 탭: 현재 선택된 탭은 비활성화 (SOS 탭이 활성화면 모두 활성화)
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (tabButtons[i] == null) continue;
            tabButtons[i].interactable = (i != _currentTabIndex) || _isSOSTabActive;
        }

        // SOS 탭 버튼: SOS 탭이 열려 있으면 비활성화
        if (sosTabButton != null)
            sosTabButton.interactable = !_isSOSTabActive;
    }

    // ── SOS 탭 전환 ─────────────────────────────────────────────────────────

    private void OnSOSTabClicked()
    {
        _isSOSTabActive   = true;
        _currentPageIndex = 0;
        RefreshTabHighlight();
        RefreshPage();
    }

    // ── SOS 발동 버튼 ────────────────────────────────────────────────────────

    private void OnSOSActionClicked()
    {
        if (!_isSOSTabActive) return;
        if (sosManualGroup == null || sosManualGroup.manualTypes == null) return;
        if (_currentPageIndex >= sosManualGroup.manualTypes.Count) return;
        if (serviceDeskManager == null)
        {
            Debug.LogWarning("[UIManualView] serviceDeskManager가 연결되지 않았습니다.");
            return;
        }

        var type   = sosManualGroup.manualTypes[_currentPageIndex];
        var manual = CreateAntiNuisanceManual(type);
        if (manual == null)
        {
            Debug.LogWarning($"[UIManualView] {type} 에 해당하는 AntiNuisanceManual이 없습니다.");
            return;
        }

        serviceDeskManager.ExecuteAntiNuisanceManual(manual);
    }

    /// <summary>
    /// AntiNuisanceManualType → AntiNuisanceManual 인스턴스 변환.
    /// 새 진상퇴치 메뉴얼 추가 시 여기에 case를 추가한다.
    /// </summary>
    private AntiNuisanceManual CreateAntiNuisanceManual(AntiNuisanceManualType type)
    {
        switch (type)
        {
            case AntiNuisanceManualType.SOS: return new M_SOS();
            default: return null;
        }
    }

    // ── 페이지 갱신 (일반/SOS 분기) ─────────────────────────────────────────

    private void OnPrevPage() { _currentPageIndex--; RefreshPage(); }
    private void OnNextPage() { _currentPageIndex++; RefreshPage(); }

    private void RefreshPage()
    {
        if (_isSOSTabActive) RefreshSOSPage();
        else                 RefreshNormalPage();
    }

    // 일반 탭 페이지 갱신
    private void RefreshNormalPage()
    {
        if (sosActionButton != null) sosActionButton.gameObject.SetActive(false);

        if (manualGroups == null || _currentTabIndex >= manualGroups.Count)
        {
            SetGuideImage(null);
            SetNavButtons(false, false);
            return;
        }

        var group  = manualGroups[_currentTabIndex];
        var images = group?.guideImages;

        if (images == null || images.Count == 0)
        {
            SetGuideImage(null);
            SetNavButtons(false, false);
            return;
        }

        _currentPageIndex = Mathf.Clamp(_currentPageIndex, 0, images.Count - 1);
        SetGuideImage(images[_currentPageIndex]);
        SetNavButtons(
            hasPrev: _currentPageIndex > 0,
            hasNext: _currentPageIndex < images.Count - 1);
    }

    // SOS 탭 페이지 갱신
    private void RefreshSOSPage()
    {
        if (sosManualGroup == null || sosManualGroup.guideImages == null ||
            sosManualGroup.guideImages.Count == 0)
        {
            SetGuideImage(null);
            SetNavButtons(false, false);
            if (sosActionButton != null) sosActionButton.gameObject.SetActive(false);
            return;
        }

        int maxPage = sosManualGroup.guideImages.Count - 1;
        _currentPageIndex = Mathf.Clamp(_currentPageIndex, 0, maxPage);

        SetGuideImage(sosManualGroup.guideImages[_currentPageIndex]);
        SetNavButtons(
            hasPrev: _currentPageIndex > 0,
            hasNext: _currentPageIndex < maxPage);

        // 해당 페이지에 메뉴얼 타입이 등록된 경우에만 발동 버튼 표시
        bool hasManual = sosManualGroup.manualTypes != null &&
                         _currentPageIndex < sosManualGroup.manualTypes.Count;
        if (sosActionButton != null)
            sosActionButton.gameObject.SetActive(hasManual);
    }

    // ── 공통 UI 헬퍼 ────────────────────────────────────────────────────────

    private void SetGuideImage(Sprite sprite)
    {
        if (guideImage == null) return;
        guideImage.sprite  = sprite;
        guideImage.enabled = sprite != null;
    }

    private void SetNavButtons(bool hasPrev, bool hasNext)
    {
        if (prevButton != null) prevButton.interactable = hasPrev;
        if (nextButton != null) nextButton.interactable = hasNext;
    }
}

// ── 데이터 클래스 ────────────────────────────────────────────────────────────

[System.Serializable]
public class ManualImageGroup
{
    [Tooltip("탭 이름 (참조용 레이블)")]
    public string groupName;

    [Tooltip("이 탭에서 순서대로 표시할 가이드 이미지 목록")]
    public List<Sprite> guideImages = new();
}

/// <summary>
/// 진상퇴치 메뉴얼 타입 열거형.
/// Inspector 드롭다운으로 페이지별 메뉴얼을 지정한다.
/// </summary>
public enum AntiNuisanceManualType
{
    SOS,
    // TalkBack,  // 추후 막말하기 추가 시 주석 해제
}

/// <summary>
/// SOS 탭 메뉴얼 그룹.
/// guideImages[i] 와 manualTypes[i] 가 1:1 대응.
/// 페이지 수는 guideImages.Count 기준.
/// </summary>
[System.Serializable]
public class SOSManualGroup
{
    [Tooltip("페이지별 안내 이미지")]
    public List<Sprite> guideImages = new();

    [Tooltip("페이지별 진상퇴치 메뉴얼 타입 (guideImages와 동일 인덱스)")]
    public List<AntiNuisanceManualType> manualTypes = new();
}
