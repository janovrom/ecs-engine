using System.Text.Json;
using EcsEngine.Core;
using EcsEngine.Replay;
using NUnit.Framework;

namespace EcsEngine.Replay.Tests;

[TestFixture]
public class SnapshotInspectorTests
{
    private static ComponentSerializerRegistry BuildRegistry()
    {
        ComponentSerializerRegistry r = new();
        r.Register<Position>(
            write: (w, p) => { w.Write(p.X); w.Write(p.Y); },
            read: reader => new Position(reader.ReadSingle(), reader.ReadSingle()));
        r.Register<Tag>(
            write: (w, t) => w.Write(t.Id),
            read: reader => new Tag(reader.ReadInt32()));
        return r;
    }

    private static byte[] CreateSnapshotBytes()
    {
        ComponentSerializerRegistry registry = BuildRegistry();
        SnapshotWriter writer = new(registry);

        EcsWorld world = new();
        EntityId e1 = world.CreateEntity();
        EntityId e2 = world.CreateEntity();

        world.QueueAddComponent(e1, new Position(1.5f, 2.5f));
        world.QueueAddComponent(e1, new Tag(7));
        world.QueueAddComponent(e2, new Tag(42));

        world.ApplySafePoint();
        world.AdvanceTick();

        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(bw, world, SnapshotMode.TickBoundary);
        bw.Flush();
        return ms.ToArray();
    }

    [Test]
    public void Inspect_DecodesConfiguredComponentSchemas()
    {
        byte[] snapshot = CreateSnapshotBytes();

        SnapshotInspector inspector = new(
        [
            new SnapshotComponentSchema(
                "EcsEngine.Replay.Tests.Position",
                [new SnapshotComponentField("X", SnapshotFieldType.Single), new SnapshotComponentField("Y", SnapshotFieldType.Single)]),
            new SnapshotComponentSchema(
                "EcsEngine.Replay.Tests.Tag",
                [new SnapshotComponentField("Id", SnapshotFieldType.Int32)]),
        ]);

        using MemoryStream ms = new(snapshot);
        using BinaryReader br = new(ms);
        SnapshotInspectionResult result = inspector.Inspect(br);

        Assert.That(result.Tick, Is.EqualTo(1));
        Assert.That(result.EntityIds, Has.Count.EqualTo(2));
        Assert.That(result.ComponentTypes, Has.Count.EqualTo(2));

        SnapshotDecodedType positionType = result.ComponentTypes.Single(t => t.TypeName == "EcsEngine.Replay.Tests.Position");
        Assert.That(positionType.Components, Has.Count.EqualTo(1));
        Assert.That(positionType.Components[0].Fields.Select(f => f.Name), Is.EqualTo(new[] { "X", "Y" }));

        SnapshotDecodedType tagType = result.ComponentTypes.Single(t => t.TypeName == "EcsEngine.Replay.Tests.Tag");
        Assert.That(tagType.Components, Has.Count.EqualTo(2));
    }

    [Test]
    public void Inspect_WithoutRequiredSchema_ThrowsInvalidDataException()
    {
        byte[] snapshot = CreateSnapshotBytes();

        SnapshotInspector inspector = new(
        [
            new SnapshotComponentSchema(
                "EcsEngine.Replay.Tests.Position",
                [new SnapshotComponentField("X", SnapshotFieldType.Single), new SnapshotComponentField("Y", SnapshotFieldType.Single)]),
        ]);

        using MemoryStream ms = new(snapshot);
        using BinaryReader br = new(ms);

        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => inspector.Inspect(br))!;
        Assert.That(ex.Message, Does.Contain("No schema registered for component type"));
    }

    [Test]
    public void ToText_ContainsSummaryAndFields()
    {
        SnapshotInspectionResult result = new(
            Version: 1,
            Mode: 0,
            Tick: 12,
            EntityIds: [3],
            ComponentTypes:
            [
                new SnapshotDecodedType(
                    "Comp",
                    [
                        new SnapshotDecodedComponent(3, [new SnapshotDecodedField("A", 10), new SnapshotDecodedField("B", 20)])
                    ])
            ]);

        string text = SnapshotInspector.ToText(result);

        Assert.That(text, Does.Contain("Version: 1"));
        Assert.That(text, Does.Contain("Tick: 12"));
        Assert.That(text, Does.Contain("Comp: 1 entries"));
        Assert.That(text, Does.Contain("A=10, B=20"));
    }

    [Test]
    public void ToJson_ProducesExpectedShape()
    {
        SnapshotInspectionResult result = new(
            Version: 1,
            Mode: 1,
            Tick: 5,
            EntityIds: [11, 12],
            ComponentTypes:
            [
                new SnapshotDecodedType(
                    "Comp",
                    [new SnapshotDecodedComponent(11, [new SnapshotDecodedField("Value", 7)])])
            ]);

        string json = SnapshotInspector.ToJson(result);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.That(root.GetProperty("version").GetInt32(), Is.EqualTo(1));
        Assert.That(root.GetProperty("mode").GetInt32(), Is.EqualTo(1));
        Assert.That(root.GetProperty("tick").GetInt32(), Is.EqualTo(5));
        Assert.That(root.GetProperty("entityIds").GetArrayLength(), Is.EqualTo(2));
        Assert.That(root.GetProperty("componentTypes").GetArrayLength(), Is.EqualTo(1));
    }
}
