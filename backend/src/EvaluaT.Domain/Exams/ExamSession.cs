using EvaluaT.Domain.Common;
using EvaluaT.Domain.Events;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Exams;

public sealed class ExamSession : Entity
{
    private readonly List<ExamResponse> _responses = new();

    private ExamSession()
    {
    }

    public Guid StudentId { get; private set; }
    public DifficultyLevel CurrentDifficulty { get; private set; }
    public Guid? CurrentQuestionId { get; private set; }
    public int MaxQuestions { get; private set; }
    public DifficultyPolicy Policy { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public IReadOnlyCollection<ExamResponse> Responses => _responses.AsReadOnly();

    public decimal ScorePercentage =>
        _responses.Count == 0
            ? 0
            : decimal.Round(_responses.Count(response => response.IsCorrect) * 100m / _responses.Count, 2);

    public static ExamSession Start(
        Guid studentId,
        Question firstQuestion,
        int maxQuestions,
        DifficultyPolicy policy,
        DateTime startedAt)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.", nameof(studentId));
        }

        if (maxQuestions < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQuestions), "An exam requires at least one question.");
        }

        return new ExamSession
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CurrentDifficulty = firstQuestion.Difficulty,
            CurrentQuestionId = firstQuestion.Id,
            MaxQuestions = maxQuestions,
            Policy = policy,
            Status = SessionStatus.InProgress,
            StartedAt = startedAt
        };
    }

    public ExamResponse AnswerCurrentQuestion(
        Question question,
        int selectedOptionOrder,
        DifficultyLevel nextDifficulty,
        Question? nextQuestion,
        DateTime answeredAt)
    {
        if (Status == SessionStatus.Completed)
        {
            throw new InvalidOperationException("The exam session is already completed.");
        }

        if (CurrentQuestionId != question.Id)
        {
            throw new InvalidOperationException("The submitted question is not the current question.");
        }

        var isCorrect = question.Evaluate(selectedOptionOrder);
        var response = ExamResponse.Create(
            Id,
            question.Id,
            selectedOptionOrder,
            isCorrect,
            question.Difficulty,
            answeredAt);

        _responses.Add(response);
        CurrentDifficulty = nextDifficulty;

        AddDomainEvent(new QuestionAnsweredEvent(
            Id,
            StudentId,
            question.Id,
            isCorrect,
            question.Difficulty,
            nextDifficulty,
            answeredAt));

        if (_responses.Count >= MaxQuestions || nextQuestion is null)
        {
            CurrentQuestionId = null;
            Status = SessionStatus.Completed;
            CompletedAt = answeredAt;

            AddDomainEvent(new SessionCompletedEvent(Id, StudentId, ScorePercentage, answeredAt));
        }
        else
        {
            CurrentQuestionId = nextQuestion.Id;
        }

        return response;
    }
}
