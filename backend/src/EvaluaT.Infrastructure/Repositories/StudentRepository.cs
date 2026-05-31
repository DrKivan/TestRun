using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Students;
using EvaluaT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Repositories;

public sealed class StudentRepository : IStudentRepository
{
    private readonly EvaluaTDbContext _dbContext;

    public StudentRepository(EvaluaTDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Students.SingleOrDefaultAsync(student => student.Id == id, cancellationToken);
    }

    public Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Students.SingleOrDefaultAsync(student => student.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Students
            .OrderBy(student => student.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken)
    {
        await _dbContext.Students.AddAsync(student, cancellationToken);
    }
}
