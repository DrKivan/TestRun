using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Lessons;
using EvaluaT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Repositories;

public sealed class LessonRepository : ILessonRepository
{
    private readonly EvaluaTDbContext _dbContext;

    public LessonRepository(EvaluaTDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Lesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Lessons.SingleOrDefaultAsync(lesson => lesson.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Lessons
            .OrderBy(lesson => lesson.Topic)
            .ThenBy(lesson => lesson.Competency)
            .ThenBy(lesson => lesson.Type)
            .ThenBy(lesson => lesson.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        await _dbContext.Lessons.AddAsync(lesson, cancellationToken);
    }
}
