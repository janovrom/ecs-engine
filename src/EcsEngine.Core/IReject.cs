namespace EcsEngine.Core;

/// <summary>
/// Declares that a system rejects entities that have component T. Filter only — no parameter generated.
/// </summary>
public interface IReject<T> where T : struct, IEcsComponent { }
