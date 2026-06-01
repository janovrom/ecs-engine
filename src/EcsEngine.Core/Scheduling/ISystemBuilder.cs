namespace EcsEngine.Core.Scheduling;

/// <summary>
/// Fluent builder used in <see cref="IEcsSystem.Configure"/> to declare a
/// system's component access and execution ordering constraints.
/// </summary>
public interface ISystemBuilder
{
    /// <summary>Declares a read-only dependency on component <typeparamref name="T"/>.</summary>
    ISystemBuilder ReadComponent<T>() where T : struct, IEcsComponent;

    /// <summary>Declares a write dependency on component <typeparamref name="T"/>.</summary>
    ISystemBuilder WriteComponent<T>() where T : struct, IEcsComponent;

    /// <summary>Excludes entities that have component <typeparamref name="T"/> from this system's query.</summary>
    ISystemBuilder RejectComponent<T>() where T : struct, IEcsComponent;

    /// <summary>This system must execute after <typeparamref name="TSystem"/>.</summary>
    ISystemBuilder After<TSystem>() where TSystem : IEcsSystem;

    /// <summary>This system must execute before <typeparamref name="TSystem"/>.</summary>
    ISystemBuilder Before<TSystem>() where TSystem : IEcsSystem;

    /// <summary>This system must execute after the system of the given type.</summary>
    ISystemBuilder After(Type systemType);

    /// <summary>This system must execute before the system of the given type.</summary>
    ISystemBuilder Before(Type systemType);
}
