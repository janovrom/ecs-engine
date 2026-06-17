namespace EcsEngine.Simulation;

public static class MovementRules
{
    private const decimal OrthogonalStepCost = 1.0m;
    private const decimal DiagonalStepCost = 1.5m;

    internal static int GetStepCostUnits(in GridPosition from, in GridPosition to)
    {
        decimal cost = GetStepCost(from, to);
        return cost == OrthogonalStepCost ? 2 : 3;
    }

    public static decimal GetStepCost(in GridPosition from, in GridPosition to)
    {
        int dx = Math.Abs(to.X - from.X);
        int dy = Math.Abs(to.Y - from.Y);
        int dz = Math.Abs(to.Z - from.Z);

        if (dx > 1 || dy > 1 || dz > 1 || (dx == 0 && dy == 0 && dz == 0))
            throw new ArgumentOutOfRangeException(nameof(to), "Step must move exactly one adjacent cell.");

        bool isDiagonal = dx + dy + dz >= 2;
        return isDiagonal ? DiagonalStepCost : OrthogonalStepCost;
    }

    public static int ComputeEffectiveSpeed(
        int baseSpeed,
        MovementModifiers modifiers,
        decimal sprintMultiplier = 1.5m,
        decimal heavyTerrainMultiplier = 0.5m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseSpeed);

        decimal multiplier = ComputeMultiplier(modifiers, sprintMultiplier, heavyTerrainMultiplier);
        return (int)Math.Floor(baseSpeed * multiplier);
    }

    public static int ComputeStepBudget(
        in MovementProfile profile,
        in GridPosition from,
        in GridPosition to,
        MovementModifiers modifiers,
        decimal sprintMultiplier = 1.5m,
        decimal heavyTerrainMultiplier = 0.5m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(profile.Speed);
        ArgumentOutOfRangeException.ThrowIfNegative(profile.ClimbSpeed);

        bool usesClimbSpeed = from.Z != to.Z;
        int baseSpeed = usesClimbSpeed ? profile.ClimbSpeed : profile.Speed;
        return ComputeEffectiveSpeed(baseSpeed, modifiers, sprintMultiplier, heavyTerrainMultiplier);
    }

    private static decimal ComputeMultiplier(
        MovementModifiers modifiers,
        decimal sprintMultiplier,
        decimal heavyTerrainMultiplier)
    {
        bool hasSprint = modifiers.HasFlag(MovementModifiers.Sprint);
        bool hasHeavyTerrain = modifiers.HasFlag(MovementModifiers.HeavyTerrain);

        if (hasSprint && hasHeavyTerrain)
            return 1.0m;

        if (hasSprint)
            return sprintMultiplier;

        if (hasHeavyTerrain)
            return heavyTerrainMultiplier;

        return 1.0m;
    }
}
