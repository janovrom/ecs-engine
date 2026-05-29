namespace EcsEngine.Core;

/// <summary>
/// Declares that a system outputs (replaces) component T. Source gen maps this to an <c>out T</c> parameter.
/// </summary>
public interface IOutput<T> where T : struct, IEcsComponent { }
