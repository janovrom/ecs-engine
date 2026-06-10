namespace EcsEngine.Runtime;

/// <summary>
/// Runtime startup configuration loaded from file.
/// </summary>
public sealed class RuntimeConfig
{
    public StartupMode StartupMode { get; init; } = StartupMode.Local;
}
