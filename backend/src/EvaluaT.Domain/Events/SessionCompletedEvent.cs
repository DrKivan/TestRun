namespace EvaluaT.Domain.Events;

public sealed record SessionCompletedEvent(
    Guid SessionId,
    Guid StudentId,
    decimal ScorePercentage,
    DateTime OccurredAt) : IDomainEvent;
