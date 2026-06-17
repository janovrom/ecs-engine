namespace EcsEngine.Simulation;

public sealed class SimulationScript
{
    public IReadOnlyList<ScriptedOperationBase> Operations { get; init; } = [];

    public void Apply(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        foreach (ScriptedOperationBase operation in Operations)
            operation.Apply(state);
    }
}
