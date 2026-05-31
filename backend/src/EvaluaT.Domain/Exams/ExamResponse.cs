using EvaluaT.Domain.Common;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Exams;

public sealed class ExamResponse : Entity
{
    private ExamResponse()
    {
    }

    public Guid ExamSessionId { get; private set; }
    public Guid QuestionId { get; private set; }
    public int SelectedOptionOrder { get; private set; }
    public bool IsCorrect { get; private set; }
    public DifficultyLevel DifficultyAtAnswer { get; private set; }
    public DateTime AnsweredAt { get; private set; }

    public static ExamResponse Create(
        Guid examSessionId,
        Guid questionId,
        int selectedOptionOrder,
        bool isCorrect,
        DifficultyLevel difficultyAtAnswer,
        DateTime answeredAt)
    {
        return new ExamResponse
        {
            Id = Guid.NewGuid(),
            ExamSessionId = examSessionId,
            QuestionId = questionId,
            SelectedOptionOrder = selectedOptionOrder,
            IsCorrect = isCorrect,
            DifficultyAtAnswer = difficultyAtAnswer,
            AnsweredAt = answeredAt
        };
    }
}
