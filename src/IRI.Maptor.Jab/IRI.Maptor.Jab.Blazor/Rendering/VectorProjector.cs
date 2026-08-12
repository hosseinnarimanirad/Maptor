using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Blazor.Toc;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Jab.Blazor.Rendering;

/// <summary>
/// Turns vector layers into screen-space draw commands for the canvas.
///
/// Everything expensive is decided here rather than in JS: which layers draw at all, which
/// features are even near the viewport, and how many vertices survive. The canvas module stays a
/// dumb painter, which is what keeps the per-frame interop to one call.
/// </summary>
public static class VectorProjector
{
    /// <summary>
    /// Vertices closer together than this on screen add nothing a viewer can see, so consecutive
    /// near-duplicates are dropped. At country-wide zooms a transmission line digitised at metre
    /// precision collapses from thousands of vertices to a handful, which is the difference
    /// between a responsive pan and a stuttering one.
    /// </summary>
    private const double MinimumVertexSpacingPixels = 0.75;

    private const double HighlightHaloWidthPixels = 10;
    private const double HighlightCoreWidthPixels = 2.5;
    private const double HighlightPointRadiusPixels = 6;

    public static List<VectorDrawCommand> Project(IEnumerable<MapVectorLayer> layers, MapViewport viewport)
    {
        var commands = new List<VectorDrawCommand>();

        var visibleExtent = viewport.GetVisibleWebMercatorExtent();

        foreach (var layer in layers.Where(l => l.IsDrawnAt(viewport.ZoomLevel)).OrderBy(l => l.DrawOrder))
        {
            var features = layer.Features?.Features;

            if (features is null || features.Count == 0)
                continue;

            var category = layer.Features!.GeometryType.GetCategory();

            var paths = new List<double[]>();

            // Cached, computed once per fetch — see MapVectorLayer.FeatureExtents. Recomputing a
            // bounding box here meant walking (and allocating a flat list of) every vertex in the
            // layer on every frame, just to decide which features to skip.
            var extents = layer.FeatureExtents;

            for (var i = 0; i < features.Count; i++)
            {
                // Cull before projecting: a national layer is mostly off-screen at city zooms,
                // and projecting what will never be painted is the single easiest waste to avoid.
                if (!Intersects(extents[i], visibleExtent))
                    continue;

                var geometry = features[i].TheGeometry;

                if (geometry is null)
                    continue;

                AppendPaths(geometry, viewport, paths);
            }

            if (paths.Count == 0)
                continue;

            commands.Add(new VectorDrawCommand
            {
                Key = layer.Id,
                Kind = category switch
                {
                    GeometryCategory.Point => "point",
                    GeometryCategory.Polyline => "polyline",
                    GeometryCategory.Polygon => "polygon",
                    _ => "polyline",
                },
                Fill = HexColor.ToCss(layer.FillColor, "transparent"),
                Stroke = HexColor.ToCss(layer.StrokeColor, "transparent"),
                LineWidth = Math.Clamp(layer.StrokeThickness <= 0 ? 1 : layer.StrokeThickness, 0.5, 12),
                Alpha = Math.Clamp(layer.Opacity, 0, 1),
                Paths = paths,
            });
        }

        return commands;
    }

    /// <summary>
    /// Turns a selection into draw commands, to be appended AFTER the layer commands so it paints
    /// on top of everything.
    ///
    /// <para>Each geometry category gets two passes — a wide translucent halo, then a solid core
    /// stroke over it. That is what makes a selected feature findable regardless of what it sits
    /// on: a single stroke in any one colour disappears against a layer that happens to use a
    /// similar one, and against the basemap at low zoom.</para>
    /// </summary>
    public static List<VectorDrawCommand> ProjectHighlight(MapHighlight highlight, MapViewport viewport)
    {
        var pointPaths = new List<double[]>();
        var linePaths = new List<double[]>();
        var polygonPaths = new List<double[]>();

        foreach (var geometry in highlight.Geometries)
        {
            if (geometry is null || geometry.IsNullOrEmpty())
                continue;

            // Not culled against the viewport: a highlight is normally the thing the user just
            // asked to look at, and the set is a handful of features rather than a whole layer.
            var target = geometry.Type.GetCategory() switch
            {
                GeometryCategory.Point => pointPaths,
                GeometryCategory.Polygon => polygonPaths,
                _ => linePaths,
            };

            AppendPaths(geometry, viewport, target);
        }

        var color = HexColor.ToCss(highlight.Color, HexColor.ToCss(MapHighlight.DefaultColor));

        var commands = new List<VectorDrawCommand>(6);

        AddHighlightCommands(commands, "polygon", polygonPaths, color);
        AddHighlightCommands(commands, "polyline", linePaths, color);
        AddHighlightCommands(commands, "point", pointPaths, color);

        return commands;
    }

