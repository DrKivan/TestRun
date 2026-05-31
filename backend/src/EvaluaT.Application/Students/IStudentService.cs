namespace EvaluaT.Application.Students;

public interface IStudentService
{
    Task<IReadOnlyList<StudentResponse>> ListAsync(CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
}
