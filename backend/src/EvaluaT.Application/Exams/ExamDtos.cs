using EvaluaT.Application.Questions;
using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Exams;

public sealed record StartExamRequest(
    Guid StudentId,
    int MaxQuestions = 10,
    DifficultyPolicy Policy = DifficultyPolicy.Balanced,
    ExamSessionKind Kind = ExamSessionKind.Standard,
    string? TargetTopic = null,
    string? TargetCompetency = null);

public sealed record AnswerQuestionRequest(Guid QuestionId, int SelectedOptionOrder);

public sealed record ExamResponseItem(
    Guid QuestionId,
    int SelectedOptionOrder,
    bool IsCorrect,
    DifficultyLevel DifficultyAtAnswer,
    DateTime AnsweredAt);

public sealed record ErrorReviewItemResponse(
    Guid QuestionId,
    string Topic,
    string QuestionText,
    int SelectedOptionOrder,
    string SelectedOptionText,
    int CorrectOptionOrder,
    string CorrectOptionText,
    string Explanation);

public sealed record CompetencyDiagnosticResponse(
    string Topic,
    string Competency,
    int AnsweredQuestions,
    int CorrectAnswers,
    decimal ScorePercentage,
    decimal WeightedScorePercentage,
    DifficultyLevel HighestDifficulty,
    string Level,
    string Confidence,
    string Pattern,
    string EvaluationSummary,
    string Recommendation);

public sealed record ExamSessionResponse(
    Guid Id,
    Guid StudentId,
    DifficultyPolicy Policy,
    ExamSessionKind Kind,
    string? TargetTopic,
    string? TargetCompetency,
    DifficultyLevel CurrentDifficulty,
    SessionStatus Status,
    int MaxQuestions,
    int AnsweredQuestions,
    int CorrectAnswers,
    decimal ScorePercentage,
    DateTime StartedAt,
    DateTime? CompletedAt,
    CurrentQuestionResponse? CurrentQuestion,
    IReadOnlyList<ExamResponseItem> Responses,
    IReadOnlyList<CompetencyDiagnosticResponse> Diagnostic,
    IReadOnlyList<ErrorReviewItemResponse> ErrorReview);

public sealed record AnswerResultResponse(
    bool IsCorrect,
    DifficultyLevel PreviousDifficulty,
    DifficultyLevel NextDifficulty,
    SessionStatus Status,
    decimal ScorePercentage,
    CurrentQuestionResponse? NextQuestion,
    ExamSessionResponse Session);

public sealed record ExamResultSummaryResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    DifficultyPolicy Policy,
    ExamSessionKind Kind,
    string? TargetTopic,
    string? TargetCompetency,
    SessionStatus Status,
    int AnsweredQuestions,
    int MaxQuestions,
    int CorrectAnswers,
    decimal ScorePercentage,
    DateTime StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<CompetencyDiagnosticResponse> Diagnostic);

public sealed record TopicAnalyticsResponse(
    string Topic,
    int AnswerCount,
    int IncorrectCount,
    decimal ErrorPercentage);

public sealed record QuestionAnalyticsResponse(
    Guid QuestionId,
    string Topic,
    string Competency,
    string Text,
    DifficultyLevel Difficulty,
    int AnswerCount,
    int IncorrectCount,
    decimal ErrorPercentage);

public sealed record ExamAnalyticsResponse(
    IReadOnlyList<TopicAnalyticsResponse> Topics,
    IReadOnlyList<QuestionAnalyticsResponse> Questions);
