using EvaluaT.Application.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvaluaT.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/students")]
public sealed class StudentsController : ControllerBase
{
    private readonly IStudentService _students;

    public StudentsController(IStudentService students)
    {
        _students = students;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _students.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<StudentResponse>> Create(
        CreateStudentRequest request,
        CancellationToken cancellationToken)
    {
        var student = await _students.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = student.Id }, student);
    }
}
