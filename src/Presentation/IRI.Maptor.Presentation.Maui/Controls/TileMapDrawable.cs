using IRI.Maptor.Presentation.Maui.Layers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Helpers;
using IRI.Maptor.Core.Spatial.Model;

using Microsoft.Maui.Graphics;

using IImage = Microsoft.Maui.Graphics.IImage;

namespace IRI.Maptor.Presentation.Maui.Controls;

/// <summary>
/// Draws the visible tile basemap. The view state (WebMercator center + resolution) is
/// pushed in by <see cref="MapViewer"/> before each <c>Invalidate()</c>.
/// </summary>
internal sealed class TileMapDrawable : IDrawable
{
    private readonly TileImageCache _cache;

    public TileMapDrawable(TileImageCache cache)
    {
        _cache = cache;
    }

    // WebMercator coordinate at the center of the view.
    public double CenterX { get; set; }
    public double CenterY { get; set; }

    // WebMercator units per device-independent pixel.
    public double Resolution { get; set; } = 1;

    public Func<TileInfo, string>? UrlFunc { get; set; }

    // Identifies the current basemap so cached tiles of different basemaps don't collide.
    public string LayerKey { get; set; } = "default";

    // Optional marker location in WebMercator coordinates (null hides it).
    public double? MarkerX { get; set; }
    public double? MarkerY { get; set; }

    // Vector layers drawn on top of the basemap (in draw order).
    public IReadOnlyList<MapLayer>? Layers { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.WhiteSmoke;
        canvas.FillRectangle(dirtyRect);

        double width = dirtyRect.Width;
        double height = dirtyRect.Height;

        if (Resolution <= 0 || width <= 0 || height <= 0)
        {
            return;
        }

        double halfWidthWorld = width / 2.0 * Resolution;
        double halfHeightWorld = height / 2.0 * Resolution;

        double viewXMin = CenterX - halfWidthWorld;
        double viewXMax = CenterX + halfWidthWorld;
        double viewYMin = CenterY - halfHeightWorld;
        double viewYMax = CenterY + halfHeightWorld;

        var viewBox = new BoundingBox(viewXMin, viewYMin, viewXMax, viewYMax);

        int zoom = MapViewerMath.ResolutionToZoom(Resolution);

        // Basemap tile grid (skipped if no online basemap is selected).
        if (UrlFunc is { } urlFunc)
        {
            var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(viewBox, zoom);

            foreach (var tile in tiles)
            {
                var image = _cache.GetOrRequest(tile, urlFunc, LayerKey);

                if (image != null)
                {
                    DrawTile(canvas, tile.WebMercatorExtent, viewXMin, viewYMax, image);
                }
            }
        }

        DrawLayers(canvas, viewBox, viewXMin, viewYMax, zoom);

        DrawMarker(canvas, viewXMin, viewYMax);
    }

    // WebMercator Y grows upward; screen Y grows downward, so flip on Y.
    private void DrawTile(ICanvas canvas, BoundingBox extent, double viewXMin, double viewYMax, IImage image)
    {
        float left = (float)((extent.XMin - viewXMin) / Resolution);
        float top = (float)((viewYMax - extent.YMax) / Resolution);
        float size = (float)(extent.Width / Resolution);

        // +1px to avoid hairline seams between neighbouring tiles.
        canvas.DrawImage(image, left, top, size + 1, size + 1);
    }

    private void DrawRasterTileLayer(ICanvas canvas, MbTilesRasterLayer layer, BoundingBox viewBox, double viewXMin, double viewYMax, int mapZoom)
    {
        int zoom = layer.ClosestZoom(mapZoom);

        var tiles = WebMercatorUtility.WebMercatorBoundingBoxToGoogleTileRegions(viewBox, zoom);

        canvas.SaveState();
        canvas.Alpha = (float)layer.Opacity;

        foreach (var tile in tiles)
        {
            var image = _cache.GetOrRequest(tile, layer.GetTileBytes, layer.LayerKey);

            if (image != null)
            {
                DrawTile(canvas, tile.WebMercatorExtent, viewXMin, viewYMax, image);
            }
        }

        canvas.RestoreState();
    }

