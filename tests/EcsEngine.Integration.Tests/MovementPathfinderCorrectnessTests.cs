using EcsEngine.Simulation;
using NUnit.Framework;

namespace EcsEngine.Integration.Tests;

[TestFixture]
public class MovementPathfinderCorrectnessTests
{
    [Test]
    public void TryFindPath_StartOccupied_ReturnsFalseWithError()
    {
        OccupancyGrid grid = new();
        GridPosition start = new(0, 0, 0);
        grid.SetOccupied(start, occupied: true);

        bool ok = MovementPathfinder.TryFindPath(
            grid,
            start,
            target: new GridPosition(1, 0, 0),
            profile: new MovementProfile(Speed: 6, ClimbSpeed: 6),
            modifiers: MovementModifiers.None,
            out IReadOnlyList<GridPosition> path,
            out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(error, Does.Contain("Start cell"));
        });
    }

    [Test]
    public void TryFindPath_TargetOccupied_ReturnsFalseWithError()
    {
        OccupancyGrid grid = new();
        GridPosition target = new(1, 0, 0);
        grid.SetOccupied(target, occupied: true);

        bool ok = MovementPathfinder.TryFindPath(
            grid,
            start: new GridPosition(0, 0, 0),
            target,
            profile: new MovementProfile(Speed: 6, ClimbSpeed: 6),
            modifiers: MovementModifiers.None,
            out IReadOnlyList<GridPosition> path,
            out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(error, Does.Contain("Target cell"));
        });
    }

    [Test]
    public void TryFindPath_ReachableScenario_ProducesValidBudgetCompliantPath()
    {
        OccupancyGrid grid = new();
        grid.SetOccupied(new GridPosition(1, 0, 0), occupied: true);

        GridPosition start = new(0, 0, 0);
        GridPosition target = new(2, 0, 0);
        MovementProfile profile = new(Speed: 8, ClimbSpeed: 8);

        bool found = MovementPathfinder.TryFindPath(
            grid,
            start,
            target,
            profile,
            MovementModifiers.None,
            out IReadOnlyList<GridPosition> path,
            out string? findError);

        bool valid = MovementPathfinder.TryValidatePath(
            grid,
            path,
            profile,
            MovementModifiers.None,
            out decimal cost,
            out string? validateError);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True, findError);
            Assert.That(path, Is.Not.Empty);
            Assert.That(path[0], Is.EqualTo(start));
            Assert.That(path[^1], Is.EqualTo(target));
            Assert.That(path, Does.Not.Contain(new GridPosition(1, 0, 0)));
            Assert.That(AllStepsAreAdjacent(path), Is.True);
            Assert.That(valid, Is.True, validateError);
            Assert.That(cost, Is.GreaterThan(0m));
        });
    }

    [Test]
    public void TryFindPath_TargetFullyBlocked_ReturnsNoPath()
    {
        OccupancyGrid grid = new();
        GridPosition start = new(0, 0, 0);
        GridPosition target = new(2, 2, 0);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    GridPosition blocker = new(target.X + dx, target.Y + dy, target.Z + dz);
                    if (blocker == start || blocker == target)
                        continue;

                    grid.SetOccupied(blocker, occupied: true);
                }
            }
        }

        bool ok = MovementPathfinder.TryFindPath(
            grid,
            start,
            target,
            profile: new MovementProfile(Speed: 20, ClimbSpeed: 20),
            modifiers: MovementModifiers.None,
            out IReadOnlyList<GridPosition> path,
            out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(error, Does.Contain("No valid path"));
        });
    }

    [Test]
    public void TryValidatePath_WithInvalidNonAdjacentStep_ReturnsFalse()
    {
        OccupancyGrid grid = new();
        IReadOnlyList<GridPosition> path =
        [
            new GridPosition(0, 0, 0),
            new GridPosition(2, 0, 0),
        ];

        bool ok = MovementPathfinder.TryValidatePath(
            grid,
            path,
            profile: new MovementProfile(Speed: 10, ClimbSpeed: 10),
            modifiers: MovementModifiers.None,
            out decimal cost,
            out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(cost, Is.EqualTo(0m));
            Assert.That(error, Does.Contain("Invalid step"));
        });
    }

    [Test]
    public void TryValidatePath_ExceedingClimbBudget_ReturnsFalse()
    {
        OccupancyGrid grid = new();
        IReadOnlyList<GridPosition> path =
        [
            new GridPosition(0, 0, 0),
            new GridPosition(0, 0, 1),
            new GridPosition(0, 0, 2),
        ];

        bool ok = MovementPathfinder.TryValidatePath(
            grid,
            path,
            profile: new MovementProfile(Speed: 10, ClimbSpeed: 1),
            modifiers: MovementModifiers.None,
            out decimal cost,
            out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(cost, Is.GreaterThanOrEqualTo(0m));
            Assert.That(error, Does.Contain("exceeds movement budget"));
        });
    }

    [Test]
    public void TryValidatePath_ClimbIsModifier_NotAdditiveBudget()
    {
        OccupancyGrid grid = new();
        grid.SetOccupied(new GridPosition(0, 0, 0), occupied: true);
        grid.SetOccupied(new GridPosition(1, 0, 1), occupied: true);
        grid.SetOccupied(new GridPosition(2, 0, 2), occupied: true);

        IReadOnlyList<GridPosition> path =
        [
            new GridPosition(0, 0, 1),
            new GridPosition(1, 0, 2),
            new GridPosition(2, 0, 3),
        ];

        bool ok = MovementPathfinder.TryValidatePath(
            grid,
            path,
            profile: new MovementProfile(Speed: 2, ClimbSpeed: 1),
            modifiers: MovementModifiers.None,
            out decimal cost,
            out string? error);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(cost, Is.GreaterThanOrEqualTo(0m));
            Assert.That(error, Does.Contain("exceeds movement budget"));
        });
    }

    private static bool AllStepsAreAdjacent(IReadOnlyList<GridPosition> path)
    {
        for (int i = 1; i < path.Count; i++)
        {
            int dx = Math.Abs(path[i].X - path[i - 1].X);
            int dy = Math.Abs(path[i].Y - path[i - 1].Y);
            int dz = Math.Abs(path[i].Z - path[i - 1].Z);

            if (dx > 1 || dy > 1 || dz > 1)
                return false;

            if (dx == 0 && dy == 0 && dz == 0)
                return false;
        }

        return true;
    }
}
