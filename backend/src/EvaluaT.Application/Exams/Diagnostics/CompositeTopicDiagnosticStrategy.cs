using EvaluaT.Domain.Questions;

namespace EvaluaT.Application.Exams.Diagnostics;

public sealed class CompositeTopicDiagnosticStrategy : ITopicDiagnosticStrategy
{
    public TopicDiagnosticResult Evaluate(TopicDiagnosticInput input)
    {
        var answers = input.Answers.ToList();
        var answered = answers.Count;
        var correct = answers.Count(answer => answer.IsCorrect);
        var accuracy = CalculatePercentage(correct, answered);
        var weightedScore = CalculateWeightedScore(answers);
        var highestDifficulty = GetHighestDifficulty(answers);
        var confidence = GetConfidence(answered);
        var pattern = GetPattern(answers, accuracy, weightedScore);
        var level = GetLevel(answered, weightedScore, highestDifficulty, confidence);

        return new TopicDiagnosticResult(
            input.Topic,
            answered,
            correct,
            accuracy,
            weightedScore,
            highestDifficulty,
            level,
            confidence,
            pattern,
            GetRecommendation(input.Topic, answered, weightedScore, pattern),
            GetEvaluationSummary(input.Topic, accuracy, weightedScore, confidence, pattern));
    }

    private static decimal CalculateWeightedScore(IReadOnlyCollection<TopicDiagnosticAnswer> answers)
    {
        var totalWeight = answers.Sum(answer => Weight(answer.Difficulty));
        if (totalWeight == 0)
        {
            return 0;
        }

        var earnedWeight = answers
            .Where(answer => answer.IsCorrect)
            .Sum(answer => Weight(answer.Difficulty));

        return Math.Round(earnedWeight * 100m / totalWeight, 0);
    }

    private static decimal CalculatePercentage(int value, int total)
    {
        return total == 0 ? 0 : Math.Round(value * 100m / total, 0);
    }

    private static int Weight(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 1,
            DifficultyLevel.Medium => 2,
            DifficultyLevel.Hard => 3,
            _ => 1
        };
    }

    private static DifficultyLevel GetHighestDifficulty(IReadOnlyCollection<TopicDiagnosticAnswer> answers)
    {
        if (answers.Any(answer => answer.Difficulty == DifficultyLevel.Hard))
        {
            return DifficultyLevel.Hard;
        }

        return answers.Any(answer => answer.Difficulty == DifficultyLevel.Medium)
            ? DifficultyLevel.Medium
            : DifficultyLevel.Easy;
    }

    private static string GetConfidence(int answered)
    {
        return answered switch
        {
            0 => "Sin evidencia",
            <= 2 => "Baja",
            <= 4 => "Media",
            _ => "Alta"
        };
    }

    private static string GetPattern(
        IReadOnlyCollection<TopicDiagnosticAnswer> answers,
        decimal accuracy,
        decimal weightedScore)
    {
        if (answers.Count == 0)
        {
            return "Sin evidencia";
        }

        if (answers.Any(answer => answer.Difficulty == DifficultyLevel.Easy && !answer.IsCorrect))
        {
            return "Dificultad en bases";
        }

        if (accuracy >= 80 && weightedScore >= 80)
        {
            return "Dominio consistente";
        }

        if (answers.Any(answer => answer.Difficulty != DifficultyLevel.Easy && !answer.IsCorrect))
        {
            return "Falla en aplicacion";
        }

        return "Dominio parcial";
    }

    private static string GetLevel(
        int answered,
        decimal weightedScore,
        DifficultyLevel highestDifficulty,
        string confidence)
    {
        if (answered == 0)
        {
            return "Sin evaluar";
        }

        if (weightedScore >= 85 && highestDifficulty >= DifficultyLevel.Medium && confidence != "Baja")
        {
            return "Dominado";
        }

        if (weightedScore >= 70)
        {
            return "Competente";
        }

        if (weightedScore >= 55)
        {
            return "En desarrollo";
        }

        return "Reforzar";
    }

    private static string GetRecommendation(string topic, int answered, decimal weightedScore, string pattern)
    {
        if (answered == 0)
        {
            return $"Aun no hay respuestas suficientes en {topic} para diagnosticar fortalezas o debilidades.";
        }

        if (pattern == "Dificultad en bases")
        {
            return $"Reforzar fundamentos de {topic} antes de subir la dificultad.";
        }

        if (weightedScore >= 85)
        {
            return $"Mantener {topic} con problemas de mayor reto.";
        }

        if (pattern == "Falla en aplicacion")
        {
            return $"Practicar {topic} con ejercicios guiados de aplicacion.";
        }

        return $"Continuar practicando {topic} para consolidar el dominio.";
    }

    private static string GetEvaluationSummary(
        string topic,
        decimal accuracy,
        decimal weightedScore,
        string confidence,
        string pattern)
    {
        return $"En {topic}, tu precision fue {accuracy}% y tu puntaje ponderado por dificultad fue {weightedScore}%. La confianza del diagnostico es {confidence.ToLowerInvariant()} y el patron detectado es: {pattern.ToLowerInvariant()}.";
    }
}
