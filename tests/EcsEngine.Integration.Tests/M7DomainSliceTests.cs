using EcsEngine.Simulation;
using NUnit.Framework;

namespace EcsEngine.Integration.Tests;

[TestFixture]
public class M7DomainSliceTests
{
    [Test]
    public void RaiseElevation_MovesSelectedOccupiedCellsUpByOne()
    {
        OccupancyGrid grid = new();
        GridPosition a = new(1, 1, 0);
        GridPosition b = new(2, 1, 0);
        GridPosition untouched = new(9, 9, 9);

        grid.SetOccupied(a, occupied: true);
        grid.SetOccupied(b, occupied: true);
        grid.SetOccupied(untouched, occupied: true);

        grid.RaiseElevation([a, b, new GridPosition(20, 20, 0)]);

        Assert.That(grid.IsOccupied(a), Is.False);
        Assert.That(grid.IsOccupied(b), Is.False);
        Assert.That(grid.IsOccupied(new GridPosition(1, 1, 1)), Is.True);
        Assert.That(grid.IsOccupied(new GridPosition(2, 1, 1)), Is.True);
        Assert.That(grid.IsOccupied(untouched), Is.True);
    }

    [Test]
    public void MovementRules_DiagonalStepsCostOnePointFive_ForPlanarAndVerticalDiagonals()
    {
        decimal planar = MovementRules.GetStepCost(new GridPosition(0, 0, 0), new GridPosition(1, 1, 0));
        decimal vertical = MovementRules.GetStepCost(new GridPosition(0, 0, 0), new GridPosition(1, 0, 1));

        Assert.That(planar, Is.EqualTo(1.5m));
        Assert.That(vertical, Is.EqualTo(1.5m));
    }

    [Test]
    public void MovementRules_ModifiersRespectCancelRuleAndFloorRounding()
    {
        int sprint = MovementRules.ComputeEffectiveSpeed(9, MovementModifiers.Sprint, sprintMultiplier: 1.5m, heavyTerrainMultiplier: 0.5m);
        int heavyTerrain = MovementRules.ComputeEffectiveSpeed(9, MovementModifiers.HeavyTerrain, sprintMultiplier: 1.5m, heavyTerrainMultiplier: 0.5m);
        int both = MovementRules.ComputeEffectiveSpeed(
            9,
            MovementModifiers.Sprint | MovementModifiers.HeavyTerrain,
            sprintMultiplier: 1.5m,
            heavyTerrainMultiplier: 0.5m);

        Assert.That(sprint, Is.EqualTo(13));
        Assert.That(heavyTerrain, Is.EqualTo(4));
        Assert.That(both, Is.EqualTo(9));
    }

    [Test]
    public void MovementRules_VerticalMovementUsesClimbSpeed()
    {
        MovementProfile profile = new(Speed: 30, ClimbSpeed: 10);

        int budget = MovementRules.ComputeStepBudget(
            profile,
            from: new GridPosition(0, 0, 0),
            to: new GridPosition(0, 0, 1),
            modifiers: MovementModifiers.None);

        Assert.That(budget, Is.EqualTo(10));
    }

    [Test]
    public void PathPreview_CommitStoresCommittedPathAndClearsPreview()
    {
        PathPreviewSession preview = new();
        preview.SetPreview([new GridPosition(0, 0, 0), new GridPosition(1, 0, 0)]);

        CommittedPath committed = preview.Commit();

        Assert.That(committed.Steps, Has.Count.EqualTo(2));
        Assert.That(preview.PreviewSteps, Is.Empty);
        Assert.That(preview.LastCommittedPath, Is.EqualTo(committed));
    }

        [Test]
        public void MovementPathfinder_FindsValidPath_AroundOccupiedCells()
        {
                OccupancyGrid grid = new();
                grid.SetOccupied(new GridPosition(1, 0, 0), occupied: true);

                bool ok = MovementPathfinder.TryFindPath(
                        grid,
                        start: new GridPosition(0, 0, 0),
                        target: new GridPosition(2, 0, 0),
                        profile: new MovementProfile(6, 6),
                        modifiers: MovementModifiers.None,
                        out IReadOnlyList<GridPosition> path,
                        out string? error);

                Assert.That(ok, Is.True, error);
                Assert.That(path[0], Is.EqualTo(new GridPosition(0, 0, 0)));
                Assert.That(path[^1], Is.EqualTo(new GridPosition(2, 0, 0)));
                Assert.That(path, Does.Not.Contain(new GridPosition(1, 0, 0)));
        }

        [Test]
        public void PathPreview_TryCommitValidated_RejectsOverBudgetPath()
        {
                OccupancyGrid grid = new();
                PathPreviewSession preview = new();
                preview.SetPreview([
                        new GridPosition(0, 0, 0),
                        new GridPosition(1, 0, 0),
                        new GridPosition(2, 0, 0),
                        new GridPosition(3, 0, 0)
                ]);

                bool ok = preview.TryCommitValidated(
                        grid,
                        profile: new MovementProfile(Speed: 2, ClimbSpeed: 2),
                        modifiers: MovementModifiers.None,
                        out CommittedPath? committed,
                        out string? error);

                Assert.That(ok, Is.False);
                Assert.That(committed, Is.Null);
                Assert.That(error, Does.Contain("exceeds movement budget"));
        }

        [Test]
        public void ScriptedScenario_EndToEnd_PreviewsAndCommitsMovement()
        {
                string path = Path.GetTempFileName();

                try
                {
                        File.WriteAllText(path, """
                        {
                            "operations": [
                                {
                                    "type": "setOccupied",
                                    "cell": { "x": 1, "y": 0, "z": 0 },
                                    "occupied": true
                                },
                                {
                                    "type": "setOccupied",
                                    "cell": { "x": 2, "y": 0, "z": 0 },
                                    "occupied": true
                                },
                                {
                                    "type": "previewPath",
                                    "start": { "x": 0, "y": 0, "z": 0 },
                                    "target": { "x": 3, "y": 0, "z": 0 },
                                    "profile": { "speed": 8, "climbSpeed": 8 },
                                    "modifiers": 0
                                },
                                {
                                    "type": "commitPreviewPath",
                                    "profile": { "speed": 8, "climbSpeed": 8 },
                                    "modifiers": 0
                                }
                            ]
                        }
                        """);

                        SimulationScript script = SimulationScriptLoader.LoadFromFile(path);
                        SimulationState state = new();

                        script.Apply(state);

                        Assert.That(state.PathPreview.LastCommittedPath, Is.Not.Null);
                        Assert.That(state.PathPreview.LastCommittedCost, Is.Not.Null);
                        Assert.That(state.LastCommittedPosition, Is.EqualTo(new GridPosition(3, 0, 0)));
                        Assert.That(state.PathPreview.PreviewSteps, Is.Empty);
                }
                finally
                {
                        File.Delete(path);
                }
        }

    [Test]
    public void ScriptedOperations_LoadFromJsonAndApplyToSimulationState()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, """
            {
              "operations": [
                {
                  "type": "setOccupied",
                  "cell": { "x": 3, "y": 4, "z": 0 },
                  "occupied": true
                },
                {
                  "type": "raiseElevation",
                  "cells": [
                    { "x": 3, "y": 4, "z": 0 }
                  ]
                }
              ]
            }
            """);

            SimulationScript script = SimulationScriptLoader.LoadFromFile(path);
            SimulationState state = new();

            script.Apply(state);

            Assert.That(state.Grid.IsOccupied(new GridPosition(3, 4, 0)), Is.False);
            Assert.That(state.Grid.IsOccupied(new GridPosition(3, 4, 1)), Is.True);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
