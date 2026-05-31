using EvaluaT.Domain.Questions;

namespace EvaluaT.Tests.Unit;

public sealed class QuestionTests
{
    [Fact]
    public void Create_Throws_WhenQuestionDoesNotHaveExactlyOneCorrectOption()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Question.Create(
                "Programacion",
                "Que es DRY?",
                DifficultyLevel.Easy,
                [("Duplicar codigo", false), ("Reutilizar conocimiento", false)]));

        Assert.Contains("exactly one correct option", exception.Message);
    }

    [Fact]
    public void Evaluate_ReturnsTrue_WhenSelectedOptionIsCorrect()
    {
        var question = Question.Create(
            "Matematica",
            "Cuanto es 2 + 2?",
            DifficultyLevel.Easy,
            [("3", false), ("4", true), ("5", false)]);

        Assert.True(question.Evaluate(1));
    }
}
