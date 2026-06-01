using System.ComponentModel;

namespace EcsEngine.Core;

/// <summary>
/// Provides a stable, cross-process deterministic hash for a component struct.
/// Use <see cref="Default"/> to obtain the instance registered by the source generator.
/// Call <see cref="SetDefault"/> from generated <c>[ModuleInitializer]</c> code only.
/// </summary>
public abstract class DeterministicHasher<T>
    where T : struct, IEcsComponent
{
    private static DeterministicHasher<T>? _Default;

    /// <summary>
    /// Returns the registered deterministic hasher for <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no hasher has been registered (source generator not wired up).
    /// </exception>
    public static DeterministicHasher<T> Default =>
        _Default ?? throw new InvalidOperationException(
            $"No DeterministicHasher<T> has been registered for {typeof(T).FullName}. " +
            "Ensure EcsEngine.SourceGen is referenced as an Analyzer in this assembly, " +
            "or call DeterministicHasher<T>.SetDefault(...) manually.");

    /// <summary>
    /// Registers the default hasher for <typeparamref name="T"/>.
    /// Called automatically by source-generated <c>[ModuleInitializer]</c> code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SetDefault(DeterministicHasher<T> instance) =>
        _Default = instance;

    /// <summary>
    /// Computes a stable 32-bit hash of <paramref name="value"/>.
    /// </summary>
    public abstract uint Hash(in T value);
}
