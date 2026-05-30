using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

internal static class ComponentTypeRegistry
{
    private static readonly Dictionary<Type, ComponentTypeInfo> _Registry = [];

    public static ComponentTypeInfo GetOrRegister<T>() where T : struct, IEcsComponent
    {
        Type type = typeof(T);
        if (!_Registry.TryGetValue(type, out ComponentTypeInfo? info))
        {
            ArchetypeStorageAttribute? attr = type.GetCustomAttribute<ArchetypeStorageAttribute>();
            StorageKind storageKind = attr is not null ? StorageKind.Archetype : StorageKind.Sparse;
            int sizeBytes = Unsafe.SizeOf<T>();
            int chunkSizeBytes = attr?.ChunkSizeBytes ?? ArchetypeStorageAttribute.DefaultChunkSizeBytes;
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
        => _Registry.TryGetValue(type, out info);
}
