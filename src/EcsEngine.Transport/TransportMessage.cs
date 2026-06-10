namespace EcsEngine.Transport;

/// <summary>
/// Message envelope used by the in-memory transport.
/// </summary>
public readonly record struct TransportMessage(int Sequence, string Topic, byte[] Payload);
