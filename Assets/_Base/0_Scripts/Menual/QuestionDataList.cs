using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestionDataList", menuName = "Game/Manual/Question")]
public class QuestionDataList : ScriptableObject
{
    [SerializeField] private List<QuestionData> questionList = new();

    public IReadOnlyList<QuestionData> QuestionList => questionList;
}
