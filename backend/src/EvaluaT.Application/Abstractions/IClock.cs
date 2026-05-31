namespace EvaluaT.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
