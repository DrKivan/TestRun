using EvaluaT.Domain.Lessons;

namespace EvaluaT.Application.Abstractions;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken);
}
