namespace EcsEngine.Core.Replay;

// All concrete IWorldOperation implementations live here.
// These are internal to EcsEngine.Core and exposed to EcsEngine.Replay via
// [InternalsVisibleTo]. The naming follows the public EcsWorld method they mirror.

internal sealed record CreateEntityOperation(EntityId EntityId) : IWorldOperation
{
    public void Apply(EcsWorld world) => world.CreateEntityWithId(EntityId.Value);
}

internal sealed record MarkForDeletionOperation(EntityId EntityId) : IWorldOperation
{
    public void Apply(EcsWorld world) => world.MarkForDeletion(EntityId);
}

internal sealed record AddComponentOperation<T>(EntityId EntityId, T Component) : IWorldOperation
    where T : struct, IEcsComponent
{
    public void Apply(EcsWorld world) { T c = Component; world.QueueAddComponent(EntityId, in c); }
}

internal sealed record RemoveComponentOperation<T>(EntityId EntityId) : IWorldOperation
    where T : struct, IEcsComponent
{
    public void Apply(EcsWorld world) => world.QueueRemoveComponent<T>(EntityId);
}

internal sealed record QueueEventOperation<T>(T Event) : IWorldOperation
    where T : struct, IEcsEvent
{
    public void Apply(EcsWorld world) { T e = Event; world.QueueEvent(in e); }
}

internal sealed record AdvanceTickOperation : IWorldOperation
{
    public void Apply(EcsWorld world) => world.AdvanceTick();
}

internal sealed record ApplySafePointOperation : IWorldOperation
{
    public void Apply(EcsWorld world) => world.ApplySafePoint();
}

internal sealed record SetTickIntervalOperation(int IntervalMs) : IWorldOperation
{
    /// <summary>No effect on the world itself; tick interval is a host concern.</summary>
    public void Apply(EcsWorld world) { }

    public void ApplyToScheduler(TickScheduler? scheduler) => scheduler?.SetIntervalDirect(IntervalMs);
}
