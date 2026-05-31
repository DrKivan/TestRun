using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Exams;

public interface IDifficultyAdjustmentStrategy
{
    DifficultyPolicy Policy { get; }
    DifficultyLevel InitialDifficulty { get; }
    DifficultyLevel GetNextDifficulty(DifficultyAdjustmentContext context);
}
