using EcsEngine.Core.Scheduling;

namespace EcsEngine.Core;

/// <summary>
/// Contract for an ECS system. Systems implement <see cref="Execute"/> for their
/// logic and optionally override the static <see cref="Configure"/> to declare
/// component access and ordering constraints.
/// </summary>
public interface IEcsSystem
{
    /// <summary>
    /// Declares this system's component access and execution ordering constraints.
    /// Override in the implementing class; default is a no-op.
    /// </summary>
    static virtual void Configure(ISystemBuilder builder) { }

    /// <summary>
    /// Executes the system's logic for the current tick.
    /// Called by <see cref="SystemExecutor"/> via <see cref="SystemExecutor.Run"/>.
    /// </summary>
    void Execute(EcsWorld world);
}
