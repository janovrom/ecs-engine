using NUnit.Framework;
using EcsEngine.Core;
using EcsEngine.Core.Scheduling;

namespace EcsEngine.Core.Tests;

[TestFixture]
public class SystemSchedulerTests
{
    private static List<string> _Log = [];

    [SetUp]
    public void SetUp() => _Log = [];

    // --- Single system ---

    [Test]
    public void Register_SingleSystem_ExecutorContainsSystem()
    {
        SystemExecutor executor = new SystemScheduler()
            .Register(new SingleSystem())
            .Build();

        Assert.That(executor.Systems, Has.Count.EqualTo(1));
        Assert.That(executor.Systems[0], Is.InstanceOf<SingleSystem>());
    }

    [Test]
    public void Run_SingleSystem_ExecutesOnce()
    {
        EcsWorld world = new();
        new SystemScheduler()
            .Register(new SingleSystem())
            .Build()
            .Run(world);

        Assert.That(_Log, Is.EqualTo(new[] { nameof(SingleSystem) }));
    }

    // --- Ordering ---

    [Test]
    public void Register_AfterConstraint_PredecessorRunsFirst()
    {
        // SystemB declares After<SystemA>, so A must come first
        EcsWorld world = new();
        new SystemScheduler()
            .Register(new SystemB()) // registered out of order intentionally
            .Register(new SystemA())
            .Build()
            .Run(world);

        Assert.That(_Log, Is.EqualTo(new[] { nameof(SystemA), nameof(SystemB) }));
    }

    [Test]
    public void Register_BeforeConstraint_PredecessorRunsFirst()
    {
        // SystemC declares Before<SystemD>, so C must come first
        EcsWorld world = new();
        new SystemScheduler()
            .Register(new SystemD()) // registered out of order intentionally
            .Register(new SystemC())
            .Build()
            .Run(world);

        Assert.That(_Log, Is.EqualTo(new[] { nameof(SystemC), nameof(SystemD) }));
    }

    [Test]
    public void Register_TransitiveDependency_CorrectOrder()
    {
        // A → B → C (each declared After the previous)
        EcsWorld world = new();
        new SystemScheduler()
            .Register(new SystemTailC())
            .Register(new SystemTailB())
            .Register(new SystemTailA())
            .Build()
            .Run(world);

        Assert.That(_Log, Is.EqualTo(new[] { nameof(SystemTailA), nameof(SystemTailB), nameof(SystemTailC) }));
    }

    [Test]
    public void Register_NoDependencies_DeterministicOrderByTypeName()
    {
        // Alpha, Beta, Gamma — no ordering constraints — should be sorted by FullName
        SystemExecutor executor = new SystemScheduler()
            .Register(new IndepGamma())
            .Register(new IndepAlpha())
            .Register(new IndepBeta())
            .Build();

        string[] names = executor.Systems.Select(s => s.GetType().Name).ToArray();
        Assert.That(names, Is.EqualTo(new[] { nameof(IndepAlpha), nameof(IndepBeta), nameof(IndepGamma) }));
    }

    // --- Cycle detection ---

    [Test]
    public void Build_WithDirectCycle_ThrowsSystemSchedulingException()
    {
        SystemScheduler scheduler = new SystemScheduler()
            .Register(new CycleA())
            .Register(new CycleB());

        SystemSchedulingException ex = Assert.Throws<SystemSchedulingException>(
            () => scheduler.Build())!;

        Assert.That(ex.InvolvedTypes, Has.Count.EqualTo(2));
        Assert.That(ex.InvolvedTypes, Does.Contain(typeof(CycleA)));
        Assert.That(ex.InvolvedTypes, Does.Contain(typeof(CycleB)));
    }

    [Test]
    public void Build_WithSelfCycle_ThrowsSystemSchedulingException()
    {
        SystemScheduler scheduler = new SystemScheduler().Register(new SelfCycle());

        Assert.Throws<SystemSchedulingException>(() => scheduler.Build());
    }

    // --- Registration errors ---

