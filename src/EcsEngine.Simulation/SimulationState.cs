namespace EcsEngine.Simulation;

public sealed class SimulationState
{
    public OccupancyGrid Grid { get; } = new();

    public PathPreviewSession PathPreview { get; } = new();

    public GridPosition? LastCommittedPosition { get; private set; }

    public void ApplyCommittedPath(CommittedPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Steps.Count > 0)
            LastCommittedPosition = path.Steps[^1];
    }
}
