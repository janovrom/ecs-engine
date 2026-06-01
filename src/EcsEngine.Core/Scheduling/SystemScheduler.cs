namespace EcsEngine.Core.Scheduling;

/// <summary>
/// Registers systems, validates their dependency graph, and builds a
/// <see cref="SystemExecutor"/> with a deterministic execution order.
/// </summary>
public sealed class SystemScheduler
{
    private readonly List<(IEcsSystem System, SystemBuilder Metadata)> _Registrations = [];

    /// <summary>
    /// Registers a system. <typeparamref name="T"/>'s static <c>Configure</c> method
    /// is called to collect its dependency declarations.
    /// </summary>
    /// <remarks>
    /// Always pass the concrete type as <typeparamref name="T"/> so that the correct
    /// static <see cref="IEcsSystem.Configure"/> override is invoked.
    /// </remarks>
    public SystemScheduler Register<T>(T system) where T : IEcsSystem
    {
        Type type = system.GetType();
        foreach ((IEcsSystem existing, _) in _Registrations)
        {
            if (existing.GetType() == type)
                throw new InvalidOperationException(
                    $"System of type '{type.FullName}' is already registered.");
        }

        SystemBuilder builder = new();
        T.Configure(builder);
        _Registrations.Add((system, builder));
        return this;
    }

    /// <summary>
    /// Validates the dependency graph, detects cycles, and returns a
    /// <see cref="SystemExecutor"/> with systems in topological order.
    /// </summary>
    /// <exception cref="SystemSchedulingException">
    /// Thrown when a cycle is detected or an After/Before constraint references
    /// a system type that was not registered.
    /// </exception>
    public SystemExecutor Build()
    {
        if (_Registrations.Count == 0)
            return new SystemExecutor([]);

        // Index systems by runtime type
        Dictionary<Type, (IEcsSystem System, SystemBuilder Metadata)> nodes = [];
        foreach ((IEcsSystem system, SystemBuilder meta) in _Registrations)
            nodes[system.GetType()] = (system, meta);

        // Validate all After/Before references point to registered systems
        foreach ((_, SystemBuilder meta) in _Registrations)
        {
            foreach (Type dep in meta.AfterTypes.Concat(meta.BeforeTypes))
            {
                if (!nodes.ContainsKey(dep))
                    throw new SystemSchedulingException(
                        $"System '{dep.FullName}' is referenced in a dependency constraint but is not registered.",
                        [dep]);
            }
        }

        // Build directed graph: edge from→to means 'from' must execute before 'to'
        // HashSet<T>.Add returns true only on new insertion, giving free deduplication.
        Dictionary<Type, HashSet<Type>> successors = [];
        Dictionary<Type, int> inDegree = [];
        foreach (Type type in nodes.Keys)
        {
            successors[type] = [];
            inDegree[type] = 0;
        }

        foreach ((Type type, (_, SystemBuilder meta)) in nodes)
        {
            foreach (Type predecessor in meta.AfterTypes)
            {
                if (successors[predecessor].Add(type))
                    inDegree[type]++;
            }

            foreach (Type successor in meta.BeforeTypes)
            {
                if (successors[type].Add(successor))
                    inDegree[successor]++;
            }
        }

        // Kahn's topological sort — SortedSet on FullName provides deterministic
        // tie-breaking when multiple systems are equally schedulable (D-052).
        SortedSet<string> available = [];
        Dictionary<string, Type> nameToType = [];
        foreach (Type type in nodes.Keys)
        {
            nameToType[type.FullName!] = type;
            if (inDegree[type] == 0)
                available.Add(type.FullName!);
        }

        List<IEcsSystem> ordered = new(_Registrations.Count);
        while (available.Count > 0)
        {
            string first = available.Min!;
            available.Remove(first);
            Type current = nameToType[first];
            ordered.Add(nodes[current].System);

            foreach (Type successor in successors[current])
            {
                inDegree[successor]--;
                if (inDegree[successor] == 0)
                    available.Add(successor.FullName!);
            }
        }

        if (ordered.Count < nodes.Count)
        {
            HashSet<Type> placed = [.. ordered.Select(s => s.GetType())];
            List<Type> cycleTypes = [.. nodes.Keys.Where(t => !placed.Contains(t))];
            string names = string.Join(", ", cycleTypes.Select(t => t.Name));
            throw new SystemSchedulingException(
                $"Cycle detected in system dependency graph involving: {names}",
                cycleTypes);
        }

        return new SystemExecutor([.. ordered]);
    }
}
