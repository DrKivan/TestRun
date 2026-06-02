using EvaluaT.Application.Abstractions;
using EvaluaT.Application.Exams.Diagnostics;
using EvaluaT.Application.Questions;
using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Exams;

public sealed class ExamSessionService : IExamSessionService
{
    private static readonly string[] DiagnosticTopics = ["Matematica", "Programacion", "Ciencias"];

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
    private readonly ITopicDiagnosticStrategy _topicDiagnosticStrategy;

    public ExamSessionService(
        IStudentRepository students,
        IQuestionRepository questions,
        IExamSessionRepository sessions,
        IUnitOfWork unitOfWork,
        IClock clock,
        DifficultyAdjustmentStrategyFactory strategyFactory,
        IDomainEventPublisher domainEventPublisher,
        ITopicDiagnosticStrategy topicDiagnosticStrategy)
    {
        _students = students;
        _questions = questions;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _strategyFactory = strategyFactory;
        _domainEventPublisher = domainEventPublisher;
        _topicDiagnosticStrategy = topicDiagnosticStrategy;
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
        var questions = await _questions.ListAsync(cancellationToken);

        return Map(session, currentQuestion, questions);
    }

    public async Task<IReadOnlyList<ExamResultSummaryResponse>> ListResultsAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessions.ListAsync(cancellationToken);
        return await MapResultsAsync(sessions, cancellationToken);
    }

    public async Task<IReadOnlyList<ExamResultSummaryResponse>> ListResultsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var sessions = await _sessions.ListAsync(cancellationToken);
        return await MapResultsAsync(
            sessions.Where(session => session.StudentId == studentId).ToList(),
            cancellationToken);
    }

    public async Task<ExamAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken)
    {
        var sessions = (await _sessions.ListAsync(cancellationToken))
            .Where(session => session.Kind == ExamSessionKind.Standard)
            .Where(session => session.Status == SessionStatus.Completed)
            .ToList();
        var questions = await _questions.ListAsync(cancellationToken);
        var questionsById = questions.ToDictionary(question => question.Id);
        var answeredItems = sessions
            .SelectMany(session => session.Responses)
            .Where(response => questionsById.ContainsKey(response.QuestionId))
            .Select(response => new { Response = response, Question = questionsById[response.QuestionId] })
            .ToList();

        var topics = DiagnosticTopics
            .Select(topic =>
            {
                var topicItems = answeredItems
                    .Where(item => string.Equals(item.Question.Topic, topic, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var answerCount = topicItems.Count;
                var incorrectCount = topicItems.Count(item => !item.Response.IsCorrect);

                return new TopicAnalyticsResponse(
                    topic,
                    answerCount,
                    incorrectCount,
                    CalculatePercentage(incorrectCount, answerCount));
            })
            .ToList();

        var questionAnalytics = answeredItems
            .GroupBy(item => item.Question.Id)
            .Select(group =>
            {
                var question = group.First().Question;
                var answerCount = group.Count();
                var incorrectCount = group.Count(item => !item.Response.IsCorrect);

                return new QuestionAnalyticsResponse(
                    question.Id,
                    question.Topic,
                    question.Competency,
                    question.Text,
                    question.Difficulty,
                    answerCount,
                    incorrectCount,
                    CalculatePercentage(incorrectCount, answerCount));
            })
            .OrderByDescending(item => item.ErrorPercentage)
            .ThenByDescending(item => item.IncorrectCount)
            .ThenBy(item => item.Topic)
            .Take(10)
            .ToList();

        return new ExamAnalyticsResponse(topics, questionAnalytics);
    }

    private async Task<IReadOnlyList<ExamResultSummaryResponse>> MapResultsAsync(
        IReadOnlyCollection<ExamSession> sessions,
        CancellationToken cancellationToken)
    {
        var students = await _students.ListAsync(cancellationToken);
        var questions = await _questions.ListAsync(cancellationToken);
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
                    session.Kind,
                    session.TargetTopic,
                    session.TargetCompetency,
                    session.Status,
                    session.Responses.Count,
                    session.MaxQuestions,
                    CountCorrectAnswers(session),
                    session.ScorePercentage,
                    session.StartedAt,
                    session.CompletedAt,
                    BuildDiagnostic(session, questions));
            })
            .ToList();
    }

    public async Task<ExamSessionResponse> StartAsync(
        StartExamRequest request,
        CancellationToken cancellationToken)
    {
        var student = await _students.GetByIdAsync(request.StudentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student was not found.");

        var maxQuestions = request.Kind == ExamSessionKind.Standard
            ? 10
            : Math.Clamp(request.MaxQuestions <= 0 ? 3 : request.MaxQuestions, 1, 5);

        if (!string.IsNullOrWhiteSpace(request.TargetTopic))
        {
            TopicCatalog.EnsureAllowed(request.TargetTopic);
        }

        var strategy = _strategyFactory.Create(request.Policy);
        var firstQuestion = await FindQuestionWithFallbackAsync(
            strategy.InitialDifficulty,
            Array.Empty<Guid>(),
            request.TargetTopic,
            request.TargetCompetency,
            cancellationToken)
            ?? throw new InvalidOperationException("There are no active questions available.");

        var session = ExamSession.Start(
            student.Id,
            firstQuestion,
            maxQuestions,
            request.Policy,
            request.Kind,
            request.TargetTopic,
            request.TargetCompetency,
            _clock.UtcNow);

        await _sessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(session, firstQuestion, [firstQuestion]);
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
            ? await FindQuestionWithFallbackAsync(
                nextDifficulty,
                answeredQuestionIds,
                session.TargetTopic,
                session.TargetCompetency,
                cancellationToken)
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

        var questions = await _questions.ListAsync(cancellationToken);

        return new AnswerResultResponse(
            isCorrect,
            currentQuestion.Difficulty,
            session.CurrentDifficulty,
            session.Status,
            session.ScorePercentage,
            nextQuestion is null ? null : QuestionService.MapCurrent(nextQuestion),
            Map(session, nextQuestion, questions));
    }

    private async Task<Question?> FindQuestionWithFallbackAsync(
        DifficultyLevel preferredDifficulty,
        IReadOnlyCollection<Guid> excludedQuestionIds,
        string? targetTopic,
        string? targetCompetency,
        CancellationToken cancellationToken)
    {
        var preferredQuestion = await FindQuestionByFocusAsync(
            preferredDifficulty,
            excludedQuestionIds,
            targetTopic,
            targetCompetency,
            cancellationToken);

        if (preferredQuestion is not null)
        {
            return preferredQuestion;
        }

        foreach (var fallbackDifficulty in FallbackDifficultyOrder.Where(difficulty => difficulty != preferredDifficulty))
        {
            var fallbackQuestion = await FindQuestionByFocusAsync(
                fallbackDifficulty,
                excludedQuestionIds,
                targetTopic,
                targetCompetency,
                cancellationToken);

            if (fallbackQuestion is not null)
            {
                return fallbackQuestion;
            }
        }

        return null;
    }

    private async Task<Question?> FindQuestionByFocusAsync(
        DifficultyLevel difficulty,
        IReadOnlyCollection<Guid> excludedQuestionIds,
        string? targetTopic,
        string? targetCompetency,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(targetCompetency))
        {
            var competencyQuestion = await _questions.FindNextAsync(
                difficulty,
                excludedQuestionIds,
                targetTopic,
                targetCompetency,
                cancellationToken);

            if (competencyQuestion is not null)
            {
                return competencyQuestion;
            }
        }

        return await _questions.FindNextAsync(
            difficulty,
            excludedQuestionIds,
            targetTopic,
            null,
            cancellationToken);
    }

    private ExamSessionResponse Map(
        ExamSession session,
        Question? currentQuestion,
        IReadOnlyCollection<Question> questions)
    {
        return new ExamSessionResponse(
            session.Id,
            session.StudentId,
            session.Policy,
            session.Kind,
            session.TargetTopic,
            session.TargetCompetency,
            session.CurrentDifficulty,
            session.Status,
            session.MaxQuestions,
            session.Responses.Count,
            CountCorrectAnswers(session),
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
                .ToList(),
            BuildDiagnostic(session, questions),
            BuildErrorReview(session, questions));
    }

    private static IReadOnlyList<ErrorReviewItemResponse> BuildErrorReview(
        ExamSession session,
        IReadOnlyCollection<Question> questions)
    {
        if (session.Status != SessionStatus.Completed)
        {
            return [];
        }

        var questionsById = questions.ToDictionary(question => question.Id);

        return session.Responses
            .Where(response => !response.IsCorrect)
            .Where(response => questionsById.ContainsKey(response.QuestionId))
            .OrderBy(response => response.AnsweredAt)
            .Select(response =>
            {
                var question = questionsById[response.QuestionId];
                var selectedOption = question.Options.Single(option => option.Order == response.SelectedOptionOrder);
                var correctOption = question.Options.Single(option => option.IsCorrect);

                return new ErrorReviewItemResponse(
                    question.Id,
                    question.Topic,
                    question.Text,
                    selectedOption.Order,
                    selectedOption.Text,
                    correctOption.Order,
                    correctOption.Text,
                    BuildExplanation(question.Topic, correctOption.Text));
            })
            .ToList();
    }

    private static string BuildExplanation(string topic, string correctAnswer)
    {
        return $"La respuesta correcta era '{correctAnswer}'. Repasa {topic} y vuelve a resolver una guia de refuerzo para afianzar este tipo de pregunta.";
    }

    private static decimal CalculatePercentage(int count, int total)
    {
        return total == 0 ? 0 : Math.Round(count * 100m / total, 0);
    }

    private static int CountCorrectAnswers(ExamSession session)
    {
        return session.Responses.Count(response => response.IsCorrect);
    }

    private IReadOnlyList<CompetencyDiagnosticResponse> BuildDiagnostic(
        ExamSession session,
        IReadOnlyCollection<Question> questions)
    {
        var questionsById = questions.ToDictionary(question => question.Id);
        var answeredItems = new List<(ExamResponse Response, Question Question)>();

        foreach (var response in session.Responses)
        {
            if (questionsById.TryGetValue(response.QuestionId, out var question))
            {
                answeredItems.Add((response, question));
            }
        }

        return DiagnosticTopics
            .Select(topic =>
            {
                var topicItems = answeredItems
                    .Where(item => string.Equals(item.Question.Topic, topic, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var result = _topicDiagnosticStrategy.Evaluate(new TopicDiagnosticInput(
                    topic,
                    topicItems
                        .Select(item => new TopicDiagnosticAnswer(
                            item.Response.IsCorrect,
                            item.Question.Difficulty))
                        .ToList()));

                return new CompetencyDiagnosticResponse(
                    result.Topic,
                    result.Topic,
                    result.AnsweredQuestions,
                    result.CorrectAnswers,
                    result.AccuracyPercentage,
                    result.WeightedScorePercentage,
                    result.HighestDifficulty,
                    result.Level,
                    result.Confidence,
                    result.Pattern,
                    result.EvaluationSummary,
                    result.Recommendation);
            })
            .ToList();
    }
}
