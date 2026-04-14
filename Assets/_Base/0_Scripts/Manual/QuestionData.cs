using System;

[Serializable]
public struct QuestionData
{
    public enum CommandVisualType
    {
        QuestionButton,
        ActionButton
    }

    public string CommandId;
    public string DisplayText;
    public CommandVisualType VisualType;

    public QuestionData(string commandId, string displayText, CommandVisualType visualType = CommandVisualType.QuestionButton)
    {
        CommandId = commandId;
        DisplayText = displayText;
        VisualType = visualType;
    }
}