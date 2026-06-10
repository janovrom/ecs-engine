namespace EcsEngine.Transport;

/// <summary>
/// Minimal transport abstraction used by runtime startup modes.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// Publishes a payload under a logical topic.
    /// </summary>
    void Publish(string topic, ReadOnlySpan<byte> payload);

    /// <summary>
    /// Reads the next available message, if any.
    /// </summary>
    bool TryRead(out TransportMessage message);

    /// <summary>
    /// Number of queued messages waiting to be consumed.
    /// </summary>
    int PendingCount { get; }
}
