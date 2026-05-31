using EvaluaT.Application.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvaluaT.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/questions")]
public sealed class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questions;

    public QuestionsController(IQuestionService questions)
    {
        _questions = questions;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuestionResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _questions.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuestionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var question = await _questions.GetAsync(id, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost]
    public async Task<ActionResult<QuestionResponse>> Create(
        CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = await _questions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = question.Id }, question);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuestionResponse>> Update(
        Guid id,
        UpdateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = await _questions.UpdateAsync(id, request, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _questions.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
