using System.Text.Json.Serialization;

namespace EcsEngine.Simulation;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SimulationScript))]
[JsonSerializable(typeof(ScriptedOperationBase))]
[JsonSerializable(typeof(SetOccupiedOperation))]
[JsonSerializable(typeof(RaiseElevationOperation))]
[JsonSerializable(typeof(PreviewPathOperation))]
[JsonSerializable(typeof(CommitPreviewPathOperation))]
internal sealed partial class SimulationScriptJsonContext : JsonSerializerContext
{
}
