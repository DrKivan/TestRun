using EvaluaT.Application.Abstractions;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Questions;

public sealed class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questions;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(IQuestionRepository questions, IUnitOfWork unitOfWork)
    {
        _questions = questions;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<QuestionResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var questions = await _questions.ListAsync(cancellationToken);
        return questions.Select(Map).ToList();
    }

    public async Task<QuestionResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);
        return question is null ? null : Map(question);
    }

    public async Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        var question = Question.Create(
            request.Topic,
            request.Competency,
            request.Text,
            request.Difficulty,
            request.Options.Select(option => (option.Text, option.IsCorrect)));

        await _questions.AddAsync(question, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(question);
    }

    public async Task<QuestionResponse?> UpdateAsync(
        Guid id,
        UpdateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);

        if (question is null)
        {
            return null;
        }

        question.Update(
            request.Topic,
            request.Competency,
            request.Text,
            request.Difficulty,
            request.Options.Select(option => (option.Text, option.IsCorrect)));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(question);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);

        if (question is null)
        {
            return false;
        }

        question.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    internal static QuestionResponse Map(Question question)
    {
        return new QuestionResponse(
            question.Id,
            question.Topic,
            question.Competency,
            question.Text,
            question.Difficulty,
            question.IsActive,
            question.Options
                .OrderBy(option => option.Order)
                .Select(option => new OptionResponse(option.Id, option.Order, option.Text, option.IsCorrect))
                .ToList());
    }

    internal static CurrentQuestionResponse MapCurrent(Question question)
    {
        return new CurrentQuestionResponse(
            question.Id,
            question.Topic,
            question.Competency,
            question.Text,
            question.Difficulty,
            question.Options
                .OrderBy(option => option.Order)
                .Select(option => new PublicOptionResponse(option.Order, option.Text))
                .ToList());
    }
}
