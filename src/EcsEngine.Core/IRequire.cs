namespace EcsEngine.Core;

/// <summary>
/// Declares that a system requires component T. Source gen maps this to an <c>in T</c> parameter.
/// </summary>
public interface IRequire<T> where T : struct, IEcsComponent { }
