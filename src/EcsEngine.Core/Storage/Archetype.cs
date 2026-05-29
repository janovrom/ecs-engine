namespace EcsEngine.Core.Storage;

internal sealed class Archetype
{
    private readonly List<ArchetypeChunk> _Chunks = [];
    private readonly Dictionary<Type, Func<int, IComponentColumn>> _ColumnFactories;

    public ArchetypeKey Key { get; }
    public int ChunkCapacity { get; }
    public IReadOnlyList<ArchetypeChunk> Chunks => _Chunks;

    public Archetype(ArchetypeKey key, int chunkCapacity, Dictionary<Type, Func<int, IComponentColumn>> columnFactories)
    {
        Key = key;
        ChunkCapacity = chunkCapacity;
        _ColumnFactories = columnFactories;
    }

    public bool HasComponent(Type type) => Key.Contains(type);

    public (ArchetypeChunk chunk, int slot) AddEntity(EntityId entity)
    {
        ArchetypeChunk chunk = GetOrCreateAvailableChunk();
        int slot = chunk.AddEntity(entity);
        return (chunk, slot);
    }

    private ArchetypeChunk GetOrCreateAvailableChunk()
    {
        foreach (ArchetypeChunk chunk in _Chunks)
        {
            if (!chunk.IsFull) return chunk;
        }
        return CreateChunk();
    }

    private ArchetypeChunk CreateChunk()
    {
        Dictionary<Type, IComponentColumn> columns = [];
        foreach ((Type type, Func<int, IComponentColumn> factory) in _ColumnFactories)
            columns[type] = factory(ChunkCapacity);
        ArchetypeChunk chunk = new(this, ChunkCapacity, columns);
        _Chunks.Add(chunk);
        return chunk;
    }
}
