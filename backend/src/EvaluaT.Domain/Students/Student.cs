using EvaluaT.Domain.Common;

namespace EvaluaT.Domain.Students;

public sealed class Student : Entity
{
    private Student()
    {
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static Student Create(string fullName, string email, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Student name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("A valid email is required.", nameof(email));
        }

        return new Student
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            CreatedAt = createdAt
        };
    }
}
