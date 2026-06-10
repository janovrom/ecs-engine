namespace EcsEngine.Runtime;

public sealed class RuntimeServiceProvider
{
    private readonly IReadOnlyDictionary<Type, object> _Singletons;

    internal RuntimeServiceProvider(IReadOnlyDictionary<Type, object> singletons)
    {
        _Singletons = singletons;
    }

    public TService GetRequired<TService>()
        where TService : class
    {
        if (_Singletons.TryGetValue(typeof(TService), out object? value))
            return (TService)value;

        throw new InvalidOperationException(
            $"No runtime service registered for type '{typeof(TService).FullName}'.");
    }
}
