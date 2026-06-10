namespace EcsEngine.Runtime;

/// <summary>
/// Minimal dependency container for constructor-only system factories.
/// </summary>
public sealed class RuntimeServiceCollection
{
    private readonly Dictionary<Type, object> _Singletons = [];

    public RuntimeServiceCollection AddSingleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _Singletons[typeof(TService)] = instance;
        return this;
    }

    internal RuntimeServiceProvider BuildProvider() => new(_Singletons);
}
