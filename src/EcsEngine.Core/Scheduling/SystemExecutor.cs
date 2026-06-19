namespace EcsEngine.Core.Scheduling;

/// <summary>
/// Holds the ordered sequence of systems produced by <see cref="SystemScheduler.Build"/>
/// and executes them sequentially via <see cref="Run"/>.
/// </summary>
public sealed class SystemExecutor
{
    private readonly IEcsSystem[] _Systems;
    private readonly SystemDependencyEdge[] _DependencyEdges;
    private readonly BatchingSnapshot _Batching;

    /// <summary>The systems in their validated execution order.</summary>
    public IReadOnlyList<IEcsSystem> Systems => _Systems;

    /// <summary>All dependency edges where <c>From</c> must execute before <c>To</c>.</summary>
    public IReadOnlyList<SystemDependencyEdge> DependencyEdges => _DependencyEdges;

    internal SystemExecutor(IEcsSystem[] systems, SystemDependencyEdge[] dependencyEdges)
    {
        _Systems = systems;
        _DependencyEdges = dependencyEdges;
        _Batching = BuildBatchingSnapshot(systems, dependencyEdges);
    }

    /// <summary>
    /// Executes all registered systems in topological order for the current tick.
    /// This is the single execution entry point (D-033, D-051).
    /// </summary>
    public void Run(EcsWorld world, ISystemExecutionObserver? observer = null)
    {
        if (observer is null || !observer.IsEnabled)
        {
            foreach (IEcsSystem system in _Systems)
                system.Execute(world);
            return;
        }

        int tick = world.Tick;
        if (!observer.ShouldSampleTick(tick))
        {
            foreach (IEcsSystem system in _Systems)
                system.Execute(world);
            return;
        }

        long tickStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long tickStartAlloc = GC.GetAllocatedBytesForCurrentThread();
        observer.OnTickStarted(
            tick,
            _Batching.SystemCount,
            _Batching.BatchCount,
            _Batching.MaxBatchSize,
            _Batching.Efficiency);

        foreach (IEcsSystem system in _Systems)
        {
            long startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            long startAlloc = GC.GetAllocatedBytesForCurrentThread();

            system.Execute(world);

            long elapsedTicks = System.Diagnostics.Stopwatch.GetTimestamp() - startTicks;
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - startAlloc;
            observer.OnSystemExecuted(tick, system.GetType(), elapsedTicks, allocatedBytes);
        }

        long tickElapsed = System.Diagnostics.Stopwatch.GetTimestamp() - tickStartTicks;
        long tickAlloc = GC.GetAllocatedBytesForCurrentThread() - tickStartAlloc;
        observer.OnTickCompleted(tick, tickElapsed, tickAlloc);
    }

    /// <summary>
    /// Exports the dependency graph as a deterministic DOT digraph.
    /// </summary>
    public string ExportDependencyGraphDot()
    {
        IEnumerable<Type> nodeTypes = _Systems.Select(static s => s.GetType())
            .OrderBy(static t => t.FullName, StringComparer.Ordinal);

        IEnumerable<SystemDependencyEdge> edges = _DependencyEdges
            .OrderBy(static e => e.From.FullName, StringComparer.Ordinal)
            .ThenBy(static e => e.To.FullName, StringComparer.Ordinal);

        System.Text.StringBuilder sb = new();
        sb.AppendLine("digraph SystemSchedule {");

        foreach (Type type in nodeTypes)
            sb.AppendLine($"  \"{type.FullName}\";");

        foreach (SystemDependencyEdge edge in edges)
            sb.AppendLine($"  \"{edge.From.FullName}\" -> \"{edge.To.FullName}\";");

        sb.Append('}');
        return sb.ToString();
    }

    private static BatchingSnapshot BuildBatchingSnapshot(
        IReadOnlyList<IEcsSystem> systems,
        IReadOnlyList<SystemDependencyEdge> dependencyEdges)
    {
        int systemCount = systems.Count;
        if (systemCount == 0)
            return new BatchingSnapshot(0, 0, 0, 0d);

        Dictionary<Type, int> indices = new(systemCount);
        for (int i = 0; i < systemCount; i++)
            indices[systems[i].GetType()] = i;

        int[] indegree = new int[systemCount];
        List<int>[] successors = new List<int>[systemCount];
        for (int i = 0; i < successors.Length; i++)
            successors[i] = [];

        foreach (SystemDependencyEdge edge in dependencyEdges)
        {
            if (!indices.TryGetValue(edge.From, out int from) || !indices.TryGetValue(edge.To, out int to))
                continue;

            successors[from].Add(to);
            indegree[to]++;
        }

        bool[] processed = new bool[systemCount];
        int processedCount = 0;
        int batchCount = 0;
        int maxBatchSize = 0;

        while (processedCount < systemCount)
        {
            List<int> layer = [];
            for (int i = 0; i < systemCount; i++)
            {
                if (!processed[i] && indegree[i] == 0)
                    layer.Add(i);
            }

            if (layer.Count == 0)
                break;

            batchCount++;
            if (layer.Count > maxBatchSize)
                maxBatchSize = layer.Count;

            foreach (int node in layer)
            {
                processed[node] = true;
                processedCount++;

                foreach (int next in successors[node])
                    indegree[next]--;
            }
        }

        if (batchCount == 0)
            batchCount = systemCount;

        if (maxBatchSize == 0)
            maxBatchSize = 1;

        double efficiency = ComputeBatchingEfficiency(systemCount, batchCount);
        return new BatchingSnapshot(systemCount, batchCount, maxBatchSize, efficiency);
    }

    private static double ComputeBatchingEfficiency(int systemCount, int batchCount)
    {
        if (systemCount <= 1)
            return 1d;

        double score = 1d - ((double)(batchCount - 1) / (systemCount - 1));
        return Math.Clamp(score, 0d, 1d);
    }

    private readonly record struct BatchingSnapshot(
        int SystemCount,
        int BatchCount,
        int MaxBatchSize,
        double Efficiency);
}
