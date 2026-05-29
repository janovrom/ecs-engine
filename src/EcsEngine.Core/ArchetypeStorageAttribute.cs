namespace EcsEngine.Core;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ArchetypeStorageAttribute : Attribute
{
    public const int DefaultChunkSizeBytes = 16 * 1024;

    public int ChunkSizeBytes { get; }

    public ArchetypeStorageAttribute(int chunkSizeBytes = DefaultChunkSizeBytes)
    {
        ChunkSizeBytes = chunkSizeBytes;
    }
}
