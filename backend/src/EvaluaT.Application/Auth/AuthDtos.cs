using EvaluaT.Domain.Auth;

namespace EvaluaT.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterStudentRequest(string FullName, string Email, string Password);

public sealed record AuthUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    Guid? StudentId);

public sealed record AuthResponse(string Token, AuthUserResponse User);
