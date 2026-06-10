using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcsEngine.Runtime;

public static class RuntimeConfigLoader
{
    public static RuntimeConfig LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        RuntimeConfig? config = JsonSerializer.Deserialize<RuntimeConfig>(stream, _JsonOptions);
        if (config is null)
            throw new InvalidDataException($"Failed to deserialize runtime configuration from '{path}'.");

        return config;
    }

    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
