using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Auth;
using EvaluaT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvaluaT.Infrastructure.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly EvaluaTDbContext _dbContext;

    public UserAccountRepository(EvaluaTDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.UserAccounts.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.UserAccounts.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        await _dbContext.UserAccounts.AddAsync(userAccount, cancellationToken);
    }
}
