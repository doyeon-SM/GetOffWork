using System.Collections.Generic;
using UnityEngine;

public class UIManualPanel : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    [Header("패널")]
    [SerializeField] private GameObject root;

    [Header("질문 버튼 생성 영역")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UIQuestionButton questionButtonPrefab;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    private void Awake()
    {
        if (serviceDeskManager == null)
            serviceDeskManager = FindFirstObjectByType<ServiceDeskManager>();

        Hide();
    }

    public void OnClickOpenQuestionPanel()
    {
        if (serviceDeskManager == null || serviceDeskManager.CurrentManual == null)
            return;

        RebuildButtons();
        Show();
    }

    public void OnClickCloseQuestionPanel()
    {
        Hide();
    }

    private void RebuildButtons()
    {
        ClearButtons();

        var manual = serviceDeskManager.CurrentManual;
        if (manual == null)
            return;

        var commandList = manual.CommandList;
        if (commandList == null)
            return;

        for (int i = 0; i < commandList.Count; i++)
        {
            UIQuestionButton button = Instantiate(questionButtonPrefab, contentRoot);
            button.Setup(commandList[i], HandleQuestionClicked);
            spawnedButtons.Add(button.gameObject);
        }
    }

    private void HandleQuestionClicked(string commandId)
    {
        if (serviceDeskManager == null)
            return;

        serviceDeskManager.ExecuteCommand(commandId);
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i]);
        }

        spawnedButtons.Clear();
    }

    private void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    private void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}