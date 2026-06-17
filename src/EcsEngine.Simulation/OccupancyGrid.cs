namespace EcsEngine.Simulation;

public sealed class OccupancyGrid
{
    private readonly HashSet<GridPosition> _Occupied = new();

    public IReadOnlyCollection<GridPosition> OccupiedCells => _Occupied;

    public bool IsOccupied(in GridPosition position) => _Occupied.Contains(position);

    public void SetOccupied(in GridPosition position, bool occupied)
    {
        if (occupied)
            _Occupied.Add(position);
        else
            _Occupied.Remove(position);
    }

    public void RaiseElevation(IEnumerable<GridPosition> selectedCells)
    {
        ArgumentNullException.ThrowIfNull(selectedCells);

        HashSet<GridPosition> selected = new(selectedCells);
        Dictionary<GridPosition, GridPosition> movedCells = new();

        foreach (GridPosition cell in selected)
        {
            if (!_Occupied.Contains(cell))
                continue;

            GridPosition raised = cell with { Z = cell.Z + 1 };
            movedCells[cell] = raised;
        }

        foreach (GridPosition raised in movedCells.Values)
        {
            if (_Occupied.Contains(raised) && !movedCells.ContainsKey(raised))
                throw new InvalidOperationException($"Cannot raise elevation into occupied cell {raised}.");
        }

        foreach (GridPosition cell in movedCells.Keys)
            _Occupied.Remove(cell);

        foreach ((_, GridPosition raised) in movedCells)
            _Occupied.Add(raised);
    }
}
