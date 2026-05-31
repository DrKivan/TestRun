using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Exams;
using EvaluaT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Repositories;

public sealed class ExamSessionRepository : IExamSessionRepository
{
    private readonly EvaluaTDbContext _dbContext;

    public ExamSessionRepository(EvaluaTDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ExamSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.ExamSessions
            .Include(session => session.Responses)
            .SingleOrDefaultAsync(session => session.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamSession>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ExamSessions
            .Include(session => session.Responses)
            .OrderByDescending(session => session.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExamSession session, CancellationToken cancellationToken)
    {
        await _dbContext.ExamSessions.AddAsync(session, cancellationToken);
    }
}
