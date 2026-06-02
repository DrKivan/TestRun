namespace EvaluaT.Application.Exams.Diagnostics;

public interface ITopicDiagnosticStrategy
{
    TopicDiagnosticResult Evaluate(TopicDiagnosticInput input);
}
