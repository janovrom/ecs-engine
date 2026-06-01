using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Maps component types to deterministic hash functions for use in
/// <see cref="WorldHasher"/>. Hashers are iterated sorted by type name (D-058).
/// </summary>
public sealed class ComponentHasherRegistry
{
    private readonly SortedList<string, Func<EcsWorld, ulong>> _Hashers =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a hash function for component type <typeparamref name="T"/>.
    /// <paramref name="hasher"/> maps a component value to a 32-bit hash.
    /// </summary>
    public void Register<T>(Func<T, uint> hasher)
        where T : struct, IEcsComponent
    {
        string typeName = typeof(T).FullName ?? typeof(T).Name;
        _Hashers[typeName] = world =>
        {
            List<(int Id, uint Hash)> entries = [];
            world.QueryEach<T>((EntityId id, in T c) => entries.Add((id.Value, hasher(c))));
            entries.Sort(static (a, b) => a.Id.CompareTo(b.Id));

            ulong h = Fnv1aOffset;
            foreach ((int id, uint hash) in entries)
            {
                h = Fnv1aMix(h, (ulong)id);
                h = Fnv1aMix(h, hash);
            }
            return h;
        };
    }

    /// <summary>
    /// Registers the deterministic hasher for <typeparamref name="T"/> using
    /// <see cref="DeterministicHasher{T}.Default"/>, which is populated automatically
    /// by the EcsEngine.SourceGen source generator.
    /// </summary>
    public void Register<T>()
        where T : struct, IEcsComponent =>
        Register<T>(c => DeterministicHasher<T>.Default.Hash(in c));

    internal ulong ComputeHash(EcsWorld world)
    {
        ulong h = Fnv1aOffset;
        foreach (Func<EcsWorld, ulong> typeHash in _Hashers.Values)
            h = Fnv1aMix(h, typeHash(world));
        return h;
    }

    // FNV-1a 64-bit constants
    private const ulong Fnv1aOffset = 14695981039346656037UL;
    private const ulong Fnv1aPrime = 1099511628211UL;

    private static ulong Fnv1aMix(ulong state, ulong value)
    {
        state ^= value;
        state *= Fnv1aPrime;
        return state;
    }
}
