namespace EcsEngine.Simulation;

public sealed class PathPreviewSession
{
    private readonly List<GridPosition> _PreviewSteps = new();

    public IReadOnlyList<GridPosition> PreviewSteps => _PreviewSteps;

    public CommittedPath? LastCommittedPath { get; private set; }

    public decimal? LastCommittedCost { get; private set; }

    public void SetPreview(IEnumerable<GridPosition> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        _PreviewSteps.Clear();
        _PreviewSteps.AddRange(steps);
    }

    public void AppendStep(in GridPosition step) => _PreviewSteps.Add(step);

    public void ClearPreview() => _PreviewSteps.Clear();

    public bool TryBuildPreview(
        OccupancyGrid grid,
        in GridPosition start,
        in GridPosition target,
        in MovementProfile profile,
        MovementModifiers modifiers,
        out string? error)
    {
        if (!MovementPathfinder.TryFindPath(grid, start, target, profile, modifiers, out IReadOnlyList<GridPosition> path, out error))
            return false;

        SetPreview(path);
        return true;
    }

    public bool TryCommitValidated(
        OccupancyGrid grid,
        in MovementProfile profile,
        MovementModifiers modifiers,
        out CommittedPath? committed,
        out string? error)
    {
        committed = null;

        if (!MovementPathfinder.TryValidatePath(grid, _PreviewSteps, profile, modifiers, out decimal cost, out error))
            return false;

        committed = new CommittedPath(_PreviewSteps.ToArray());
        LastCommittedPath = committed;
        LastCommittedCost = cost;
        _PreviewSteps.Clear();
        return true;
    }

    public CommittedPath Commit()
    {
        GridPosition[] committed = _PreviewSteps.ToArray();
        LastCommittedPath = new CommittedPath(committed);
        LastCommittedCost = null;
        _PreviewSteps.Clear();
        return LastCommittedPath;
    }
}