    private static void AddHighlightCommands(List<VectorDrawCommand> commands, string kind, List<double[]> paths, string color)
    {
        if (paths.Count == 0)
            return;

        commands.Add(new VectorDrawCommand
        {
            Key = $"__highlight_halo_{kind}",
            Kind = kind,
            Stroke = color,
            Fill = "transparent",
            LineWidth = HighlightHaloWidthPixels,
            PointRadius = HighlightPointRadiusPixels + 4,
            Alpha = 0.35,
            Paths = paths,
        });

        commands.Add(new VectorDrawCommand
        {
            Key = $"__highlight_core_{kind}",
            Kind = kind,
            Stroke = color,
            // Polygons keep a wash of fill so a large selected boundary reads as filled rather than
            // as an outline the user has to trace; the halo underneath supplies the emphasis.
            Fill = kind == "polygon" ? color : "transparent",
            LineWidth = HighlightCoreWidthPixels,
            PointRadius = HighlightPointRadiusPixels,
            Alpha = kind == "polygon" ? 0.45 : 1,
            Paths = paths,
        });
    }

    /// <summary>
    /// Walks the geometry tree, emitting one flat path per leaf. Polygon rings (including holes)
    /// each become their own path; the canvas relies on the non-zero fill rule to knock holes out,
    /// exactly as the WKB ring ordering intends.
    /// </summary>
    private static void AppendPaths(Geometry<Point> geometry, MapViewport viewport, List<double[]> paths)
    {
        if (geometry.Geometries is { Count: > 0 })
        {
            foreach (var part in geometry.Geometries)
                AppendPaths(part, viewport, paths);

            return;
        }

        var points = geometry.Points;

        if (points is null || points.Count == 0)
            return;

        // Exact-capacity array rather than a List<double>: the vertex count is known up front, so
        // there is no reason to pay for geometric growth plus a final ToArray copy on what is the
        // hottest allocation in the whole draw path.
        var buffer = new double[points.Count * 2];
        var count = 0;

        double lastX = double.NaN;
        double lastY = double.NaN;

        for (var i = 0; i < points.Count; i++)
        {
            var (x, y) = viewport.WebMercatorToScreen(points[i]);

            if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
                continue;

            if (!double.IsNaN(lastX)
                && Math.Abs(x - lastX) < MinimumVertexSpacingPixels
                && Math.Abs(y - lastY) < MinimumVertexSpacingPixels)
            {
                continue;
            }

            buffer[count++] = x;
            buffer[count++] = y;

            lastX = x;
            lastY = y;
        }

        // A single surviving vertex still matters for a point layer, but a line or ring needs two.
        if (count < 2)
            return;

        paths.Add(count == buffer.Length ? buffer : buffer[..count]);
    }

    /// <summary>
    /// Bounding-box overlap test. Written here rather than taken from BoundingBox because a NaN
    /// extent (an empty feature set, or a geometry that failed to decode) must read as "no
    /// overlap" instead of throwing or, worse, silently passing.
    /// </summary>
    private static bool Intersects(BoundingBox a, BoundingBox b)
    {
        if (double.IsNaN(a.XMin) || double.IsNaN(b.XMin))
            return false;

        return a.XMin <= b.XMax && a.XMax >= b.XMin && a.YMin <= b.YMax && a.YMax >= b.YMin;
    }
}
