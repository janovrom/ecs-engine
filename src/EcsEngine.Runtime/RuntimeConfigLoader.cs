using System.Text.Json;
namespace EcsEngine.Runtime;

public static class RuntimeConfigLoader
{
    public static RuntimeConfig LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        RuntimeConfig? config = JsonSerializer.Deserialize(stream, RuntimeConfigJsonContext.Default.RuntimeConfig);
        if (config is null)
            throw new InvalidDataException($"Failed to deserialize runtime configuration from '{path}'.");

        return config;
    }
}
