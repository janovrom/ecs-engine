using BenchmarkDotNet.Attributes;
using EcsEngine.Simulation;

namespace EcsEngine.Benchmarks;

[MemoryDiagnoser]
public class MovementPathfinderBenchmarks
{
    [Params(16, 32, 64)]
    public int GridSize { get; set; }

    [Params(0.00, 0.10, 0.20)]
    public double ObstacleRatio { get; set; }

    private OccupancyGrid _ObstacleGrid = null!;
    private OccupancyGrid _OpenGrid = null!;
    private MovementProfile _Profile;
    private IReadOnlyList<GridPosition> _PrecomputedPath = null!;
    private GridPosition _Start;
    private GridPosition _Target;

    [GlobalSetup]
    public void Setup()
    {
        _Start = new GridPosition(0, 0, 0);
        _Target = new GridPosition(GridSize - 1, GridSize - 1, 0);
        _Profile = new MovementProfile(Speed: GridSize * 2, ClimbSpeed: GridSize * 2);

        _OpenGrid = new OccupancyGrid();
        _ObstacleGrid = BuildObstacleGrid(GridSize, ObstacleRatio, seed: 1337);

        if (!MovementPathfinder.TryFindPath(
            _OpenGrid,
            _Start,
            _Target,
            _Profile,
            MovementModifiers.None,
            out IReadOnlyList<GridPosition> path,
            out string? error))
        {
            throw new InvalidOperationException($"Failed to precompute validation path: {error}");
        }

        _PrecomputedPath = path;
    }

    [Benchmark(Baseline = true)]
    public bool TryFindPath_OpenGrid()
    {
        return MovementPathfinder.TryFindPath(
            _OpenGrid,
            _Start,
            _Target,
            _Profile,
            MovementModifiers.None,
            out _,
            out _);
    }

    [Benchmark]
    public bool TryFindPath_WithObstacles()
    {
        return MovementPathfinder.TryFindPath(
            _ObstacleGrid,
            _Start,
            _Target,
            _Profile,
            MovementModifiers.None,
            out _,
            out _);
    }

    [Benchmark]
    public bool TryValidatePath_Precomputed()
    {
        return MovementPathfinder.TryValidatePath(
            _OpenGrid,
            _PrecomputedPath,
            _Profile,
            MovementModifiers.None,
            out _,
            out _);
    }

    private static OccupancyGrid BuildObstacleGrid(int size, double obstacleRatio, int seed)
    {
        OccupancyGrid grid = new();
        if (obstacleRatio <= 0)
            return grid;

        Random random = new(seed + size);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GridPosition position = new(x, y, 0);
                if (position == new GridPosition(0, 0, 0) || position == new GridPosition(size - 1, size - 1, 0))
                    continue;

                if (random.NextDouble() < obstacleRatio)
                    grid.SetOccupied(position, occupied: true);
            }
        }

        return grid;
    }
}
