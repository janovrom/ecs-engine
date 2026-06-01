namespace EcsEngine.Core.Scheduling;

/// <summary>
/// Holds the ordered sequence of systems produced by <see cref="SystemScheduler.Build"/>
/// and executes them sequentially via <see cref="Run"/>.
/// </summary>
public sealed class SystemExecutor
{
    private readonly IEcsSystem[] _Systems;

    /// <summary>The systems in their validated execution order.</summary>
    public IReadOnlyList<IEcsSystem> Systems => _Systems;

    internal SystemExecutor(IEcsSystem[] systems)
    {
        _Systems = systems;
    }

    /// <summary>
    /// Executes all registered systems in topological order for the current tick.
    /// This is the single execution entry point (D-033, D-051).
    /// </summary>
    public void Run(EcsWorld world)
    {
        foreach (IEcsSystem system in _Systems)
            system.Execute(world);
    }
}
