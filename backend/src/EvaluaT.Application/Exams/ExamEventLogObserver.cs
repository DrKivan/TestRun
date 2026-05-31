using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Events;
using Microsoft.Extensions.Logging;

namespace EvaluaT.Application.Exams;

public sealed class ExamEventLogObserver : IDomainEventObserver
{
    private readonly ILogger<ExamEventLogObserver> _logger;

    public ExamEventLogObserver(ILogger<ExamEventLogObserver> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case QuestionAnsweredEvent answered:
                _logger.LogInformation(
                    "Question {QuestionId} answered in session {SessionId}. Correct: {IsCorrect}. Next difficulty: {NextDifficulty}.",
                    answered.QuestionId,
                    answered.SessionId,
                    answered.IsCorrect,
                    answered.NextDifficulty);
                break;
            case SessionCompletedEvent completed:
                _logger.LogInformation(
                    "Session {SessionId} completed by student {StudentId}. Score: {ScorePercentage}%.",
                    completed.SessionId,
                    completed.StudentId,
                    completed.ScorePercentage);
                break;
        }

        return Task.CompletedTask;
    }
}
