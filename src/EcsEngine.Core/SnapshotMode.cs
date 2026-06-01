namespace EcsEngine.Core;

/// <summary>
/// Controls the semantics of a world snapshot.
/// </summary>
public enum SnapshotMode : byte
{
    /// <summary>
    /// Snapshot is taken at a safe tick boundary (after <see cref="EcsWorld.ApplySafePoint"/>).
    /// No pending mutations are allowed at write time.
    /// </summary>
    TickBoundary = 0,

    /// <summary>
    /// Snapshot is taken immediately at any point, including mid-tick.
    /// State may be partially applied and is labelled as best-effort.
    /// </summary>
    Immediate = 1,
}
