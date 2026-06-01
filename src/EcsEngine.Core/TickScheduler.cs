namespace EcsEngine.Core;

/// <summary>
/// Manages the current adaptive tick interval and records interval changes to
/// an attached <see cref="OpLog"/> so that replays reproduce the same tick schedule.
/// </summary>
public sealed class TickScheduler
{
    private int _IntervalMs;
    private OpLog? _OpLog;

    /// <summary>The current tick interval in milliseconds.</summary>
    public int IntervalMs => _IntervalMs;

    public TickScheduler(int initialIntervalMs)
    {
        _IntervalMs = initialIntervalMs;
    }

    /// <summary>Starts recording interval changes to <paramref name="log"/>.</summary>
    public void AttachOpLog(OpLog log) => _OpLog = log;

    /// <summary>Stops recording interval changes.</summary>
    public void DetachOpLog() => _OpLog = null;

    /// <summary>
    /// Sets the tick interval to <paramref name="intervalMs"/> and records the
    /// change to the attached op-log if one is set.
    /// </summary>
    public void SetInterval(int intervalMs)
    {
        _IntervalMs = intervalMs;
        _OpLog?.Record(new Replay.SetTickIntervalOperation(intervalMs));
    }

    /// <summary>
    /// Sets the tick interval without recording to the op-log.
    /// Used by <c>WorldReplayer</c> during replay to avoid double-recording.
    /// </summary>
    internal void SetIntervalDirect(int intervalMs) => _IntervalMs = intervalMs;
}
