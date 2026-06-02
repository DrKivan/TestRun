using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Exams.Diagnostics;

public sealed record TopicDiagnosticAnswer(bool IsCorrect, DifficultyLevel Difficulty);

public sealed record TopicDiagnosticInput(
    string Topic,
    IReadOnlyCollection<TopicDiagnosticAnswer> Answers);

public sealed record TopicDiagnosticResult(
    string Topic,
    int AnsweredQuestions,
    int CorrectAnswers,
    decimal AccuracyPercentage,
    decimal WeightedScorePercentage,
    DifficultyLevel HighestDifficulty,
    string Level,
    string Confidence,
    string Pattern,
    string Recommendation,
    string EvaluationSummary);
