using UnityEngine;

public abstract class ClickableWorldObject : MonoBehaviour, IClickableObject
{
    [Header("기본 설정")]
    [SerializeField] protected string displayName;
    [SerializeField] protected bool showDebugLog = true;
    [SerializeField] protected AudioClip ObjectClickSFX;

    [Header("호버 효과")]
    [SerializeField] private Color hoverOutlineColor = Color.yellow;
    [SerializeField] private float hoverOutlineThickness = 0.05f;
    [SerializeField] private bool enableHoverEffect = true;

    private GameObject _outlineObject;
    private SpriteRenderer _outlineRenderer;
    private SpriteRenderer _targetRenderer;

    protected virtual void Awake()
    {
        if (enableHoverEffect)
            SetupOutline();
    }

    private void SetupOutline()
    {
        _targetRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_targetRenderer == null) return;

        _outlineObject = new GameObject("HoverOutline");
        _outlineObject.transform.SetParent(_targetRenderer.transform, false);
        _outlineObject.transform.localPosition = Vector3.zero;
        _outlineObject.transform.localScale = Vector3.one;

        _outlineRenderer = _outlineObject.AddComponent<SpriteRenderer>();
        _outlineRenderer.sprite = _targetRenderer.sprite;
        _outlineRenderer.sortingLayerID = _targetRenderer.sortingLayerID;
        _outlineRenderer.sortingOrder = _targetRenderer.sortingOrder - 1;
        _outlineRenderer.color = hoverOutlineColor;

        float scale = 1f + hoverOutlineThickness;
        _outlineObject.transform.localScale = new Vector3(scale, scale, 1f);

        _outlineObject.SetActive(false);
    }

    public virtual string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return gameObject.name;
    }

    public virtual void OnClicked()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.PlaySfxOneShot(ObjectClickSFX);
        if (showDebugLog)
            Debug.Log($"[{GetType().Name}] {GetDisplayName()} 클릭");
    }

    public virtual void OnHoverEnter()
    {
        if (!enableHoverEffect) return;

        if (_outlineRenderer == null)
        {
            SetupOutline();
            if (_outlineRenderer == null) return;
        }

        if (_targetRenderer != null && _outlineRenderer.sprite != _targetRenderer.sprite)
            _outlineRenderer.sprite = _targetRenderer.sprite;

        _outlineObject.SetActive(true);
    }

    public virtual void OnHoverExit()
    {
        if (_outlineObject != null)
            _outlineObject.SetActive(false);
    }
}