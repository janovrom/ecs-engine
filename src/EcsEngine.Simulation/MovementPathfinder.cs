namespace EcsEngine.Simulation;

public static class MovementPathfinder
{
    public static bool TryFindPath(
        OccupancyGrid grid,
        in GridPosition start,
        in GridPosition target,
        in MovementProfile profile,
        MovementModifiers modifiers,
        out IReadOnlyList<GridPosition> path,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(grid);

        path = [];
        error = null;

        if (grid.IsOccupied(start))
        {
            error = $"Start cell {start} is occupied.";
            return false;
        }

        if (grid.IsOccupied(target))
        {
            error = $"Target cell {target} is occupied.";
            return false;
        }

        int maxSpeedUnits = MovementRules.ComputeEffectiveSpeed(profile.Speed, modifiers) * 2;
        int maxClimbUnits = MovementRules.ComputeEffectiveSpeed(profile.ClimbSpeed, modifiers) * 2;

        PathState startState = new(start, maxSpeedUnits, maxClimbUnits);
        PriorityQueue<PathState, int> frontier = new();
        frontier.Enqueue(startState, 0);

        Dictionary<PathState, int> bestCost = new()
        {
            [startState] = 0
        };

        Dictionary<PathState, PathState?> previous = new()
        {
            [startState] = null
        };

        while (frontier.TryDequeue(out PathState current, out _))
        {
            if (current.Position == target)
            {
                path = ReconstructPath(previous, current);
                return true;
            }

            foreach (GridPosition next in GetNeighbors(current.Position))
            {
                if (grid.IsOccupied(next))
                    continue;

                int costUnits = MovementRules.GetStepCostUnits(current.Position, next);
                bool usesClimb = current.Position.Z != next.Z;

                int nextSpeedUnits = current.SpeedUnits;
                int nextClimbUnits = current.ClimbUnits;
                if (usesClimb)
                    nextClimbUnits -= costUnits;
                else
                    nextSpeedUnits -= costUnits;

                if (nextSpeedUnits < 0 || nextClimbUnits < 0)
                    continue;

                PathState nextState = new(next, nextSpeedUnits, nextClimbUnits);
                int nextCost = bestCost[current] + costUnits;

                if (bestCost.TryGetValue(nextState, out int knownCost) && knownCost <= nextCost)
                    continue;

                bestCost[nextState] = nextCost;
                previous[nextState] = current;
                frontier.Enqueue(nextState, nextCost);
            }
        }

        error = $"No valid path from {start} to {target} for the current movement budget.";
        return false;
    }

    public static bool TryValidatePath(
        OccupancyGrid grid,
        IReadOnlyList<GridPosition> path,
        in MovementProfile profile,
        MovementModifiers modifiers,
        out decimal totalCost,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(path);

        totalCost = 0m;
        error = null;

        if (path.Count < 2)
        {
            error = "Path must contain at least a start and a destination.";
            return false;
        }

        if (grid.IsOccupied(path[0]))
        {
            error = $"Start cell {path[0]} is occupied.";
            return false;
        }

        int remainingSpeedUnits = MovementRules.ComputeEffectiveSpeed(profile.Speed, modifiers) * 2;
        int remainingClimbUnits = MovementRules.ComputeEffectiveSpeed(profile.ClimbSpeed, modifiers) * 2;

        for (int i = 1; i < path.Count; i++)
        {
            GridPosition from = path[i - 1];
            GridPosition to = path[i];

            if (grid.IsOccupied(to))
            {
                error = $"Path moves into occupied cell {to}.";
                return false;
            }

            int stepCostUnits;
            try
            {
                stepCostUnits = MovementRules.GetStepCostUnits(from, to);
            }
            catch (ArgumentOutOfRangeException)
            {
                error = $"Invalid step from {from} to {to}.";
                return false;
            }

            bool usesClimb = from.Z != to.Z;
            if (usesClimb)
                remainingClimbUnits -= stepCostUnits;
            else
                remainingSpeedUnits -= stepCostUnits;

            if (remainingSpeedUnits < 0 || remainingClimbUnits < 0)
            {
                error = "Path exceeds movement budget.";
                return false;
            }

            totalCost += stepCostUnits / 2m;
        }

        return true;
    }

    private static IReadOnlyList<GridPosition> ReconstructPath(
        Dictionary<PathState, PathState?> previous,
        PathState end)
    {
        List<GridPosition> reversed = [end.Position];
        PathState? cursor = previous[end];

        while (cursor is not null)
        {
            reversed.Add(cursor.Value.Position);
            cursor = previous[cursor.Value];
        }

        reversed.Reverse();
        return reversed;
    }

    private static IEnumerable<GridPosition> GetNeighbors(GridPosition position)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    yield return new GridPosition(position.X + dx, position.Y + dy, position.Z + dz);
                }
            }
        }
    }

    private readonly record struct PathState(GridPosition Position, int SpeedUnits, int ClimbUnits);
}
