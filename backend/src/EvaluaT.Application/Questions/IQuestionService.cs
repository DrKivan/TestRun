namespace EvaluaT.Application.Questions;

public interface IQuestionService
{
    Task<IReadOnlyList<QuestionResponse>> ListAsync(CancellationToken cancellationToken);
    Task<QuestionResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken);
    Task<QuestionResponse?> UpdateAsync(Guid id, UpdateQuestionRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
