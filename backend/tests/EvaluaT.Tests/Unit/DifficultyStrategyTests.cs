using EvaluaT.Domain.Exams;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Tests.Unit;

public sealed class DifficultyStrategyTests
{
    [Fact]
    public void BalancedPolicy_IncreasesDifficulty_WhenAnswerIsCorrect()
    {
        var strategy = new BalancedDifficultyAdjustmentStrategy();

        var nextDifficulty = strategy.GetNextDifficulty(new DifficultyAdjustmentContext(
            DifficultyLevel.Medium,
            LastAnswerWasCorrect: true,
            PreviousResults: []));

        Assert.Equal(DifficultyLevel.Hard, nextDifficulty);
    }

    [Fact]
    public void BalancedPolicy_DecreasesDifficulty_WhenAnswerIsIncorrect()
    {
        var strategy = new BalancedDifficultyAdjustmentStrategy();

        var nextDifficulty = strategy.GetNextDifficulty(new DifficultyAdjustmentContext(
            DifficultyLevel.Medium,
            LastAnswerWasCorrect: false,
            PreviousResults: []));

        Assert.Equal(DifficultyLevel.Easy, nextDifficulty);
    }

    [Fact]
    public void ConservativePolicy_RequiresTwoCorrectAnswers_ToIncreaseDifficulty()
    {
        var strategy = new ConservativeDifficultyAdjustmentStrategy();

        var nextDifficulty = strategy.GetNextDifficulty(new DifficultyAdjustmentContext(
            DifficultyLevel.Easy,
            LastAnswerWasCorrect: true,
            PreviousResults: [true]));

        Assert.Equal(DifficultyLevel.Medium, nextDifficulty);
    }
}
