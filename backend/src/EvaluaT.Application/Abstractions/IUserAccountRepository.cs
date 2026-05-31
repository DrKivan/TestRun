using EvaluaT.Domain.Auth;

namespace EvaluaT.Application.Abstractions;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken);
}
