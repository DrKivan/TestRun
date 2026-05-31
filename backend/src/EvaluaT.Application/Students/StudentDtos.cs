namespace EvaluaT.Application.Students;

public sealed record CreateStudentRequest(string FullName, string Email);

public sealed record StudentResponse(Guid Id, string FullName, string Email, DateTime CreatedAt);
