using EcsEngine.Simulation;
using SkiaSharp;

namespace EcsEngine.Rendering;

public sealed class SkiaOccupancyGridRenderer : IOccupancyGridRenderer
{
    private readonly OccupancyRenderOptions _Options;

    public SkiaOccupancyGridRenderer(OccupancyRenderOptions? options = null)
    {
        _Options = options ?? new OccupancyRenderOptions();
    }

    public byte[] RenderPng(OccupancyGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        using SKBitmap bitmap = new(_Options.CanvasWidth, _Options.CanvasHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(ToColor(_Options.BackgroundArgb));

        using SKPaint topPaint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = ToColor(_Options.OccupiedTopArgb),
        };
        using SKPaint sidePaint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = ToColor(_Options.OccupiedSideArgb),
        };
        using SKPaint linePaint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = ToColor(_Options.GridLineArgb),
        };

        IReadOnlyList<GridPosition> ordered = [
            .. grid.OccupiedCells
                .OrderBy(static p => p.X + p.Y)
                .ThenBy(static p => p.Z)
                .ThenBy(static p => p.X)
                .ThenBy(static p => p.Y)
        ];

        foreach (GridPosition cell in ordered)
            DrawCell(canvas, cell, topPaint, sidePaint, linePaint);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return data.ToArray();
    }

    private void DrawCell(SKCanvas canvas, in GridPosition cell, SKPaint topPaint, SKPaint sidePaint, SKPaint linePaint)
    {
        SKPoint center = Project(cell);
        float halfW = _Options.TileWidth / 2f;
        float halfH = _Options.TileHeight / 2f;
        float sideDepth = _Options.ElevationStep;

        SKPoint top = new(center.X, center.Y - halfH);
        SKPoint right = new(center.X + halfW, center.Y);
        SKPoint bottom = new(center.X, center.Y + halfH);
        SKPoint left = new(center.X - halfW, center.Y);

        SKPoint rightBottom = new(right.X, right.Y + sideDepth);
        SKPoint bottomBottom = new(bottom.X, bottom.Y + sideDepth);
        SKPoint leftBottom = new(left.X, left.Y + sideDepth);

        using SKPath rightSide = new();
        rightSide.MoveTo(right);
        rightSide.LineTo(rightBottom);
        rightSide.LineTo(bottomBottom);
        rightSide.LineTo(bottom);
        rightSide.Close();
        canvas.DrawPath(rightSide, sidePaint);

        using SKPath leftSide = new();
        leftSide.MoveTo(bottom);
        leftSide.LineTo(bottomBottom);
        leftSide.LineTo(leftBottom);
        leftSide.LineTo(left);
        leftSide.Close();
        canvas.DrawPath(leftSide, sidePaint);

        using SKPath topFace = new();
        topFace.MoveTo(top);
        topFace.LineTo(right);
        topFace.LineTo(bottom);
        topFace.LineTo(left);
        topFace.Close();
        canvas.DrawPath(topFace, topPaint);
        canvas.DrawPath(topFace, linePaint);
    }

    private SKPoint Project(in GridPosition p)
    {
        float cx = _Options.CanvasWidth / 2f;
        float cy = _Options.CanvasHeight / 3f;

        float x = cx + (p.X - p.Y) * (_Options.TileWidth / 2f);
        float y = cy + (p.X + p.Y) * (_Options.TileHeight / 2f) - (p.Z * _Options.ElevationStep);
        return new SKPoint(x, y);
    }

    private static SKColor ToColor(uint argb)
    {
        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8) & 0xFF);
        byte b = (byte)(argb & 0xFF);
        return new SKColor(r, g, b, a);
    }
}
