using EvaluaT.Domain.Common;
using EvaluaT.Domain.Questions;

namespace EvaluaT.Domain.Lessons;

public sealed class Lesson : Entity
{
    private Lesson()
    {
    }

    public string Topic { get; private set; } = string.Empty;
    public string? Competency { get; private set; }
    public LessonType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? ResourceUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Lesson Create(
        string topic,
        string? competency,
        LessonType type,
        string title,
        string content,
        string? resourceUrl)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        TopicCatalog.EnsureAllowed(topic);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Lesson title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Lesson content is required.", nameof(content));
        }

        return new Lesson
        {
            Id = Guid.NewGuid(),
            Topic = topic.Trim(),
            Competency = string.IsNullOrWhiteSpace(competency) ? null : competency.Trim(),
            Type = type,
            Title = title.Trim(),
            Content = content.Trim(),
            ResourceUrl = string.IsNullOrWhiteSpace(resourceUrl) ? null : resourceUrl.Trim(),
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
