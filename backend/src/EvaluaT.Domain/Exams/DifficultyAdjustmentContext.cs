using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Exams;

public sealed record DifficultyAdjustmentContext(
    DifficultyLevel CurrentDifficulty,
    bool LastAnswerWasCorrect,
    IReadOnlyCollection<bool> PreviousResults);
