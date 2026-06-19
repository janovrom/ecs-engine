namespace EcsEngine.Replay;

/// <summary>
/// Describes the scalar payload type for a snapshot component field.
/// </summary>
public enum SnapshotFieldType
{
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
    Byte,
    Boolean,
}

/// <summary>
/// A named scalar field inside a component payload schema.
/// </summary>
public readonly record struct SnapshotComponentField(string Name, SnapshotFieldType Type);

/// <summary>
/// Schema used by <see cref="SnapshotInspector"/> to decode binary component payloads.
/// </summary>
public sealed record SnapshotComponentSchema(string TypeName, IReadOnlyList<SnapshotComponentField> Fields);

/// <summary>
/// A decoded field value from a component payload.
/// </summary>
public sealed record SnapshotDecodedField(string Name, object Value);

/// <summary>
/// A decoded component instance attached to an entity.
/// </summary>
public sealed record SnapshotDecodedComponent(int EntityId, IReadOnlyList<SnapshotDecodedField> Fields);

/// <summary>
/// Decoded component group for a single component type.
/// </summary>
public sealed record SnapshotDecodedType(string TypeName, IReadOnlyList<SnapshotDecodedComponent> Components);

/// <summary>
/// In-memory representation of a decoded snapshot file.
/// </summary>
public sealed record SnapshotInspectionResult(
    ushort Version,
    byte Mode,
    int Tick,
    IReadOnlyList<int> EntityIds,
    IReadOnlyList<SnapshotDecodedType> ComponentTypes);
