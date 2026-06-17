using System.Text.Json.Serialization;

namespace EcsEngine.Simulation;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SetOccupiedOperation), typeDiscriminator: "setOccupied")]
[JsonDerivedType(typeof(RaiseElevationOperation), typeDiscriminator: "raiseElevation")]
[JsonDerivedType(typeof(PreviewPathOperation), typeDiscriminator: "previewPath")]
[JsonDerivedType(typeof(CommitPreviewPathOperation), typeDiscriminator: "commitPreviewPath")]
public abstract record ScriptedOperationBase
{
    public abstract void Apply(SimulationState state);
}

public sealed record SetOccupiedOperation(GridPosition Cell, bool Occupied) : ScriptedOperationBase
{
    public override void Apply(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Grid.SetOccupied(Cell, Occupied);
    }
}

public sealed record RaiseElevationOperation(IReadOnlyList<GridPosition> Cells) : ScriptedOperationBase
{
    public override void Apply(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Grid.RaiseElevation(Cells);
    }
}

public sealed record PreviewPathOperation(
    GridPosition Start,
    GridPosition Target,
    MovementProfile Profile,
    MovementModifiers Modifiers) : ScriptedOperationBase
{
    public override void Apply(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.PathPreview.TryBuildPreview(state.Grid, Start, Target, Profile, Modifiers, out string? error))
            throw new InvalidOperationException(error ?? "Failed to build preview path.");
    }
}

public sealed record CommitPreviewPathOperation(
    MovementProfile Profile,
    MovementModifiers Modifiers) : ScriptedOperationBase
{
    public override void Apply(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.PathPreview.TryCommitValidated(state.Grid, Profile, Modifiers, out CommittedPath? committed, out string? error))
            throw new InvalidOperationException(error ?? "Failed to commit preview path.");

        state.ApplyCommittedPath(committed!);
    }
}
