using System.Diagnostics;
using System.Diagnostics.Metrics;
using EcsEngine.Core.Scheduling;

namespace EcsEngine.Runtime;

/// <summary>
/// Emits runtime execution metrics through System.Diagnostics.Metrics for OpenTelemetry pipelines.
/// </summary>
public sealed class MetricsSystemExecutionObserver : ISystemExecutionObserver
{
    private static readonly Meter Meter = new("EcsEngine.Runtime");
    private static readonly Counter<long> _TickBatchCount = Meter.CreateCounter<long>("ecs.tick.batch.count");
    private static readonly Histogram<long> _TickMaxBatchSize = Meter.CreateHistogram<long>("ecs.tick.batch.max_size");
    private static readonly Histogram<double> _TickBatchingEfficiency = Meter.CreateHistogram<double>("ecs.tick.batch.efficiency");
    private static readonly Counter<long> _TickCount = Meter.CreateCounter<long>("ecs.tick.count");
    private static readonly Counter<long> _TickAllocationBytes = Meter.CreateCounter<long>("ecs.tick.alloc.bytes");
    private static readonly Histogram<double> _TickDurationMilliseconds = Meter.CreateHistogram<double>("ecs.tick.duration.ms");
    private static readonly Counter<long> _SystemRunCount = Meter.CreateCounter<long>("ecs.system.run.count");
    private static readonly Counter<long> _SystemAllocationBytes = Meter.CreateCounter<long>("ecs.system.alloc.bytes");
    private static readonly Histogram<double> _SystemDurationMilliseconds = Meter.CreateHistogram<double>("ecs.system.duration.ms");

    private readonly int _PublishEveryTicks;
    private readonly RuntimeObservabilitySeverity _MinimumSeverity;

    public MetricsSystemExecutionObserver(RuntimeObservabilityConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _PublishEveryTicks = Math.Max(1, config.PublishEveryTicks);
        _MinimumSeverity = config.MinimumSeverity;
    }

    public bool IsEnabled => true;

    public bool ShouldSampleTick(int tick)
    {
        if (_PublishEveryTicks <= 1)
            return true;

        if (tick < 0)
            return false;

        return tick % _PublishEveryTicks == 0;
    }

    public void OnTickStarted(
        int tick,
        int systemCount,
        int batchCount,
        int maxBatchSize,
        double batchingEfficiency)
    {
        if (!ShouldEmit(RuntimeObservabilitySeverity.Information))
            return;

        _TickCount.Add(1);
        _TickBatchCount.Add(Math.Max(0L, batchCount));
        _TickMaxBatchSize.Record(Math.Max(0, maxBatchSize));
        _TickBatchingEfficiency.Record(Math.Clamp(batchingEfficiency, 0d, 1d));
    }

    public void OnSystemExecuted(int tick, Type systemType, long elapsedTicks, long allocatedBytes)
    {
        if (!ShouldEmit(RuntimeObservabilitySeverity.Debug))
            return;

        TagList tags = new()
        {
            { "system", systemType.FullName ?? systemType.Name },
        };

        _SystemRunCount.Add(1, tags);
        _SystemAllocationBytes.Add(Math.Max(0L, allocatedBytes), tags);
        _SystemDurationMilliseconds.Record(TicksToMilliseconds(elapsedTicks), tags);
    }

    public void OnTickCompleted(int tick, long elapsedTicks, long allocatedBytes)
    {
        if (!ShouldEmit(RuntimeObservabilitySeverity.Information))
            return;

        _TickAllocationBytes.Add(Math.Max(0L, allocatedBytes));
        _TickDurationMilliseconds.Record(TicksToMilliseconds(elapsedTicks));
    }

    private bool ShouldEmit(RuntimeObservabilitySeverity severity)
        => severity >= _MinimumSeverity;

    private static double TicksToMilliseconds(long elapsedTicks)
        => elapsedTicks * 1000d / Stopwatch.Frequency;
}
