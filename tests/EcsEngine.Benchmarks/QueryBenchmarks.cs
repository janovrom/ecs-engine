using BenchmarkDotNet.Attributes;
using EcsEngine.Core;

namespace EcsEngine.Benchmarks;

[MemoryDiagnoser]
public class QueryBenchmarks
{
    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    private EcsWorld _World = null!;

    [GlobalSetup]
    public void Setup()
    {
        _World = new EcsWorld();
        for (int i = 0; i < EntityCount; i++)
        {
            EntityId entity = _World.CreateEntity();
            _World.QueueAddComponent(entity, new BenchPosition(i, i, i));
            _World.QueueAddComponent(entity, new BenchVelocity(1f, 0f, 0f));
        }
        _World.ApplySafePoint();
        _World.PreloadQuery<BenchPosition, BenchVelocity>();
    }

    [Benchmark]
    public void QueryEach_PositionVelocity()
    {
        _World.QueryEach<BenchPosition, BenchVelocity>(
            static (EntityId _, in BenchPosition _, in BenchVelocity _) => { });
    }

    [ArchetypeStorage]
    public readonly record struct BenchPosition(float X, float Y, float Z) : IEcsComponent;

    [ArchetypeStorage]
    public readonly record struct BenchVelocity(float X, float Y, float Z) : IEcsComponent;
}
