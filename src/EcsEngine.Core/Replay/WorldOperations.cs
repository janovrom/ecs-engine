namespace EcsEngine.Core.Replay;

// All concrete IWorldOperation implementations live here.
// These are internal to EcsEngine.Core and exposed to EcsEngine.Replay via
// [InternalsVisibleTo]. The naming follows the public EcsWorld method they mirror.

internal sealed record CreateEntityOperation(EntityId EntityId) : IWorldOperation
{
    public void Apply(in ReplayContext ctx) => ctx.World.CreateEntityWithId(EntityId.Value);
}

internal sealed record MarkForDeletionOperation(EntityId EntityId) : IWorldOperation
{
    public void Apply(in ReplayContext ctx) => ctx.World.MarkForDeletion(EntityId);
}

internal sealed record AddComponentOperation<T>(EntityId EntityId, T Component) : IWorldOperation
    where T : struct, IEcsComponent
{
    public void Apply(in ReplayContext ctx) { T c = Component; ctx.World.QueueAddComponent(EntityId, in c); }
}

internal sealed record RemoveComponentOperation<T>(EntityId EntityId) : IWorldOperation
    where T : struct, IEcsComponent
{
    public void Apply(in ReplayContext ctx) => ctx.World.QueueRemoveComponent<T>(EntityId);
}

internal sealed record QueueEventOperation<T>(T Event) : IWorldOperation
    where T : struct, IEcsEvent
{
    public void Apply(in ReplayContext ctx) { T e = Event; ctx.World.QueueEvent(in e); }
}

internal sealed record AdvanceTickOperation : IWorldOperation
{
    public void Apply(in ReplayContext ctx) => ctx.World.AdvanceTick();
}

internal sealed record ApplySafePointOperation : IWorldOperation
{
    public void Apply(in ReplayContext ctx) => ctx.World.ApplySafePoint();
}

internal sealed record SetTickIntervalOperation(int IntervalMs) : IWorldOperation
{
    public void Apply(in ReplayContext ctx) => ctx.Scheduler?.SetIntervalDirect(IntervalMs);
}
