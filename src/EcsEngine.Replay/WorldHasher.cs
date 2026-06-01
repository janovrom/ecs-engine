using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Computes a deterministic state hash for an <see cref="EcsWorld"/> (D-058).
/// The hash covers: tick, alive entity IDs sorted ascending, and per-registered
/// component type the sorted (entityId, componentHash) pairs.
/// </summary>
public sealed class WorldHasher
{
    private readonly ComponentHasherRegistry? _ComponentHashers;

    public WorldHasher(ComponentHasherRegistry? componentHashers = null)
    {
        _ComponentHashers = componentHashers;
    }

    /// <summary>
    /// Returns a deterministic 64-bit hash of the current world state.
    /// Two worlds with identical entity sets, ticks, and component values produce
    /// the same hash.
    /// </summary>
    public ulong Hash(EcsWorld world)
    {
        ulong h = Fnv1aOffset;
        h = Mix(h, (ulong)world.Tick);

        int[] entityIds = [.. world.AliveEntityIds.Order()];
        h = Mix(h, (ulong)entityIds.Length);
        foreach (int id in entityIds)
            h = Mix(h, (ulong)id);

        if (_ComponentHashers is not null)
            h = Mix(h, _ComponentHashers.ComputeHash(world));

        return h;
    }

    private const ulong Fnv1aOffset = 14695981039346656037UL;
    private const ulong Fnv1aPrime = 1099511628211UL;

    private static ulong Mix(ulong state, ulong value)
    {
        state ^= value;
        state *= Fnv1aPrime;
        return state;
    }
}
