using EcsEngine.Core;
using EcsEngine.Core.Scheduling;
using EcsEngine.Transport;

namespace EcsEngine.Runtime;

/// <summary>
/// Runtime host builder with an explicit system registration window.
/// </summary>
public sealed class RuntimeHostBuilder
{
    private readonly RuntimeServiceCollection _Services = new();
    private readonly List<SystemRegistration> _Registrations = [];
    private ISystemExecutionObserver? _ExecutionObserver;
    private bool _registrationClosed;

    public StartupMode StartupMode { get; private set; } = StartupMode.Local;
    public ITransport? Transport { get; private set; }

    public RuntimeHostBuilder UseStartupMode(StartupMode startupMode)
    {
        StartupMode = startupMode;
        return this;
    }

    public RuntimeHostBuilder UseConfiguration(RuntimeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        StartupMode = config.StartupMode;

        if (config.Observability.Enabled && _ExecutionObserver is null)
            _ExecutionObserver = new MetricsSystemExecutionObserver(config.Observability);

        return this;
    }

    public RuntimeHostBuilder UseSystemExecutionObserver(ISystemExecutionObserver observer)
    {
        _ExecutionObserver = observer ?? throw new ArgumentNullException(nameof(observer));
        return this;
    }

    public RuntimeHostBuilder UseTransport(ITransport transport)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        return this;
    }

    public RuntimeHostBuilder AddSingleton<TService>(TService instance)
        where TService : class
    {
        _Services.AddSingleton(instance);
        return this;
    }

    public RuntimeHostBuilder RegisterSystem<TSystem>(Func<RuntimeServiceProvider, TSystem> factory)
        where TSystem : class, IEcsSystem
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureRegistrationOpen();

        Type type = typeof(TSystem);
        if (_Registrations.Any(r => r.SystemType == type))
        {
            throw new InvalidOperationException(
                $"System of type '{type.FullName}' is already registered.");
        }

        _Registrations.Add(new SystemRegistration(type, sp => factory(sp)));
        return this;
    }

    public RuntimeHost Build()
    {
        EnsureRegistrationOpen();
        _registrationClosed = true;

        if (StartupMode == StartupMode.Server && Transport is null)
            throw new InvalidOperationException("Server startup mode requires a configured transport.");

        RuntimeServiceProvider serviceProvider = _Services.BuildProvider();
        SystemScheduler scheduler = new();

        // Deterministic ordering of runtime registration independent of call order.
        foreach (SystemRegistration registration in _Registrations.OrderBy(r => r.SystemType.FullName, StringComparer.Ordinal))
        {
            IEcsSystem system = registration.Factory(serviceProvider);
            scheduler.Register(system);
        }

        return new RuntimeHost(StartupMode, serviceProvider, Transport, scheduler.Build(), _ExecutionObserver);
    }

    private void EnsureRegistrationOpen()
    {
        if (_registrationClosed)
        {
            throw new InvalidOperationException(
                "System registration window is closed after Build().");
        }
    }

    private readonly record struct SystemRegistration(
        Type SystemType,
        Func<RuntimeServiceProvider, IEcsSystem> Factory);
}
