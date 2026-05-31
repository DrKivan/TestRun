using System.Security.Claims;
using EvaluaT.Domain.Auth;
using EvaluaT.Application.Exams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvaluaT.Api.Controllers;

[ApiController]
[Route("api/exam-sessions")]
public sealed class ExamSessionsController : ControllerBase
{
    private readonly IExamSessionService _examSessions;

    public ExamSessionsController(IExamSessionService examSessions)
    {
        _examSessions = examSessions;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExamResultSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _examSessions.ListResultsAsync(cancellationToken));
    }

    [Authorize(Roles = "Teacher,Student")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExamSessionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var session = await _examSessions.GetAsync(id, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        if (!CanAccessSession(session.StudentId))
        {
            return Forbid();
        }

        return Ok(session);
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<ActionResult<ExamSessionResponse>> Start(
        StartExamRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanAccessSession(request.StudentId))
        {
            return Forbid();
        }

        var session = await _examSessions.StartAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = session.Id }, session);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id:guid}/answers")]
    public async Task<ActionResult<AnswerResultResponse>> Answer(
        Guid id,
        AnswerQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _examSessions.AnswerAsync(id, request, cancellationToken);

        if (!CanAccessSession(result.Session.StudentId))
        {
            return Forbid();
        }

        return Ok(result);
    }

    private bool CanAccessSession(Guid studentId)
    {
        if (User.IsInRole(UserRole.Teacher.ToString()))
        {
            return true;
        }

        var claimValue = User.FindFirstValue("studentId");
        return Guid.TryParse(claimValue, out var claimStudentId) && claimStudentId == studentId;
    }
}
