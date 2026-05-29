namespace EcsEngine.Core.Storage;

internal readonly struct ArchetypeKey : IEquatable<ArchetypeKey>
{
    private readonly Type[] _ComponentTypes; // sorted by FullName, ascending

    public static readonly ArchetypeKey Empty = new(Array.Empty<Type>());

    internal ArchetypeKey(IEnumerable<Type> types)
    {
        _ComponentTypes = [.. types.OrderBy(static t => t.FullName, StringComparer.Ordinal)];
    }

    private ArchetypeKey(Type[] sorted)
    {
        _ComponentTypes = sorted;
    }

    public IReadOnlyList<Type> ComponentTypes => _ComponentTypes;

    public bool Contains(Type type)
    {
        foreach (Type t in _ComponentTypes)
        {
            if (t == type) return true;
        }
        return false;
    }

    public ArchetypeKey Add(Type type)
    {
        if (Contains(type)) return this;
        Type[] newTypes = [.. _ComponentTypes, type];
        Array.Sort(newTypes, static (a, b) => StringComparer.Ordinal.Compare(a.FullName, b.FullName));
        return new ArchetypeKey(newTypes);
    }

    public ArchetypeKey Remove(Type type)
    {
        if (!Contains(type)) return this;
        return new ArchetypeKey(_ComponentTypes.Where(t => t != type));
    }

    public bool Equals(ArchetypeKey other)
    {
        if (_ComponentTypes.Length != other._ComponentTypes.Length) return false;
        for (int i = 0; i < _ComponentTypes.Length; i++)
        {
            if (_ComponentTypes[i] != other._ComponentTypes[i]) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is ArchetypeKey other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (Type type in _ComponentTypes)
            hash.Add(type);
        return hash.ToHashCode();
    }
}
