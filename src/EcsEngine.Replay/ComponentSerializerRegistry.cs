using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EcsEngine.Core;

namespace EcsEngine.Replay;

/// <summary>
/// Maps component types to their binary serializers. Serializers are iterated in
/// deterministic order (by <c>Type.FullName</c> ascending) to ensure consistent
/// snapshot layout across runs (D-057).
/// </summary>
public sealed class ComponentSerializerRegistry
{
    private readonly SortedList<string, IComponentSerializer> _Serializers = new(StringComparer.Ordinal);

    /// <summary>All registered serializers in deterministic type-name order.</summary>
    public IReadOnlyList<IComponentSerializer> Serializers => [.. _Serializers.Values];

    /// <summary>
    /// Registers binary read and write delegates for component type <typeparamref name="T"/>.
    /// </summary>
    public void Register<T>(Action<BinaryWriter, T> write, Func<BinaryReader, T> read)
        where T : struct, IEcsComponent
    {
        ComponentSerializer<T> serializer = new(write, read);
        _Serializers[serializer.TypeName] = serializer;
    }

    /// <summary>
    /// Tries to get the serializer registered for the given type name.
    /// Returns false if no serializer has been registered for that name.
    /// </summary>
    public bool TryGetSerializer(string typeName, [NotNullWhen(true)] out IComponentSerializer? serializer)
    {
        bool found = _Serializers.TryGetValue(typeName, out serializer);
        Debug.Assert(!found || serializer is not null);
        return found;
    }
}