    [Test]
    public void Register_DuplicateSystemType_ThrowsInvalidOperationException()
    {
        SystemScheduler scheduler = new SystemScheduler().Register(new SingleSystem());

        Assert.Throws<InvalidOperationException>(() => scheduler.Register(new SingleSystem()));
    }

    [Test]
    public void Build_AfterUnregisteredSystem_ThrowsSystemSchedulingException()
    {
        SystemScheduler scheduler = new SystemScheduler().Register(new AfterUnregistered());

        Assert.Throws<SystemSchedulingException>(() => scheduler.Build());
    }

    [Test]
    public void Build_EmptyScheduler_ReturnsEmptyExecutor()
    {
        SystemExecutor executor = new SystemScheduler().Build();

        Assert.That(executor.Systems, Is.Empty);
    }

    [Test]
    public void Run_MultipleCallsRunsAllEachTime()
    {
        EcsWorld world = new();
        SystemExecutor executor = new SystemScheduler()
            .Register(new SystemA())
            .Register(new SystemB())
            .Build();

        executor.Run(world);
        executor.Run(world);

        Assert.That(_Log, Is.EqualTo(new[] {
            nameof(SystemA), nameof(SystemB),
            nameof(SystemA), nameof(SystemB)
        }));
    }

    // -------------------------------------------------------------------------
    // Private test component and system types
    // -------------------------------------------------------------------------

    private readonly record struct CompA(int Value) : IEcsComponent;
    private readonly record struct CompB(int Value) : IEcsComponent;

    [EcsSystem]
    private sealed class SingleSystem : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.ReadComponent<CompA>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SingleSystem));
    }

    [EcsSystem]
    private sealed class SystemA : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.WriteComponent<CompA>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemA));
    }

    [EcsSystem]
    private sealed class SystemB : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) =>
            b.ReadComponent<CompA>().After<SystemA>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemB));
    }

    [EcsSystem]
    private sealed class SystemC : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) =>
            b.WriteComponent<CompB>().Before<SystemD>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemC));
    }

    [EcsSystem]
    private sealed class SystemD : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.ReadComponent<CompB>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemD));
    }

    [EcsSystem]
    private sealed class SystemTailA : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.WriteComponent<CompA>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemTailA));
    }

    [EcsSystem]
    private sealed class SystemTailB : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) =>
            b.ReadComponent<CompA>().WriteComponent<CompB>().After<SystemTailA>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemTailB));
    }

    [EcsSystem]
    private sealed class SystemTailC : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) =>
            b.ReadComponent<CompB>().After<SystemTailB>();
        public void Execute(EcsWorld world) => _Log.Add(nameof(SystemTailC));
    }

    // Alphabetically ordered names to test deterministic ordering
    [EcsSystem]
    private sealed class IndepAlpha : IEcsSystem
    {
        public void Execute(EcsWorld world) => _Log.Add(nameof(IndepAlpha));
    }

    [EcsSystem]
    private sealed class IndepBeta : IEcsSystem
    {
        public void Execute(EcsWorld world) => _Log.Add(nameof(IndepBeta));
    }

    [EcsSystem]
    private sealed class IndepGamma : IEcsSystem
    {
        public void Execute(EcsWorld world) => _Log.Add(nameof(IndepGamma));
    }

    // Cycle: A declares After<B>, B declares After<A>
    [EcsSystem]
    private sealed class CycleA : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.After<CycleB>();
        public void Execute(EcsWorld world) { }
    }

    [EcsSystem]
    private sealed class CycleB : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.After<CycleA>();
        public void Execute(EcsWorld world) { }
    }

    // Self-cycle: declares After itself
    [EcsSystem]
    private sealed class SelfCycle : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.After<SelfCycle>();
        public void Execute(EcsWorld world) { }
    }

    // References an unregistered system type
    [EcsSystem]
    private sealed class AfterUnregistered : IEcsSystem
    {
        public static void Configure(ISystemBuilder b) => b.After<SingleSystem>();
        public void Execute(EcsWorld world) { }
    }
}
