using EvaluaT.Application.Abstractions;
using EvaluaT.Application.Questions;
using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Exams;

public sealed class ExamSessionService : IExamSessionService
{
    private static readonly DifficultyLevel[] FallbackDifficultyOrder =
    [
        DifficultyLevel.Easy,
        DifficultyLevel.Medium,
        DifficultyLevel.Hard
    ];

    private readonly IStudentRepository _students;
    private readonly IQuestionRepository _questions;
    private readonly IExamSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly DifficultyAdjustmentStrategyFactory _strategyFactory;
    private readonly IDomainEventPublisher _domainEventPublisher;

    public ExamSessionService(
        IStudentRepository students,
        IQuestionRepository questions,
        IExamSessionRepository sessions,
        IUnitOfWork unitOfWork,
        IClock clock,
        DifficultyAdjustmentStrategyFactory strategyFactory,
        IDomainEventPublisher domainEventPublisher)
    {
        _students = students;
        _questions = questions;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _strategyFactory = strategyFactory;
        _domainEventPublisher = domainEventPublisher;
    }

    public async Task<ExamSessionResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(id, cancellationToken);

        if (session is null)
        {
            return null;
        }

        var currentQuestion = session.CurrentQuestionId is null
            ? null
            : await _questions.GetActiveByIdAsync(session.CurrentQuestionId.Value, cancellationToken);

        return Map(session, currentQuestion);
    }

    public async Task<IReadOnlyList<ExamResultSummaryResponse>> ListResultsAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessions.ListAsync(cancellationToken);
        var students = await _students.ListAsync(cancellationToken);
        var studentsById = students.ToDictionary(student => student.Id);

        return sessions
            .OrderByDescending(session => session.StartedAt)
            .Select(session =>
            {
                var studentName = studentsById.TryGetValue(session.StudentId, out var student)
                    ? student.FullName
                    : "Estudiante no encontrado";

                return new ExamResultSummaryResponse(
                    session.Id,
                    session.StudentId,
                    studentName,
                    session.Policy,
                    session.Status,
                    session.Responses.Count,
                    session.MaxQuestions,
                    session.ScorePercentage,
                    session.StartedAt,
                    session.CompletedAt);
            })
            .ToList();
    }

    public async Task<ExamSessionResponse> StartAsync(
        StartExamRequest request,
        CancellationToken cancellationToken)
    {
        var student = await _students.GetByIdAsync(request.StudentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student was not found.");

        var maxQuestions = request.MaxQuestions <= 0 ? 5 : Math.Min(request.MaxQuestions, 20);
        var strategy = _strategyFactory.Create(request.Policy);
        var firstQuestion = await FindQuestionWithFallbackAsync(
            strategy.InitialDifficulty,
            Array.Empty<Guid>(),
            cancellationToken)
            ?? throw new InvalidOperationException("There are no active questions available.");

        var session = ExamSession.Start(
            student.Id,
            firstQuestion,
            maxQuestions,
            request.Policy,
            _clock.UtcNow);

        await _sessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(session, firstQuestion);
    }

    public async Task<AnswerResultResponse> AnswerAsync(
        Guid sessionId,
        AnswerQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Exam session was not found.");

        if (session.Status == SessionStatus.Completed)
        {
            throw new InvalidOperationException("The exam session is already completed.");
        }

        if (session.CurrentQuestionId != request.QuestionId)
        {
            throw new InvalidOperationException("The submitted question is not the current question.");
        }

        var currentQuestion = await _questions.GetActiveByIdAsync(request.QuestionId, cancellationToken)
            ?? throw new KeyNotFoundException("Current question was not found.");

        var isCorrect = currentQuestion.Evaluate(request.SelectedOptionOrder);
        var strategy = _strategyFactory.Create(session.Policy);
        var previousResults = session.Responses.Select(response => response.IsCorrect).ToList();
        var nextDifficulty = strategy.GetNextDifficulty(new DifficultyAdjustmentContext(
            currentQuestion.Difficulty,
            isCorrect,
            previousResults));

        var answeredQuestionIds = session.Responses
            .Select(response => response.QuestionId)
            .Append(currentQuestion.Id)
            .ToList();

        var shouldContinue = session.Responses.Count + 1 < session.MaxQuestions;
        var nextQuestion = shouldContinue
            ? await FindQuestionWithFallbackAsync(nextDifficulty, answeredQuestionIds, cancellationToken)
            : null;

        session.AnswerCurrentQuestion(
            currentQuestion,
            request.SelectedOptionOrder,
            nextDifficulty,
            nextQuestion,
            _clock.UtcNow);

        var domainEvents = session.DomainEvents.ToList();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _domainEventPublisher.PublishAsync(domainEvents, cancellationToken);
        session.ClearDomainEvents();

        return new AnswerResultResponse(
            isCorrect,
            currentQuestion.Difficulty,
            session.CurrentDifficulty,
            session.Status,
            session.ScorePercentage,
            nextQuestion is null ? null : QuestionService.MapCurrent(nextQuestion),
            Map(session, nextQuestion));
    }

    private async Task<Question?> FindQuestionWithFallbackAsync(
        DifficultyLevel preferredDifficulty,
        IReadOnlyCollection<Guid> excludedQuestionIds,
        CancellationToken cancellationToken)
    {
        var preferredQuestion = await _questions.FindNextAsync(
            preferredDifficulty,
            excludedQuestionIds,
            cancellationToken);

        if (preferredQuestion is not null)
        {
            return preferredQuestion;
        }

        foreach (var fallbackDifficulty in FallbackDifficultyOrder.Where(difficulty => difficulty != preferredDifficulty))
        {
            var fallbackQuestion = await _questions.FindNextAsync(
                fallbackDifficulty,
                excludedQuestionIds,
                cancellationToken);

            if (fallbackQuestion is not null)
            {
                return fallbackQuestion;
            }
        }

        return null;
    }

    private static ExamSessionResponse Map(ExamSession session, Question? currentQuestion)
    {
        return new ExamSessionResponse(
            session.Id,
            session.StudentId,
            session.Policy,
            session.CurrentDifficulty,
            session.Status,
            session.MaxQuestions,
            session.Responses.Count,
            session.ScorePercentage,
            session.StartedAt,
            session.CompletedAt,
            currentQuestion is null ? null : QuestionService.MapCurrent(currentQuestion),
            session.Responses
                .OrderBy(response => response.AnsweredAt)
                .Select(response => new ExamResponseItem(
                    response.QuestionId,
                    response.SelectedOptionOrder,
                    response.IsCorrect,
                    response.DifficultyAtAnswer,
                    response.AnsweredAt))
                .ToList());
    }
}
