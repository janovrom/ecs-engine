namespace EcsEngine.Core.Scheduling;

/// <summary>
/// Observes system execution metrics emitted by <see cref="SystemExecutor"/>.
/// Implementations should be low-overhead and deterministic-friendly.
/// </summary>
public interface ISystemExecutionObserver
{
    /// <summary>
    /// Whether this observer is active.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Returns true when the current tick should be sampled.
    /// </summary>
    bool ShouldSampleTick(int tick);

    /// <summary>
    /// Called before any system executes for the tick.
    /// </summary>
    void OnTickStarted(
        int tick,
        int systemCount,
        int batchCount,
        int maxBatchSize,
        double batchingEfficiency);

    /// <summary>
    /// Called after an individual system executes.
    /// </summary>
    void OnSystemExecuted(int tick, Type systemType, long elapsedTicks, long allocatedBytes);

    /// <summary>
    /// Called after all systems execute for the tick.
    /// </summary>
    void OnTickCompleted(int tick, long elapsedTicks, long allocatedBytes);
}
