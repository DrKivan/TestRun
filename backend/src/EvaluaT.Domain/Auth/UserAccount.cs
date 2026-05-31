using EvaluaT.Domain.Common;

namespace EvaluaT.Domain.Auth;

public sealed class UserAccount : Entity
{
    private UserAccount()
    {
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public Guid? StudentId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static UserAccount CreateTeacher(
        string fullName,
        string email,
        string passwordHash,
        DateTime createdAt)
    {
        return Create(fullName, email, passwordHash, UserRole.Teacher, null, createdAt);
    }

    public static UserAccount CreateStudent(
        string fullName,
        string email,
        string passwordHash,
        Guid studentId,
        DateTime createdAt)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.", nameof(studentId));
        }

        return Create(fullName, email, passwordHash, UserRole.Student, studentId, createdAt);
    }

    private static UserAccount Create(
        string fullName,
        string email,
        string passwordHash,
        UserRole role,
        Guid? studentId,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("A valid email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            StudentId = studentId,
            CreatedAt = createdAt
        };
    }
}
