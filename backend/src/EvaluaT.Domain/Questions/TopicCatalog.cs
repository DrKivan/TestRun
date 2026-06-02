namespace EvaluaT.Domain.Questions;

public static class TopicCatalog
{
    public static readonly IReadOnlySet<string> AllowedTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Matematica",
        "Programacion",
        "Ciencias"
    };

    public static void EnsureAllowed(string topic)
    {
        if (!AllowedTopics.Contains(topic.Trim()))
        {
            throw new ArgumentException("Topic is not allowed.", nameof(topic));
        }
    }
}
