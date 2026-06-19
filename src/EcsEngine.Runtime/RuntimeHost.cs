using EcsEngine.Core;
using EcsEngine.Core.Scheduling;
using EcsEngine.Transport;

namespace EcsEngine.Runtime;

public sealed class RuntimeHost
{
    public StartupMode StartupMode { get; }
    public RuntimeServiceProvider Services { get; }
    public ITransport? Transport { get; }
    public SystemExecutor Executor { get; }
    public ISystemExecutionObserver? ExecutionObserver { get; }

    internal RuntimeHost(
        StartupMode startupMode,
        RuntimeServiceProvider services,
        ITransport? transport,
        SystemExecutor executor,
        ISystemExecutionObserver? executionObserver)
    {
        StartupMode = startupMode;
        Services = services;
        Transport = transport;
        Executor = executor;
        ExecutionObserver = executionObserver;
    }

    public void RunTick(EcsWorld world)
    {
        Executor.Run(world, ExecutionObserver);
        world.ApplySafePoint();
        world.AdvanceTick();
    }
}
