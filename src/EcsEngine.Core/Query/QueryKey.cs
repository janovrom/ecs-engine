namespace EcsEngine.Core.Query;

internal readonly struct QueryKey : IEquatable<QueryKey>
{
    private readonly Type[] _RequireTypes; // sorted by FullName
    private readonly Type[] _RejectTypes;  // sorted by FullName

    public IReadOnlyList<Type> RequireTypes => _RequireTypes;
    public IReadOnlyList<Type> RejectTypes => _RejectTypes;

    private QueryKey(Type[] require, Type[] reject)
    {
        _RequireTypes = require;
        _RejectTypes = reject;
    }

    public static QueryKey For<T1>()
        where T1 : struct, IEcsComponent
        => new([typeof(T1)], []);

    public static QueryKey For<T1, T2>()
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        => new(Sort([typeof(T1), typeof(T2)]), []);

    public static QueryKey For<T1, T2, T3>()
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        => new(Sort([typeof(T1), typeof(T2), typeof(T3)]), []);

    public QueryKey WithReject(Type rejectType)
        => new(_RequireTypes, Sort([.. _RejectTypes, rejectType]));

    private static Type[] Sort(Type[] types)
    {
        Array.Sort(types, static (a, b) => StringComparer.Ordinal.Compare(a.FullName, b.FullName));
        return types;
    }

    public bool Equals(QueryKey other)
        => _RequireTypes.SequenceEqual(other._RequireTypes)
        && _RejectTypes.SequenceEqual(other._RejectTypes);

    public override bool Equals(object? obj) => obj is QueryKey other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (Type t in _RequireTypes) hash.Add(t);
        hash.Add(0);
        foreach (Type t in _RejectTypes) hash.Add(t);
        return hash.ToHashCode();
    }
}
