using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Lessons;

namespace EvaluaT.Application.Lessons;

public sealed class LessonService : ILessonService
{
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;

    public LessonService(ILessonRepository lessons, IUnitOfWork unitOfWork)
    {
        _lessons = lessons;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LessonResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var lessons = await _lessons.ListAsync(cancellationToken);
        return lessons.Select(Map).ToList();
    }

    public async Task<LessonResponse> CreateAsync(CreateLessonRequest request, CancellationToken cancellationToken)
    {
        var lesson = Lesson.Create(
            request.Topic,
            request.Competency,
            request.Type,
            request.Title,
            request.Content,
            request.ResourceUrl);

        await _lessons.AddAsync(lesson, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(lesson);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _lessons.GetByIdAsync(id, cancellationToken);

        if (lesson is null)
        {
            return false;
        }

        lesson.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LessonResponse Map(Lesson lesson)
    {
        return new LessonResponse(
            lesson.Id,
            lesson.Topic,
            lesson.Competency,
            lesson.Type,
            lesson.Title,
            lesson.Content,
            lesson.ResourceUrl,
            lesson.IsActive);
    }
}
