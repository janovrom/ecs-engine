using EcsEngine.Core;
using EcsEngine.Core.Scheduling;
using NUnit.Framework;

namespace EcsEngine.Core.Tests;

[TestFixture]
public class SystemExecutionBatchingMetricsTests
{
    [Test]
    public void Run_WithIndependentSystems_ReportsSingleBatchAndFullEfficiency()
    {
        EcsWorld world = new();
        CapturingObserver observer = new();

        SystemExecutor executor = new SystemScheduler()
            .Register(new IndepA())
            .Register(new IndepB())
            .Register(new IndepC())
            .Build();

        executor.Run(world, observer);

        Assert.That(observer.SystemCount, Is.EqualTo(3));
        Assert.That(observer.BatchCount, Is.EqualTo(1));
        Assert.That(observer.MaxBatchSize, Is.EqualTo(3));
        Assert.That(observer.BatchingEfficiency, Is.EqualTo(1d).Within(0.0001));
    }

    [Test]
    public void Run_WithLinearDependencyChain_ReportsThreeBatchesAndZeroEfficiency()
    {
        EcsWorld world = new();
        CapturingObserver observer = new();

        SystemExecutor executor = new SystemScheduler()
            .Register(new ChainC())
            .Register(new ChainB())
            .Register(new ChainA())
            .Build();

        executor.Run(world, observer);

        Assert.That(observer.SystemCount, Is.EqualTo(3));
        Assert.That(observer.BatchCount, Is.EqualTo(3));
        Assert.That(observer.MaxBatchSize, Is.EqualTo(1));
        Assert.That(observer.BatchingEfficiency, Is.EqualTo(0d).Within(0.0001));
    }

    [EcsSystem]
    private sealed class IndepA : IEcsSystem
    {
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class IndepB : IEcsSystem
    {
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class IndepC : IEcsSystem
    {
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class ChainA : IEcsSystem
    {
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class ChainB : IEcsSystem
    {
        public static void Configure(ISystemBuilder builder) => builder.After<ChainA>();
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class ChainC : IEcsSystem
    {
        public static void Configure(ISystemBuilder builder) => builder.After<ChainB>();
        public void Execute(EcsWorld world) { }
    }

    private sealed class CapturingObserver : ISystemExecutionObserver
    {
        public bool IsEnabled => true;
        public int SystemCount { get; private set; }
        public int BatchCount { get; private set; }
        public int MaxBatchSize { get; private set; }
        public double BatchingEfficiency { get; private set; }

        public bool ShouldSampleTick(int tick) => true;

        public void OnTickStarted(
            int tick,
            int systemCount,
            int batchCount,
            int maxBatchSize,
            double batchingEfficiency)
        {
            SystemCount = systemCount;
            BatchCount = batchCount;
            MaxBatchSize = maxBatchSize;
            BatchingEfficiency = batchingEfficiency;
        }

        public void OnSystemExecuted(int tick, Type systemType, long elapsedTicks, long allocatedBytes) { }

        public void OnTickCompleted(int tick, long elapsedTicks, long allocatedBytes) { }
    }
}
