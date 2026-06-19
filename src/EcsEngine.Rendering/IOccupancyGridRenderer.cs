using EcsEngine.Simulation;

namespace EcsEngine.Rendering;

public interface IOccupancyGridRenderer
{
    byte[] RenderPng(OccupancyGrid grid);
}
