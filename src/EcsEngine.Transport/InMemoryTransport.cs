using System.Collections.Concurrent;
using System.Threading;

namespace EcsEngine.Transport;

/// <summary>
/// Thread-safe in-memory transport for local/server process integration tests.
/// </summary>
public sealed class InMemoryTransport : ITransport
{
    private readonly ConcurrentQueue<TransportMessage> _Queue = new();
    private int _sequence;

    public int PendingCount => _Queue.Count;

    public void Publish(string topic, ReadOnlySpan<byte> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        // Copy to keep message payload immutable after publish.
        byte[] copy = payload.ToArray();
        int sequence = Interlocked.Increment(ref _sequence);
        _Queue.Enqueue(new TransportMessage(sequence, topic, copy));
    }

    public bool TryRead(out TransportMessage message) => _Queue.TryDequeue(out message);
}
