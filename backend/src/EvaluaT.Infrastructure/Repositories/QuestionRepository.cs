using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Questions;
using EvaluaT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Repositories;

public sealed class QuestionRepository : IQuestionRepository
{
    private readonly EvaluaTDbContext _dbContext;

    public QuestionRepository(EvaluaTDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Questions
            .Include(question => question.Options)
            .SingleOrDefaultAsync(question => question.Id == id, cancellationToken);
    }

    public Task<Question?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Questions
            .Include(question => question.Options)
            .SingleOrDefaultAsync(question => question.Id == id && question.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Questions
            .Include(question => question.Options)
            .OrderBy(question => question.Topic)
            .ThenBy(question => question.Difficulty)
            .ThenBy(question => question.Text)
            .ToListAsync(cancellationToken);
    }

    public Task<Question?> FindNextAsync(
        DifficultyLevel difficulty,
        IReadOnlyCollection<Guid> excludedQuestionIds,
        string? topic,
        string? competency,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Questions
            .Include(question => question.Options)
            .Where(question =>
                question.IsActive
                && question.Difficulty == difficulty
                && !excludedQuestionIds.Contains(question.Id));

        if (!string.IsNullOrWhiteSpace(topic))
        {
            query = query.Where(question => question.Topic == topic);
        }

        if (!string.IsNullOrWhiteSpace(competency))
        {
            query = query.Where(question => question.Competency == competency);
        }

        return query
            .OrderBy(question => question.Topic)
            .ThenBy(question => question.Competency)
            .ThenBy(question => question.Text)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Question question, CancellationToken cancellationToken)
    {
        await _dbContext.Questions.AddAsync(question, cancellationToken);
    }

    public void Remove(Question question)
    {
        _dbContext.Questions.Remove(question);
    }
}
