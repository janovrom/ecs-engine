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
        ArgumentOutOfRangeException.ThrowIfNegative(profile.Speed);
        ArgumentOutOfRangeException.ThrowIfNegative(profile.ClimbSpeed);

        path = [];
        error = null;

        if (grid.IsOccupied(start))
        {
            error = $"Start cell {start} is occupied.";
            return false;
        }

        if (!IsWalkableSurfaceCell(grid, start))
        {
            error = $"Start cell {start} is not on a walkable surface.";
            return false;
        }

        if (grid.IsOccupied(target))
        {
            error = $"Target cell {target} is occupied.";
            return false;
        }

        if (!IsWalkableSurfaceCell(grid, target))
        {
            error = $"Target cell {target} is not on a walkable surface.";
            return false;
        }

        int maxMovementUnits = MovementRules.ComputeEffectiveSpeed(profile.Speed, modifiers) * 2;
        int maxReachSteps = maxMovementUnits / 2;

        PriorityQueue<GridPosition, int> frontier = new();
        int startHeuristic = EstimateRemainingCostUnits(start, target);
        frontier.Enqueue(start, startHeuristic);

        Dictionary<GridPosition, int> bestCostByPosition = new()
        {
            [start] = 0
        };

        Dictionary<GridPosition, GridPosition?> previous = new()
        {
            [start] = null
        };

        while (frontier.TryDequeue(out GridPosition current, out int dequeuedPriority))
        {
            int currentCost = bestCostByPosition[current];
            int expectedPriority = currentCost + EstimateRemainingCostUnits(current, target);
            if (dequeuedPriority != expectedPriority)
                continue;

            if (currentCost > maxMovementUnits)
                continue;

            if (currentCost + EstimateRemainingCostUnits(current, target) > maxMovementUnits)
                continue;

            if (current == target)
            {
                path = ReconstructPath(previous, current);
                return true;
            }

            foreach (GridPosition next in GetNeighbors(current))
            {
                if (grid.IsOccupied(next))
                    continue;

                if (!IsWalkableSurfaceCell(grid, next))
                    continue;

                if (IsOutsideReachableRadius(start, next, maxReachSteps))
                    continue;

                int stepCostUnits = ComputeStepCostUnits(current, next, profile);
                if (stepCostUnits == int.MaxValue)
                    continue;

                int nextCost = currentCost + stepCostUnits;

                if (nextCost > maxMovementUnits)
                    continue;

                int lowerBoundToTarget = EstimateRemainingCostUnits(next, target);
                if (nextCost + lowerBoundToTarget > maxMovementUnits)
                    continue;

                if (bestCostByPosition.TryGetValue(next, out int knownCost) && knownCost <= nextCost)
                    continue;

                bestCostByPosition[next] = nextCost;
                previous[next] = current;
                int priority = nextCost + EstimateRemainingCostUnits(next, target);
                frontier.Enqueue(next, priority);
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
        ArgumentOutOfRangeException.ThrowIfNegative(profile.Speed);
        ArgumentOutOfRangeException.ThrowIfNegative(profile.ClimbSpeed);

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

        int remainingMovementUnits = MovementRules.ComputeEffectiveSpeed(profile.Speed, modifiers) * 2;

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
                stepCostUnits = ComputeStepCostUnits(from, to, profile);
            }
            catch (ArgumentOutOfRangeException)
            {
                error = $"Invalid step from {from} to {to}.";
                return false;
            }

            if (stepCostUnits == int.MaxValue)
            {
                error = "Path exceeds movement budget.";
                return false;
            }

            remainingMovementUnits -= stepCostUnits;
            if (remainingMovementUnits < 0)
            {
                error = "Path exceeds movement budget.";
                return false;
            }

            totalCost += stepCostUnits / 2m;
        }

        return true;
    }

    private static IReadOnlyList<GridPosition> ReconstructPath(
        Dictionary<GridPosition, GridPosition?> previous,
        GridPosition end)
    {
        List<GridPosition> reversed = [end];
        GridPosition? cursor = previous[end];

        while (cursor is not null)
        {
            reversed.Add(cursor.Value);
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

                    // Surface traversal: disallow pure vertical hops with no planar movement.
                    if (dx == 0 && dy == 0)
                        continue;

                    yield return new GridPosition(position.X + dx, position.Y + dy, position.Z + dz);
                }
            }
        }
    }

    private static bool IsOutsideReachableRadius(in GridPosition start, in GridPosition candidate, int maxReachSteps)
    {
        int dx = Math.Abs(candidate.X - start.X);
        int dy = Math.Abs(candidate.Y - start.Y);
        int dz = Math.Abs(candidate.Z - start.Z);
        int chebyshevDistance = Math.Max(dx, Math.Max(dy, dz));
        return chebyshevDistance > maxReachSteps;
    }

    private static int EstimateRemainingCostUnits(in GridPosition from, in GridPosition to)
    {
        int dx = Math.Abs(to.X - from.X);
        int dy = Math.Abs(to.Y - from.Y);
        int dz = Math.Abs(to.Z - from.Z);

        // Admissible lower bound for this movement model:
        // minimum step cost is 2 units, and at least max(planar, vertical) adjacent
        // moves are needed when diagonal movement is allowed.
        int planarDistance = Math.Max(dx, dy);
        int minSteps = Math.Max(planarDistance, dz);
        return minSteps * 2;
    }

    private static int ComputeStepCostUnits(in GridPosition from, in GridPosition to, in MovementProfile profile)
    {
        int baseCostUnits = MovementRules.GetStepCostUnits(from, to);

        bool usesClimb = from.Z != to.Z;
        if (!usesClimb)
            return baseCostUnits;

        if (profile.ClimbSpeed <= 0)
            return int.MaxValue;

        decimal climbPenalty = Math.Max(1m, (decimal)Math.Max(profile.Speed, 1) / profile.ClimbSpeed);
        return (int)Math.Ceiling(baseCostUnits * climbPenalty);
    }

    private static bool IsWalkableSurfaceCell(OccupancyGrid grid, in GridPosition position)
    {
        if (grid.IsOccupied(position))
            return false;

        if (position.Z == 0)
            return true;

        GridPosition below = position with { Z = position.Z - 1 };
        return grid.IsOccupied(below);
    }

}
