using BenchmarkDotNet.Attributes;
using EcsEngine.Core;
using EcsEngine.Core.Scheduling;
using EcsEngine.Runtime;

namespace EcsEngine.Benchmarks;

[MemoryDiagnoser]
public class SystemExecutionBenchmarks
{
    [Params(2, 8, 32)]
    public int SystemCount { get; set; }

    private EcsWorld _World = null!;
    private EcsWorld _UnsampledWorld = null!;
    private SystemExecutor _Executor = null!;
    private ISystemExecutionObserver _ObserverAllTicks = null!;
    private ISystemExecutionObserver _ObserverUnsampledTick = null!;

    [GlobalSetup]
    public void Setup()
    {
        _World = new EcsWorld();
        _UnsampledWorld = new EcsWorld();

        // Tick 1 with PublishEveryTicks=8 means this world is intentionally unsampled.
        _UnsampledWorld.AdvanceTick();

        IEcsSystem[] availableSystems =
        [
            new NoOpSystem<M01>(), new NoOpSystem<M02>(), new NoOpSystem<M03>(), new NoOpSystem<M04>(),
            new NoOpSystem<M05>(), new NoOpSystem<M06>(), new NoOpSystem<M07>(), new NoOpSystem<M08>(),
            new NoOpSystem<M09>(), new NoOpSystem<M10>(), new NoOpSystem<M11>(), new NoOpSystem<M12>(),
            new NoOpSystem<M13>(), new NoOpSystem<M14>(), new NoOpSystem<M15>(), new NoOpSystem<M16>(),
            new NoOpSystem<M17>(), new NoOpSystem<M18>(), new NoOpSystem<M19>(), new NoOpSystem<M20>(),
            new NoOpSystem<M21>(), new NoOpSystem<M22>(), new NoOpSystem<M23>(), new NoOpSystem<M24>(),
            new NoOpSystem<M25>(), new NoOpSystem<M26>(), new NoOpSystem<M27>(), new NoOpSystem<M28>(),
            new NoOpSystem<M29>(), new NoOpSystem<M30>(), new NoOpSystem<M31>(), new NoOpSystem<M32>(),
        ];

        SystemScheduler scheduler = new();
        for (int i = 0; i < SystemCount; i++)
            scheduler.Register(availableSystems[i]);

        _Executor = scheduler.Build();

        _ObserverAllTicks = new MetricsSystemExecutionObserver(new RuntimeObservabilityConfig
        {
            Enabled = true,
            PublishEveryTicks = 1,
            MinimumSeverity = RuntimeObservabilitySeverity.Information,
        });

        _ObserverUnsampledTick = new MetricsSystemExecutionObserver(new RuntimeObservabilityConfig
        {
            Enabled = true,
            PublishEveryTicks = 8,
            MinimumSeverity = RuntimeObservabilitySeverity.Information,
        });
    }

    [Benchmark(Baseline = true)]
    public void Execute_NoObserver()
    {
        _Executor.Run(_World);
    }

    [Benchmark]
    public void Execute_WithMetricsObserver_AllTicksSampled()
    {
        _Executor.Run(_World, _ObserverAllTicks);
    }

    [Benchmark]
    public void Execute_WithMetricsObserver_UnsampledTick()
    {
        _Executor.Run(_UnsampledWorld, _ObserverUnsampledTick);
    }

    [EcsSystem]
    private sealed class NoOpSystem<TMarker> : IEcsSystem
    {
        public void Execute(EcsWorld world)
        {
        }
    }

    private readonly record struct M01;
    private readonly record struct M02;
    private readonly record struct M03;
    private readonly record struct M04;
    private readonly record struct M05;
    private readonly record struct M06;
    private readonly record struct M07;
    private readonly record struct M08;
    private readonly record struct M09;
    private readonly record struct M10;
    private readonly record struct M11;
    private readonly record struct M12;
    private readonly record struct M13;
    private readonly record struct M14;
    private readonly record struct M15;
    private readonly record struct M16;
    private readonly record struct M17;
    private readonly record struct M18;
    private readonly record struct M19;
    private readonly record struct M20;
    private readonly record struct M21;
    private readonly record struct M22;
    private readonly record struct M23;
    private readonly record struct M24;
    private readonly record struct M25;
    private readonly record struct M26;
    private readonly record struct M27;
    private readonly record struct M28;
    private readonly record struct M29;
    private readonly record struct M30;
    private readonly record struct M31;
    private readonly record struct M32;
}
