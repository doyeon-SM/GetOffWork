using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILunchResult : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;

    private WorkDayManager workDayManager;

    public void Initialize(WorkDayManager manager, LunchOptionData selectedOption)
    {
        workDayManager = manager;
        BuildResultText(selectedOption);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }
    }

    private void BuildResultText(LunchOptionData selectedOption)
    {
        if (resultText == null)
            return;

        if (selectedOption == null || selectedOption.statChanges == null || selectedOption.statChanges.Count == 0)
        {
            resultText.text = "변화 없음";
            return;
        }

        List<string> lines = new List<string>();

        for (int i = 0; i < selectedOption.statChanges.Count; i++)
        {
            LunchStatChange change = selectedOption.statChanges[i];
            if (change == null)
                continue;

            string sign = change.amount >= 0 ? "+" : "";
            lines.Add($"{change.stat}: {sign}{change.amount}");
        }

        resultText.text = string.Join("\n", lines);
    }

    private void OnClickConfirm()
    {
        if (workDayManager == null)
            return;

        workDayManager.CloseLunchResultUIAndStartAfternoon();
    }
}