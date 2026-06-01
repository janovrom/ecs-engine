namespace EcsEngine.Core;

/// <summary>
/// Bundles the replay targets passed to <see cref="IWorldOperation.Apply"/>.
/// </summary>
/// <param name="World">The world being replayed into. Never null.</param>
/// <param name="Scheduler">Optional scheduler; null when replaying without tick-interval tracking.</param>
public readonly record struct ReplayContext(EcsWorld World, TickScheduler? Scheduler);

/// <summary>
/// Represents a single recorded mutation against an <see cref="EcsWorld"/>.
/// Implementations are value-typed record structs or sealed records defined in
/// <c>EcsEngine.Core.Replay</c> and recorded by the world when an <see cref="OpLog"/>
/// is attached.
/// </summary>
public interface IWorldOperation
{
    /// <summary>Applies this operation to the given replay context.</summary>
    void Apply(in ReplayContext ctx);
}
