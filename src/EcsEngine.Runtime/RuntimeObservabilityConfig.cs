namespace EcsEngine.Runtime;

/// <summary>
/// Runtime configuration for observability controls in release and debug modes.
/// </summary>
public sealed class RuntimeObservabilityConfig
{
    /// <summary>
    /// Enables execution metrics collection when true.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Samples every Nth simulation tick. Values below 1 are treated as 1.
    /// </summary>
    public int PublishEveryTicks { get; init; } = 1;

    /// <summary>
    /// Minimum severity level for future observability logs.
    /// </summary>
    public RuntimeObservabilitySeverity MinimumSeverity { get; init; } = RuntimeObservabilitySeverity.Information;
}

public enum RuntimeObservabilitySeverity
{
    Debug,
    Information,
    Warning,
    Error,
}
