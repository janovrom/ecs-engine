using System.Text;
using System.Text.Json;
using EcsEngine.Core;
using EcsEngine.Core.Scheduling;
using EcsEngine.Rendering;
using EcsEngine.Replay;
using EcsEngine.Simulation;
using NUnit.Framework;

namespace EcsEngine.Integration.Tests;

[TestFixture]
public class M8RenderingAndToolingTests
{
    [Test]
    public void Rendering_OccupancyGrid_ProducesDeterministicNonEmptyPng()
    {
        OccupancyGrid grid = new();
        grid.SetOccupied(new GridPosition(0, 0, 0), occupied: true);
        grid.SetOccupied(new GridPosition(1, 0, 0), occupied: true);
        grid.SetOccupied(new GridPosition(0, 1, 1), occupied: true);

        IOccupancyGridRenderer renderer = new SkiaOccupancyGridRenderer(
            new OccupancyRenderOptions(CanvasWidth: 320, CanvasHeight: 240));

        byte[] first = renderer.RenderPng(grid);
        byte[] second = renderer.RenderPng(grid);
        byte[] empty = renderer.RenderPng(new OccupancyGrid());

        Assert.That(first.Length, Is.GreaterThan(64));
        Assert.That(first[0], Is.EqualTo(0x89));
        Assert.That(first[1], Is.EqualTo((byte)'P'));
        Assert.That(first[2], Is.EqualTo((byte)'N'));
        Assert.That(first[3], Is.EqualTo((byte)'G'));
        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.Not.EqualTo(empty));
    }

    [Test]
    public void SnapshotInspector_ProducesTextAndJsonOutputs()
    {
        ComponentSerializerRegistry serializers = new();
        serializers.Register<RenderTag>(
            write: (writer, value) => writer.Write(value.Id),
            read: reader => new RenderTag(reader.ReadInt32()));

        SnapshotWriter snapshotWriter = new(serializers);
        EcsWorld world = new();
        EntityId e = world.CreateEntity();
        world.QueueAddComponent(e, new RenderTag(42));
        world.ApplySafePoint();

        byte[] snapshotBytes;
        using (MemoryStream ms = new())
        {
            using BinaryWriter bw = new(ms, Encoding.UTF8, leaveOpen: true);
            snapshotWriter.Write(bw, world, SnapshotMode.TickBoundary);
            bw.Flush();
            snapshotBytes = ms.ToArray();
        }

        SnapshotInspector inspector = new(
        [
            new SnapshotComponentSchema(
                typeof(RenderTag).FullName!,
                [new SnapshotComponentField("Id", SnapshotFieldType.Int32)]),
        ]);

        SnapshotInspectionResult result;
        using (MemoryStream ms = new(snapshotBytes))
        using (BinaryReader br = new(ms, Encoding.UTF8, leaveOpen: false))
            result = inspector.Inspect(br);

        string text = SnapshotInspector.ToText(result);
        string json = SnapshotInspector.ToJson(result);

        Assert.That(result.EntityIds, Has.Count.EqualTo(1));
        Assert.That(result.ComponentTypes, Has.Count.EqualTo(1));
        Assert.That(text, Does.Contain("Snapshot Inspector Report"));
        Assert.That(text, Does.Contain("Id=42"));
        Assert.That(json, Does.Contain("\"componentTypes\""));

        using JsonDocument parsed = JsonDocument.Parse(json);
        string typeName = parsed.RootElement
            .GetProperty("componentTypes")[0]
            .GetProperty("typeName")
            .GetString()!;

        Assert.That(typeName, Is.EqualTo(typeof(RenderTag).FullName));
    }

    [Test]
    public void ScheduleVisualization_ExportsDeterministicDot()
    {
        SystemExecutor executor = new SystemScheduler()
            .Register(new RenderPrepSystem())
            .Register(new RenderCompositeSystem())
            .Build();

        string dot = executor.ExportDependencyGraphDot();

        Assert.That(dot, Does.StartWith("digraph SystemSchedule {"));
        Assert.That(dot, Does.Contain($"\"{typeof(RenderPrepSystem).FullName}\" -> \"{typeof(RenderCompositeSystem).FullName}\";"));
    }

    private readonly record struct RenderTag(int Id) : IEcsComponent;

    private sealed class RenderPrepSystem : IEcsSystem
    {
        public static void Configure(ISystemBuilder builder) => builder.WriteComponent<RenderTag>();
        public void Execute(EcsWorld world) { }
    }

    private sealed class RenderCompositeSystem : IEcsSystem
    {
        public static void Configure(ISystemBuilder builder) => builder.After<RenderPrepSystem>();
        public void Execute(EcsWorld world) { }
    }
}
