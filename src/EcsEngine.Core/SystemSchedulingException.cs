namespace EcsEngine.Core;

/// <summary>
/// Thrown by <see cref="Scheduling.SystemScheduler.Build"/> when the system
/// dependency graph contains a cycle or an unresolved reference.
/// </summary>
public sealed class SystemSchedulingException : Exception
{
    /// <summary>The system types involved in the scheduling error.</summary>
    public IReadOnlyList<Type> InvolvedTypes { get; }

    public SystemSchedulingException(string message, IReadOnlyList<Type> involvedTypes)
        : base(message)
    {
        InvolvedTypes = involvedTypes;
    }
}
