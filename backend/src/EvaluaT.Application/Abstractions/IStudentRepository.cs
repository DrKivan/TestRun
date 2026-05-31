using EvaluaT.Domain.Students;

namespace EvaluaT.Application.Abstractions;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<Student>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(Student student, CancellationToken cancellationToken);
}
