using System.Text.Json;

namespace EcsEngine.Simulation;

public static class SimulationScriptLoader
{
    public static SimulationScript LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using FileStream stream = File.OpenRead(path);
        SimulationScript? script = JsonSerializer.Deserialize(stream, SimulationScriptJsonContext.Default.SimulationScript);
        if (script is null)
            throw new InvalidDataException($"Failed to deserialize simulation script from '{path}'.");

        return script;
    }
}
