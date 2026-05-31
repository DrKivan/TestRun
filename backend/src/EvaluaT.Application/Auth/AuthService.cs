using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Auth;
using EvaluaT.Domain.Students;

namespace EvaluaT.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserAccountRepository _users;
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthService(
        IUserAccountRepository users,
        IStudentRepository students,
        IUnitOfWork unitOfWork,
        IClock clock,
        ITokenGenerator tokenGenerator)
    {
        _users = users;
        _students = students;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return Map(user);
    }

    public async Task<AuthResponse> RegisterStudentAsync(
        RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        var existingUser = await _users.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var existingStudent = await _students.GetByEmailAsync(request.Email, cancellationToken);
        var student = existingStudent ?? Student.Create(request.FullName, request.Email, _clock.UtcNow);

        if (existingStudent is null)
        {
            await _students.AddAsync(student, cancellationToken);
        }

        var user = UserAccount.CreateStudent(
            request.FullName,
            request.Email,
            PasswordHasher.Hash(request.Password),
            student.Id,
            _clock.UtcNow);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(user);
    }

    private AuthResponse Map(UserAccount userAccount)
    {
        return new AuthResponse(
            _tokenGenerator.Generate(userAccount),
            new AuthUserResponse(
                userAccount.Id,
                userAccount.FullName,
                userAccount.Email,
                userAccount.Role,
                userAccount.StudentId));
    }
}
