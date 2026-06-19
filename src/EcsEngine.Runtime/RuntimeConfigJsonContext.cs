using System.Text.Json.Serialization;

namespace EcsEngine.Runtime;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(RuntimeConfig))]
[JsonSerializable(typeof(RuntimeObservabilityConfig))]
[JsonSerializable(typeof(RuntimeObservabilitySeverity))]
internal sealed partial class RuntimeConfigJsonContext : JsonSerializerContext
{
}
