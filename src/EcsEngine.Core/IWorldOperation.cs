namespace EcsEngine.Core;

/// <summary>
/// Represents a single recorded mutation against an <see cref="EcsWorld"/>.
/// Implementations are value-typed record structs or sealed records defined in
/// <c>EcsEngine.Core.Replay</c> and recorded by the world when an <see cref="OpLog"/>
/// is attached.
/// </summary>
public interface IWorldOperation
{
    /// <summary>Applies this operation to the given world.</summary>
    void Apply(EcsWorld world);

    /// <summary>
    /// Applies scheduler-level effects of this operation.
    /// Default implementation is a no-op; only <c>SetTickIntervalOperation</c> overrides this.
    /// </summary>
    void ApplyToScheduler(TickScheduler? scheduler) { }
}
