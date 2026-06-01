namespace EcsEngine.Core.Scheduling;

internal sealed class SystemBuilder : ISystemBuilder
{
    public HashSet<Type> ReadComponents { get; } = [];
    public HashSet<Type> WriteComponents { get; } = [];
    public HashSet<Type> RejectComponents { get; } = [];

    /// <summary>This system must run after all types in this set.</summary>
    public HashSet<Type> AfterTypes { get; } = [];

    /// <summary>This system must run before all types in this set.</summary>
    public HashSet<Type> BeforeTypes { get; } = [];

    public ISystemBuilder ReadComponent<T>() where T : struct, IEcsComponent
    {
        ReadComponents.Add(typeof(T));
        return this;
    }

    public ISystemBuilder WriteComponent<T>() where T : struct, IEcsComponent
    {
        WriteComponents.Add(typeof(T));
        return this;
    }

    public ISystemBuilder RejectComponent<T>() where T : struct, IEcsComponent
    {
        RejectComponents.Add(typeof(T));
        return this;
    }

    public ISystemBuilder After<TSystem>() where TSystem : IEcsSystem
        => After(typeof(TSystem));

    public ISystemBuilder Before<TSystem>() where TSystem : IEcsSystem
        => Before(typeof(TSystem));

    public ISystemBuilder After(Type systemType)
    {
        AfterTypes.Add(systemType);
        return this;
    }

    public ISystemBuilder Before(Type systemType)
    {
        BeforeTypes.Add(systemType);
        return this;
    }
}
