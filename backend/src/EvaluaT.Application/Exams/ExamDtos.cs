using EvaluaT.Application.Questions;
using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Exams;

public sealed record StartExamRequest(
    Guid StudentId,
    int MaxQuestions = 5,
    DifficultyPolicy Policy = DifficultyPolicy.Balanced);

public sealed record AnswerQuestionRequest(Guid QuestionId, int SelectedOptionOrder);

public sealed record ExamResponseItem(
    Guid QuestionId,
    int SelectedOptionOrder,
    bool IsCorrect,
    DifficultyLevel DifficultyAtAnswer,
    DateTime AnsweredAt);

public sealed record ExamSessionResponse(
    Guid Id,
    Guid StudentId,
    DifficultyPolicy Policy,
    DifficultyLevel CurrentDifficulty,
    SessionStatus Status,
    int MaxQuestions,
    int AnsweredQuestions,
    decimal ScorePercentage,
    DateTime StartedAt,
    DateTime? CompletedAt,
    CurrentQuestionResponse? CurrentQuestion,
    IReadOnlyList<ExamResponseItem> Responses);

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
    SessionStatus Status,
    int AnsweredQuestions,
    int MaxQuestions,
    decimal ScorePercentage,
    DateTime StartedAt,
    DateTime? CompletedAt);
