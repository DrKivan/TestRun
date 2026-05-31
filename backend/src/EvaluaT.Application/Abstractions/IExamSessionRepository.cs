using EvaluaT.Domain.Exams;

namespace EvaluaT.Application.Abstractions;

public interface IExamSessionRepository
{
    Task<ExamSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExamSession>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(ExamSession session, CancellationToken cancellationToken);
}
