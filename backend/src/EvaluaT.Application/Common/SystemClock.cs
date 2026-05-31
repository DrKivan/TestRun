using EvaluaT.Application.Abstractions;

namespace EvaluaT.Application.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
