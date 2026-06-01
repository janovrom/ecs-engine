namespace EcsEngine.Core;

/// <summary>
/// An ordered, in-memory record of all world mutations for a seeded simulation run.
/// Attach to an <see cref="EcsWorld"/> via <c>world.AttachOpLog(log)</c> to begin
/// recording. Pass to <c>EcsEngine.Replay.WorldReplayer</c> to replay.
/// </summary>
public sealed class OpLog
{
    private readonly List<IWorldOperation> _Operations = [];

    /// <summary>The random seed associated with this recorded run.</summary>
    public uint Seed { get; }

    /// <summary>The ordered sequence of recorded operations.</summary>
    public IReadOnlyList<IWorldOperation> Operations => _Operations;

    public OpLog(uint seed)
    {
        Seed = seed;
    }

    internal void Record(IWorldOperation operation) => _Operations.Add(operation);
}
