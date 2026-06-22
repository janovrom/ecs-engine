using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EcsEngine.Core.Storage;

internal sealed class ComponentTypeInfo
{
    public Type Type { get; }
    public StorageKind StorageKind { get; }
    public int SizeBytes { get; }
    public int ChunkSizeBytes { get; }
    public Func<int, IComponentColumn> ColumnFactory { get; }

    public ComponentTypeInfo(
        Type type,
        StorageKind storageKind,
        int sizeBytes,
        int chunkSizeBytes,
        Func<int, IComponentColumn> columnFactory)
    {
        Type = type;
        StorageKind = storageKind;
        SizeBytes = sizeBytes;
        ChunkSizeBytes = chunkSizeBytes;
        ColumnFactory = columnFactory;
    }
}

/// <summary>
/// Registry of component type metadata. Populated at module initialization by
/// <c>ComponentRegistryGenerator</c> (via compile-time attribute scanning) without
/// runtime reflection on the hot path.
/// </summary>
internal static partial class ComponentTypeRegistry
{
    private static readonly Dictionary<Type, ComponentTypeInfo> _Registry = [];

    // Populated by generated code in ComponentRegistry.g.cs via [ModuleInitializer]
    // Public scope to allow generated initializer code to populate it
    internal static readonly Dictionary<Type, ComponentTypeInfo> _GeneratedRegistry = [];

    /// <summary>
    /// Retrieves or lazily registers component type metadata.
    /// Checks generated registry first (no reflection); falls back to dynamic
    /// registration only if type was not discovered at compile time.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentTypeInfo GetOrRegister<T>() where T : struct, IEcsComponent
    {
        Type type = typeof(T);

        // Fast path: check generated registry (pre-populated at module init, no reflection).
        if (_GeneratedRegistry.TryGetValue(type, out ComponentTypeInfo? generated))
        {
            return generated;
        }

        // Fallback: legacy dynamic registration (for dynamically loaded components, if any).
        if (!_Registry.TryGetValue(type, out ComponentTypeInfo? info))
        {
            StorageKind storageKind = StorageKind.Sparse; // Safe default: sparse-set.
            int sizeBytes = Unsafe.SizeOf<T>();
            int chunkSizeBytes = ArchetypeStorageAttribute.DefaultChunkSizeBytes;
            info = new ComponentTypeInfo(
                type,
                storageKind,
                sizeBytes,
                chunkSizeBytes,
                static capacity => new ComponentColumn<T>(capacity));
            _Registry[type] = info;
        }
        return info;
    }

    public static bool TryGet(Type type, [NotNullWhen(true)] out ComponentTypeInfo? info)
    {
        // Check generated registry first.
        if (_GeneratedRegistry.TryGetValue(type, out info))
        {
            Debug.Assert(info is not null);
            return true;
        }

        // Fall back to dynamic registry.
        bool found = _Registry.TryGetValue(type, out info);
        Debug.Assert(!found || info is not null);
        return found;
    }
}
