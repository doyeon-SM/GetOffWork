using UnityEngine;

public abstract class ClickableWorldObject : MonoBehaviour, IClickableObject
{
    [Header("기본 정보")]
    [SerializeField] protected string displayName;
    [SerializeField] protected bool showDebugLog = true;

    protected virtual void Awake()
    {

    }

    public virtual string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return gameObject.name;
    }

    public virtual void OnClicked()
    {
        if (showDebugLog)
            Debug.Log($"[{GetType().Name}] {GetDisplayName()} 클릭됨");
    }
}