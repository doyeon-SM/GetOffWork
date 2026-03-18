using System;

[Serializable]
public struct QuestionData
{
    public string QuestionId;
    public string DisplayText;

    public QuestionData(string questionId, string displayText)
    {
        QuestionId = questionId;
        DisplayText = displayText;
    }
}