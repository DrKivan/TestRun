namespace EvaluaT.Application.Exams;

public interface IExamSessionService
{
    Task<IReadOnlyList<ExamResultSummaryResponse>> ListResultsAsync(CancellationToken cancellationToken);
    Task<ExamSessionResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ExamSessionResponse> StartAsync(StartExamRequest request, CancellationToken cancellationToken);
    Task<AnswerResultResponse> AnswerAsync(
        Guid sessionId,
        AnswerQuestionRequest request,
        CancellationToken cancellationToken);
}
