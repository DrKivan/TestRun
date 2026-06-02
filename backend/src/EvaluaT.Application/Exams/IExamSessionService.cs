namespace EvaluaT.Application.Exams;

public interface IExamSessionService
{
    Task<IReadOnlyList<ExamResultSummaryResponse>> ListResultsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ExamResultSummaryResponse>> ListResultsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken);
    Task<ExamAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken);
    Task<ExamSessionResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ExamSessionResponse> StartAsync(StartExamRequest request, CancellationToken cancellationToken);
    Task<AnswerResultResponse> AnswerAsync(
        Guid sessionId,
        AnswerQuestionRequest request,
        CancellationToken cancellationToken);
}
