using EvaluaT.Domain.Lessons;

namespace EvaluaT.Application.Lessons;

public sealed record CreateLessonRequest(
    string Topic,
    string? Competency,
    LessonType Type,
    string Title,
    string Content,
    string? ResourceUrl);

public sealed record LessonResponse(
    Guid Id,
    string Topic,
    string? Competency,
    LessonType Type,
    string Title,
    string Content,
    string? ResourceUrl,
    bool IsActive);
