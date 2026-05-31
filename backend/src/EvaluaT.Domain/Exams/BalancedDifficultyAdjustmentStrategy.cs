using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Exams;

public sealed class BalancedDifficultyAdjustmentStrategy : IDifficultyAdjustmentStrategy
{
    public DifficultyPolicy Policy => DifficultyPolicy.Balanced;
    public DifficultyLevel InitialDifficulty => DifficultyLevel.Medium;

    public DifficultyLevel GetNextDifficulty(DifficultyAdjustmentContext context)
    {
        if (context.LastAnswerWasCorrect)
        {
            return Increase(context.CurrentDifficulty);
        }

        return Decrease(context.CurrentDifficulty);
    }

    private static DifficultyLevel Increase(DifficultyLevel difficulty)
    {
        return difficulty == DifficultyLevel.Hard
            ? DifficultyLevel.Hard
            : (DifficultyLevel)((int)difficulty + 1);
    }

    private static DifficultyLevel Decrease(DifficultyLevel difficulty)
    {
        return difficulty == DifficultyLevel.Easy
            ? DifficultyLevel.Easy
            : (DifficultyLevel)((int)difficulty - 1);
    }
}
