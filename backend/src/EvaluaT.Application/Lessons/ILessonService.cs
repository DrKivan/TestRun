namespace EvaluaT.Application.Lessons;

public interface ILessonService
{
    Task<IReadOnlyList<LessonResponse>> ListAsync(CancellationToken cancellationToken);
    Task<LessonResponse> CreateAsync(CreateLessonRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
