namespace EcsEngine.Rendering;

public sealed record OccupancyRenderOptions(
    int CanvasWidth = 1024,
    int CanvasHeight = 768,
    float TileWidth = 44f,
    float TileHeight = 22f,
    float ElevationStep = 20f,
    uint BackgroundArgb = 0xFFF2F0E9,
    uint OccupiedTopArgb = 0xFF2E6F95,
    uint OccupiedSideArgb = 0xFF1C4762,
    uint GridLineArgb = 0xFFC7C3B8);
