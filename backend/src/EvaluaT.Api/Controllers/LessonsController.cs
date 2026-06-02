using EvaluaT.Application.Lessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvaluaT.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher,Student")]
[Route("api/lessons")]
public sealed class LessonsController : ControllerBase
{
    private readonly ILessonService _lessons;

    public LessonsController(ILessonService lessons)
    {
        _lessons = lessons;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _lessons.ListAsync(cancellationToken));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<ActionResult<LessonResponse>> Create(
        CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await _lessons.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = lesson.Id }, lesson);
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _lessons.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
