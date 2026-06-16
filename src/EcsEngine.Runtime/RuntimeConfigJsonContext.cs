using System.Text.Json.Serialization;

namespace EcsEngine.Runtime;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(RuntimeConfig))]
internal sealed partial class RuntimeConfigJsonContext : JsonSerializerContext
{
}
