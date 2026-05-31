namespace EvaluaT.Domain.Exams;

public sealed class DifficultyAdjustmentStrategyFactory
{
    private readonly IReadOnlyDictionary<DifficultyPolicy, IDifficultyAdjustmentStrategy> _strategies;

    public DifficultyAdjustmentStrategyFactory(IEnumerable<IDifficultyAdjustmentStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(strategy => strategy.Policy);
    }

    public IDifficultyAdjustmentStrategy Create(DifficultyPolicy policy)
    {
        if (_strategies.TryGetValue(policy, out var strategy))
        {
            return strategy;
        }

        throw new NotSupportedException($"Difficulty policy '{policy}' is not supported.");
    }
}
