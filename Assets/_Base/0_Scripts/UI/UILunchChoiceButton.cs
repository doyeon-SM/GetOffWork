using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILunchChoiceButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    private LunchOptionData currentData;
    private UILunchChoice ownerUI;

    public void Setup(LunchOptionData data, UILunchChoice owner)
    {
        currentData = data;
        ownerUI = owner;

        if (nameText != null)
            nameText.text = currentData != null ? currentData.optionName : string.Empty;

        if (descriptionText != null)
            descriptionText.text = currentData != null ? currentData.description : string.Empty;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickButton);
        }
    }

    private void OnClickButton()
    {
        if (currentData == null || ownerUI == null)
            return;

        ownerUI.SelectOption(currentData);
    }
}