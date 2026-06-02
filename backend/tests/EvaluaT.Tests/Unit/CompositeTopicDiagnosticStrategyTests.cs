using EvaluaT.Application.Exams.Diagnostics;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Tests.Unit;

public sealed class CompositeTopicDiagnosticStrategyTests
{
    [Fact]
    public void Evaluate_WeightsDifficultyAndExplainsFactors()
    {
        var strategy = new CompositeTopicDiagnosticStrategy();

        var result = strategy.Evaluate(new TopicDiagnosticInput(
            "Matematica",
            [
                new TopicDiagnosticAnswer(true, DifficultyLevel.Easy),
                new TopicDiagnosticAnswer(false, DifficultyLevel.Hard)
            ]));

        Assert.Equal(50, result.AccuracyPercentage);
        Assert.Equal(25, result.WeightedScorePercentage);
        Assert.Equal("Falla en aplicacion", result.Pattern);
        Assert.Contains("puntaje ponderado", result.EvaluationSummary);
    }
}
