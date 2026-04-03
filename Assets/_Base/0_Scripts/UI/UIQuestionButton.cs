using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UIQuestionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private string commandId;
    private Action<string> onClick;

    public void Setup(QuestionData data, Action<string> clickCallback)
    {
        commandId = data.CommandId;
        onClick = clickCallback;

        if (label != null)
            label.text = data.DisplayText;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke(commandId);
    }
}