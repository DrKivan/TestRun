using EvaluaT.Domain.Common;

namespace EvaluaT.Domain.Questions;

public sealed class AnswerOption : Entity
{
    private AnswerOption()
    {
    }

    public Guid QuestionId { get; private set; }
    public int Order { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }

    internal static AnswerOption Create(int order, string text, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Option text is required.", nameof(text));
        }

        return new AnswerOption
        {
            Id = Guid.NewGuid(),
            Order = order,
            Text = text.Trim(),
            IsCorrect = isCorrect
        };
    }

    internal void Update(int order, string text, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Option text is required.", nameof(text));
        }

        Order = order;
        Text = text.Trim();
        IsCorrect = isCorrect;
    }
}
