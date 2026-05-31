using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Students;

namespace EvaluaT.Application.Students;

public sealed class StudentService : IStudentService
{
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StudentService(IStudentRepository students, IUnitOfWork unitOfWork, IClock clock)
    {
        _students = students;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<StudentResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var students = await _students.ListAsync(cancellationToken);
        return students.Select(Map).ToList();
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var existingStudent = await _students.GetByEmailAsync(request.Email, cancellationToken);

        if (existingStudent is not null)
        {
            return Map(existingStudent);
        }

        var student = Student.Create(request.FullName, request.Email, _clock.UtcNow);
        await _students.AddAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(student);
    }

    private static StudentResponse Map(Student student)
    {
        return new StudentResponse(student.Id, student.FullName, student.Email, student.CreatedAt);
    }
}
