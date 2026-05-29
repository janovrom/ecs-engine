namespace EcsEngine.Core;

public delegate void QueryCallback<T1>(
    EntityId entity,
    in T1 c1)
    where T1 : struct, IEcsComponent;

public delegate void QueryCallback<T1, T2>(
    EntityId entity,
    in T1 c1,
    in T2 c2)
    where T1 : struct, IEcsComponent
    where T2 : struct, IEcsComponent;

public delegate void QueryCallback<T1, T2, T3>(
    EntityId entity,
    in T1 c1,
    in T2 c2,
    in T3 c3)
    where T1 : struct, IEcsComponent
    where T2 : struct, IEcsComponent
    where T3 : struct, IEcsComponent;
