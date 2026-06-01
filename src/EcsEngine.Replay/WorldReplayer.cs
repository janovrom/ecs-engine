using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Replays an <see cref="OpLog"/> on a fresh <see cref="EcsWorld"/>, producing an
/// identical world state to the one that was recorded (D-055).
/// </summary>
public sealed class WorldReplayer
{
    private readonly OpLog _Log;

    public WorldReplayer(OpLog log)
    {
        _Log = log;
    }

    /// <summary>
    /// Creates a fresh <see cref="EcsWorld"/>, replays all recorded operations in order,
    /// and returns the resulting world. If <paramref name="scheduler"/> is provided,
    /// <c>SetTickIntervalOperation</c> entries are also applied to it via
    /// <c>SetIntervalDirect</c> (without re-recording).
    /// </summary>
    public EcsWorld Run(TickScheduler? scheduler = null)
    {
        EcsWorld world = new();
        foreach (IWorldOperation op in _Log.Operations)
        {
            op.Apply(world);
            op.ApplyToScheduler(scheduler);
        }
        return world;
    }
}
