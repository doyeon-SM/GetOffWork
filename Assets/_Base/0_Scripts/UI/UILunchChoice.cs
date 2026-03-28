using System.Collections.Generic;
using UnityEngine;

public class UILunchChoice : MonoBehaviour
{
    [Header("¹öÆ° 3°³")]
    [SerializeField] private List<UILunchChoiceButton> choiceButtons = new List<UILunchChoiceButton>(3);

    private WorkDayManager workDayManager;
    private List<LunchOptionData> currentOptions = new List<LunchOptionData>();

    public void Initialize(WorkDayManager manager, List<LunchOptionData> selectedOptions)
    {
        workDayManager = manager;
        currentOptions = selectedOptions;

        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (choiceButtons[i] == null)
                continue;

            bool hasData = i < currentOptions.Count && currentOptions[i] != null;
            choiceButtons[i].gameObject.SetActive(hasData);

            if (hasData)
            {
                choiceButtons[i].Setup(currentOptions[i], this);
            }
        }
    }

    public void SelectOption(LunchOptionData optionData)
    {
        if (workDayManager == null || optionData == null)
            return;

        workDayManager.OnLunchOptionSelected(optionData);
    }
}