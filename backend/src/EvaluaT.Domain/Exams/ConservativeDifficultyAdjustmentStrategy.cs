using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Exams;

public sealed class ConservativeDifficultyAdjustmentStrategy : IDifficultyAdjustmentStrategy
{
    public DifficultyPolicy Policy => DifficultyPolicy.Conservative;
    public DifficultyLevel InitialDifficulty => DifficultyLevel.Easy;

    public DifficultyLevel GetNextDifficulty(DifficultyAdjustmentContext context)
    {
        var recentResults = context.PreviousResults
            .Append(context.LastAnswerWasCorrect)
            .TakeLast(2)
            .ToList();

        if (recentResults.Count == 2 && recentResults.All(result => result))
        {
            return Increase(context.CurrentDifficulty);
        }

        if (!context.LastAnswerWasCorrect)
        {
            return Decrease(context.CurrentDifficulty);
        }

        return context.CurrentDifficulty;
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
