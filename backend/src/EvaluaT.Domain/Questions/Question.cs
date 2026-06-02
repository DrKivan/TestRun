using EvaluaT.Domain.Common;

namespace EvaluaT.Domain.Questions;

public sealed class Question : Entity
{
    private readonly List<AnswerOption> _options = new();

    private Question()
    {
    }

    public string Topic { get; private set; } = string.Empty;
    public string Competency { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public DifficultyLevel Difficulty { get; private set; } = DifficultyLevel.Easy;
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<AnswerOption> Options => _options.AsReadOnly();

    public static Question Create(
        string topic,
        string competency,
        string text,
        DifficultyLevel difficulty,
        IEnumerable<(string Text, bool IsCorrect)> options)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        TopicCatalog.EnsureAllowed(topic);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Question text is required.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(competency))
        {
            throw new ArgumentException("Competency is required.", nameof(competency));
        }

        var normalizedOptions = options
            .Select((option, index) => AnswerOption.Create(index, option.Text, option.IsCorrect))
            .ToList();

        EnsureValidOptions(normalizedOptions);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Topic = topic.Trim(),
            Competency = competency.Trim(),
            Text = text.Trim(),
            Difficulty = difficulty,
            IsActive = true
        };

        question._options.AddRange(normalizedOptions);
        return question;
    }

    public bool Evaluate(int selectedOptionOrder)
    {
        var selectedOption = _options.SingleOrDefault(option => option.Order == selectedOptionOrder);

        if (selectedOption is null)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedOptionOrder), "Selected option does not exist.");
        }

        return selectedOption.IsCorrect;
    }

    public void Update(
        string topic,
        string competency,
        string text,
        DifficultyLevel difficulty,
        IEnumerable<(string Text, bool IsCorrect)> options)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        TopicCatalog.EnsureAllowed(topic);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Question text is required.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(competency))
        {
            throw new ArgumentException("Competency is required.", nameof(competency));
        }

        var normalizedOptions = options
            .Select((option, index) => AnswerOption.Create(index, option.Text, option.IsCorrect))
            .ToList();

        EnsureValidOptions(normalizedOptions);

        Topic = topic.Trim();
        Competency = competency.Trim();
        Text = text.Trim();
        Difficulty = difficulty;

        _options.Clear();
        _options.AddRange(normalizedOptions);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void EnsureValidOptions(IReadOnlyCollection<AnswerOption> options)
    {
        if (options.Count < 2)
        {
            throw new ArgumentException("A question must have at least two options.");
        }

        if (options.Count(option => option.IsCorrect) != 1)
        {
            throw new ArgumentException("A question must have exactly one correct option.");
        }
    }
}
