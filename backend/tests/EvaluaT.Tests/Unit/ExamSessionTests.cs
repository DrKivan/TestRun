using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Events;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Tests.Unit;

public sealed class ExamSessionTests
{
    [Fact]
    public void AnswerCurrentQuestion_CompletesSession_WhenMaxQuestionsIsReached()
    {
        var question = Question.Create(
            "Programacion",
            "Que patron encapsula algoritmos intercambiables?",
            DifficultyLevel.Medium,
            [("Factory", false), ("Strategy", true), ("Singleton", false)]);
        var session = ExamSession.Start(
            Guid.NewGuid(),
            question,
            maxQuestions: 1,
            DifficultyPolicy.Balanced,
            DateTime.UtcNow);

        session.AnswerCurrentQuestion(
            question,
            selectedOptionOrder: 1,
            DifficultyLevel.Hard,
            nextQuestion: null,
            DateTime.UtcNow);

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.Equal(100m, session.ScorePercentage);
        Assert.Contains(session.DomainEvents, domainEvent => domainEvent is SessionCompletedEvent);
    }
}
