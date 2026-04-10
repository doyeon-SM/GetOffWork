using UnityEngine;

public class UIMonitorView : MonoBehaviour
{
    [Tooltip("UIMonitorController Prefab")]
    [SerializeField] private UIMonitorController monitorControllerPrefab;

    private UIMonitorController _instance;


    private void Awake()
    {
        if (monitorControllerPrefab == null)
        {
            Debug.LogError("[UIMonitorView] monitorControllerPrefab not assigned.");
            return;
        }

        var canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        _instance = Instantiate(monitorControllerPrefab, parent);
        _instance.gameObject.name = "UIMonitorController(Runtime)";
    }

    public void Open()
    {
        _instance?.Open();
    }

    public void Hide()
    {
        _instance?.Close();
    }
}