    private void DrawLayers(ICanvas canvas, BoundingBox viewBox, double viewXMin, double viewYMax, int mapZoom)
    {
        var layers = Layers;

        if (layers is null)
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (!layer.IsVisible)
            {
                continue;
            }

            if (layer is MbTilesRasterLayer raster)
            {
                DrawRasterTileLayer(canvas, raster, viewBox, viewXMin, viewYMax, mapZoom);
                continue;
            }

            if (layer.Parts.Count == 0)
            {
                continue;
            }

            var stroke = layer.Color;
            var fill = layer.Color.WithAlpha(0.30f);
            var strokeWidth = (float)layer.StrokeWidth;
            var pointRadius = (float)(layer.PointSize / 2.0);

            canvas.SaveState();
            canvas.Alpha = (float)layer.Opacity;

            foreach (var part in layer.Parts)
            {
                switch (part.Kind)
                {
                    case RenderKind.Point:
                        DrawPointPart(canvas, part, viewXMin, viewYMax, stroke, pointRadius);
                        break;

                    case RenderKind.Line:
                        DrawLinePart(canvas, part, viewXMin, viewYMax, stroke, strokeWidth);
                        break;

                    case RenderKind.Polygon:
                        DrawPolygonPart(canvas, part, viewXMin, viewYMax, stroke, fill, strokeWidth);
                        break;
                }
            }

            canvas.RestoreState();
        }
    }

    private void DrawPointPart(ICanvas canvas, RenderPart part, double viewXMin, double viewYMax, Color stroke, float radius)
    {
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 1;

        foreach (var (xs, ys) in part.Rings)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                float sx = (float)((xs[i] - viewXMin) / Resolution);
                float sy = (float)((viewYMax - ys[i]) / Resolution);

                canvas.FillColor = stroke;
                canvas.FillCircle(sx, sy, radius);
                canvas.DrawCircle(sx, sy, radius);
            }
        }
    }

    private void DrawLinePart(ICanvas canvas, RenderPart part, double viewXMin, double viewYMax, Color stroke, float strokeWidth)
    {
        var path = BuildPath(part, viewXMin, viewYMax, close: false);

        if (path is null)
        {
            return;
        }

        canvas.StrokeColor = stroke;
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
    }

    private void DrawPolygonPart(ICanvas canvas, RenderPart part, double viewXMin, double viewYMax, Color stroke, Color fill, float strokeWidth)
    {
        var path = BuildPath(part, viewXMin, viewYMax, close: true);

        if (path is null)
        {
            return;
        }

        canvas.FillColor = fill;
        canvas.FillPath(path, WindingMode.EvenOdd);

        canvas.StrokeColor = stroke;
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
    }

    private PathF? BuildPath(RenderPart part, double viewXMin, double viewYMax, bool close)
    {
        PathF? path = null;

        foreach (var (xs, ys) in part.Rings)
        {
            if (xs.Length == 0)
            {
                continue;
            }

            path ??= new PathF();

            path.MoveTo((float)((xs[0] - viewXMin) / Resolution), (float)((viewYMax - ys[0]) / Resolution));

            for (int i = 1; i < xs.Length; i++)
            {
                path.LineTo((float)((xs[i] - viewXMin) / Resolution), (float)((viewYMax - ys[i]) / Resolution));
            }

            if (close)
            {
                path.Close();
            }
        }

        return path;
    }

    private void DrawMarker(ICanvas canvas, double viewXMin, double viewYMax)
    {
        if (!MarkerX.HasValue || !MarkerY.HasValue)
        {
            return;
        }

        float markerX = (float)((MarkerX.Value - viewXMin) / Resolution);
        float markerY = (float)((viewYMax - MarkerY.Value) / Resolution);

        const float radius = 7f;

        canvas.FillColor = Colors.Red;
        canvas.FillCircle(markerX, markerY, radius);

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(markerX, markerY, radius);
    }
}
