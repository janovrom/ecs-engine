namespace EcsEngine.Core.Storage;

internal sealed class ArchetypeChunk
{
    private readonly EntityId[] _EntityIds;
    private readonly Dictionary<Type, IComponentColumn> _Columns;

    public int Count { get; private set; }
    public int Capacity { get; }
    public bool IsFull => Count == Capacity;
    public Archetype Owner { get; }

    public ArchetypeChunk(Archetype owner, int capacity, Dictionary<Type, IComponentColumn> columns)
    {
        Owner = owner;
        Capacity = capacity;
        _EntityIds = new EntityId[capacity];
        _Columns = columns;
    }

    public Span<EntityId> EntityIds => _EntityIds.AsSpan(0, Count);

    public ComponentColumn<T> GetColumn<T>() where T : struct, IEcsComponent
        => (ComponentColumn<T>)_Columns[typeof(T)];

    public IComponentColumn GetColumn(Type type) => _Columns[type];

    public bool HasColumn(Type type) => _Columns.ContainsKey(type);

    public int AddEntity(EntityId entity)
    {
        int slot = Count++;
        _EntityIds[slot] = entity;
        return slot;
    }

    // Swap-removes the entity at slot. Returns the EntityId that was moved into slot
    // (i.e. the entity that was previously at the last slot). Returns default if no swap occurred.
    public EntityId RemoveAt(int slot)
    {
        int last = --Count;

        if (slot != last)
        {
            EntityId swapped = _EntityIds[last];
            _EntityIds[slot] = swapped;
            foreach (IComponentColumn col in _Columns.Values)
                col.RemoveAt(slot, last);
            _EntityIds[last] = default;
            foreach (IComponentColumn col in _Columns.Values)
                col.Clear(last);
            return swapped;
        }

        _EntityIds[last] = default;
        foreach (IComponentColumn col in _Columns.Values)
            col.Clear(last);
        return default;
    }
}
