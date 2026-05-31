using EvaluaT.Domain.Auth;

namespace EvaluaT.Application.Auth;

public interface ITokenGenerator
{
    string Generate(UserAccount userAccount);
}
