using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcsEngine.Replay;

/// <summary>
/// Parses binary snapshots and emits decoded data structures that can be rendered
/// as human-readable text or JSON without runtime reflection.
/// </summary>
public sealed class SnapshotInspector
{
    private readonly IReadOnlyDictionary<string, SnapshotComponentSchema> _Schemas;

    public SnapshotInspector(IEnumerable<SnapshotComponentSchema> schemas)
    {
        ArgumentNullException.ThrowIfNull(schemas);
        _Schemas = schemas.ToDictionary(static s => s.TypeName, StringComparer.Ordinal);
    }

    /// <summary>
    /// Reads and decodes a snapshot stream according to the configured schemas.
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// Thrown when the header is invalid, the snapshot version is unsupported,
    /// or a required component schema is missing.
    /// </exception>
    public SnapshotInspectionResult Inspect(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        uint magic = reader.ReadUInt32();
        if (magic != Magic)
            throw new InvalidDataException($"Invalid snapshot magic 0x{magic:X8}. Expected 0x{Magic:X8}.");

        ushort version = reader.ReadUInt16();
        if (version != Version)
            throw new InvalidDataException($"Unsupported snapshot version {version}. Expected {Version}.");

        byte mode = reader.ReadByte();
        int tick = reader.ReadInt32();

        int entityCount = reader.ReadInt32();
        List<int> entityIds = new(entityCount);
        for (int i = 0; i < entityCount; i++)
            entityIds.Add(reader.ReadInt32());

        int componentTypeCount = reader.ReadInt32();
        List<SnapshotDecodedType> decodedTypes = new(componentTypeCount);

        for (int t = 0; t < componentTypeCount; t++)
        {
            ushort nameLength = reader.ReadUInt16();
            byte[] nameBytes = reader.ReadBytes(nameLength);
            string typeName = Encoding.UTF8.GetString(nameBytes);

            if (!_Schemas.TryGetValue(typeName, out SnapshotComponentSchema? schema))
            {
                throw new InvalidDataException(
                    $"No schema registered for component type '{typeName}'. " +
                    "Pass --schema type=field:type,... for every component type in the snapshot.");
            }

            int entityCountForType = reader.ReadInt32();
            List<SnapshotDecodedComponent> components = new(entityCountForType);
            for (int i = 0; i < entityCountForType; i++)
            {
                int entityId = reader.ReadInt32();
                List<SnapshotDecodedField> fields = DecodeFields(reader, schema.Fields);
                components.Add(new SnapshotDecodedComponent(entityId, fields));
            }

            decodedTypes.Add(new SnapshotDecodedType(typeName, components));
        }

        return new SnapshotInspectionResult(version, mode, tick, entityIds, decodedTypes);
    }

    public static string ToJson(SnapshotInspectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return JsonSerializer.Serialize(result, SnapshotInspectorJsonContext.Default.SnapshotInspectionResult);
    }

    public static string ToText(SnapshotInspectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        StringBuilder sb = new();
        sb.AppendLine("Snapshot Inspector Report");
        sb.AppendLine($"Version: {result.Version}");
        sb.AppendLine($"Mode: {result.Mode}");
        sb.AppendLine($"Tick: {result.Tick}");
        sb.AppendLine($"Entities: {result.EntityIds.Count}");

        if (result.EntityIds.Count > 0)
            sb.AppendLine($"EntityIds: {string.Join(", ", result.EntityIds)}");

        sb.AppendLine($"ComponentTypes: {result.ComponentTypes.Count}");

        foreach (SnapshotDecodedType componentType in result.ComponentTypes)
        {
            sb.AppendLine($"- {componentType.TypeName}: {componentType.Components.Count} entries");
            foreach (SnapshotDecodedComponent component in componentType.Components)
            {
                sb.Append("  ");
                sb.Append(component.EntityId);
                sb.Append(": ");
                sb.AppendLine(string.Join(", ", component.Fields.Select(static f => $"{f.Name}={f.Value}")));
            }
        }

        return sb.ToString();
    }

    private static List<SnapshotDecodedField> DecodeFields(
        BinaryReader reader,
        IReadOnlyList<SnapshotComponentField> fields)
    {
        List<SnapshotDecodedField> decoded = new(fields.Count);

        foreach (SnapshotComponentField field in fields)
        {
            object value = field.Type switch
            {
                SnapshotFieldType.Int32 => reader.ReadInt32(),
                SnapshotFieldType.UInt32 => reader.ReadUInt32(),
                SnapshotFieldType.Int64 => reader.ReadInt64(),
                SnapshotFieldType.UInt64 => reader.ReadUInt64(),
                SnapshotFieldType.Single => reader.ReadSingle(),
                SnapshotFieldType.Double => reader.ReadDouble(),
                SnapshotFieldType.Byte => reader.ReadByte(),
                SnapshotFieldType.Boolean => reader.ReadBoolean(),
                _ => throw new InvalidDataException($"Unsupported field type: {field.Type}"),
            };

            decoded.Add(new SnapshotDecodedField(field.Name, value));
        }

        return decoded;
    }

    private const uint Magic = 0x45435353u;
    private const ushort Version = 1;
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(SnapshotInspectionResult))]
[JsonSerializable(typeof(SnapshotDecodedType))]
[JsonSerializable(typeof(SnapshotDecodedComponent))]
[JsonSerializable(typeof(SnapshotDecodedField))]
internal sealed partial class SnapshotInspectorJsonContext : JsonSerializerContext;
