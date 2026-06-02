using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Abstractions;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Question?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Question>> ListAsync(CancellationToken cancellationToken);
    Task<Question?> FindNextAsync(
        DifficultyLevel difficulty,
        IReadOnlyCollection<Guid> excludedQuestionIds,
        string? topic,
        string? competency,
        CancellationToken cancellationToken);

    Task AddAsync(Question question, CancellationToken cancellationToken);
    void Remove(Question question);
}
