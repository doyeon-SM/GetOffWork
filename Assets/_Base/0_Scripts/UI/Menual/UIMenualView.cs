using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴얼 UI 뷰.
/// 여러 가이드 이미지 그룹을 탭 버튼으로 전환하며,
/// 각 그룹 내의 이미지를 이전/다음 버튼으로 페이지 넘김 한다.
/// </summary>
public class UIMenualView : MonoBehaviour
{
    // ── Inspector 연결 필드 ──────────────────────────────────────────────────

    [Header("패널 루트 (메뉴얼 UI 전체 패널)")]
    [SerializeField] private GameObject panelRoot;

    [Header("메뉴얼 탭 버튼 목록 (ManualImageGroup 순서와 동일)")]
    [SerializeField] private List<Button> tabButtons = new();

    [Header("각 메뉴얼의 가이드 이미지 그룹 (탭 순서와 동일)")]
    [SerializeField] private List<ManualImageGroup> manualGroups = new();

    [Header("가이드 이미지를 표시할 Image 컴포넌트")]
    [SerializeField] private Image guideImage;

    [Header("페이지 이동 버튼")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    // ── 내부 상태 ──────────────────────────────────────────────────────────

    private int _currentTabIndex  = 0;
    private int _currentPageIndex = 0;

    // ── Unity 생명주기 ──────────────────────────────────────────────────────

    private void Awake()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            int captured = i;
            if (tabButtons[i] != null)
                tabButtons[i].onClick.AddListener(() => OnTabClicked(captured));
        }

        if (prevButton  != null) prevButton.onClick.AddListener(OnPrevPage);
        if (nextButton  != null) nextButton.onClick.AddListener(OnNextPage);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ── 공개 API ────────────────────────────────────────────────────────────

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        ShowTab(0);
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

    // ── 탭 전환 ────────────────────────────────────────────────────────────

    private void OnTabClicked(int index) => ShowTab(index);

    private void ShowTab(int index)
    {
        if (manualGroups == null || manualGroups.Count == 0) return;
        index = Mathf.Clamp(index, 0, manualGroups.Count - 1);
        _currentTabIndex  = index;
        _currentPageIndex = 0;
        RefreshTabHighlight();
        RefreshPage();
    }

    private void RefreshTabHighlight()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (tabButtons[i] == null) continue;
            tabButtons[i].interactable = (i != _currentTabIndex);
        }
    }

    // ── 페이지 이동 ─────────────────────────────────────────────────────────

    private void OnPrevPage() { _currentPageIndex--; RefreshPage(); }
    private void OnNextPage() { _currentPageIndex++; RefreshPage(); }

    private void RefreshPage()
    {
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

[System.Serializable]
public class ManualImageGroup
{
    [Tooltip("탭 이름 (참조용 레이블)")]
    public string groupName;

    [Tooltip("이 탭에서 순서대로 표시할 가이드 이미지 목록")]
    public List<Sprite> guideImages = new();
}
