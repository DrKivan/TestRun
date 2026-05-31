using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Events;

public sealed record QuestionAnsweredEvent(
    Guid SessionId,
    Guid StudentId,
    Guid QuestionId,
    bool IsCorrect,
    DifficultyLevel Difficulty,
    DifficultyLevel NextDifficulty,
    DateTime OccurredAt) : IDomainEvent;
