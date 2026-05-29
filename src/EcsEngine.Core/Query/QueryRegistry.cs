using EcsEngine.Core.Storage;

namespace EcsEngine.Core.Query;

internal sealed class QueryRegistry
{
    private readonly ArchetypeRegistry _ArchetypeRegistry;
    private readonly Dictionary<QueryKey, List<Archetype>> _Cache = [];

    public QueryRegistry(ArchetypeRegistry archetypeRegistry)
    {
        _ArchetypeRegistry = archetypeRegistry;
        _ArchetypeRegistry.ArchetypeCreated += OnArchetypeCreated;
    }

    private void OnArchetypeCreated(Archetype _) => _Cache.Clear();

    public IReadOnlyList<Archetype> GetMatchingArchetypes(QueryKey key)
    {
        if (!_Cache.TryGetValue(key, out List<Archetype>? result))
        {
            result = BuildMatchingArchetypes(key);
            _Cache[key] = result;
        }
        return result;
    }

    public void Preload(QueryKey key) => GetMatchingArchetypes(key);

    private List<Archetype> BuildMatchingArchetypes(QueryKey key)
    {
        List<Archetype> result = [];
        foreach (Archetype archetype in _ArchetypeRegistry.AllArchetypes.Values)
        {
            if (Matches(archetype, key))
                result.Add(archetype);
        }
        return result;
    }

    private static bool Matches(Archetype archetype, QueryKey key)
    {
        foreach (Type required in key.RequireTypes)
        {
            if (!archetype.HasComponent(required)) return false;
        }
        foreach (Type rejected in key.RejectTypes)
        {
            if (archetype.HasComponent(rejected)) return false;
        }
        return true;
    }
}
