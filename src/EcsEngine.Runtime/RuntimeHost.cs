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

    internal RuntimeHost(
        StartupMode startupMode,
        RuntimeServiceProvider services,
        ITransport? transport,
        SystemExecutor executor)
    {
        StartupMode = startupMode;
        Services = services;
        Transport = transport;
        Executor = executor;
    }

    public void RunTick(EcsWorld world)
    {
        Executor.Run(world);
        world.ApplySafePoint();
        world.AdvanceTick();
    }
}
