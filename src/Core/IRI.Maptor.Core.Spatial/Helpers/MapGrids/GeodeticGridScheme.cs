using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// The graticule: meridians and parallels.
/// </summary>
/// <remarks>
/// The simplest of the three schemes, and the only one that needs no sampling. Web Mercator maps
/// every meridian and every parallel to a straight line, so each is exactly two vertices — and
/// because the values are read straight off the view's own degrees, there is no plane to walk and
/// no clipping to do.
/// </remarks>
internal static class GeodeticGridScheme
{
    internal static MapGrid Create(BoundingBox geodeticView, MapGridDefinition definition, MapGridOptions options)
    {
        var ladder = MapGridLadders.Degrees;

        var span = Math.Max(geodeticView.Width, geodeticView.Height);

        var major = definition.MajorInterval ?? MapGridLadders.ChooseMajor(span, ladder, options.MinMajorLines, options.MaxMajorLines);

        var minor = options.ShowMinorLines ? MapGridLadders.MinorOf(major, ladder) : null;

        var lines = new List<MapGridLine>();

        var placer = new MapGridLabelPlacer(geodeticView, definition, options);

        AddLines(MapGridAxis.X, MapGridLineKind.Major, major, major, geodeticView, options, lines, placer);
        AddLines(MapGridAxis.Y, MapGridLineKind.Major, major, major, geodeticView, options, lines, placer);

        if (minor.HasValue)
        {
            AddLines(MapGridAxis.X, MapGridLineKind.Minor, minor.Value, major, geodeticView, options, lines, placer);
            AddLines(MapGridAxis.Y, MapGridLineKind.Minor, minor.Value, major, geodeticView, options, lines, placer);
        }

        return new MapGrid(definition, major, minor, lines, placer.Labels);
    }

    private static void AddLines(
        MapGridAxis axis,
        MapGridLineKind kind,
        double step,
        double major,
        BoundingBox view,
        MapGridOptions options,
        List<MapGridLine> lines,
        MapGridLabelPlacer placer)
    {
        var from = axis == MapGridAxis.X ? view.XMin : view.YMin;
        var to = axis == MapGridAxis.X ? view.XMax : view.YMax;

        var first = Math.Ceiling(from / step) * step;

        var count = (int)Math.Floor((to - first) / step) + 1;

        if (count <= 0 || count > options.MaxLines * 4)
            return;

        var isMeridian = axis == MapGridAxis.X;

        for (var i = 0; i < count; i++)
        {
            if (lines.Count >= options.MaxLines)
                return;

            var value = first + i * step;

            if (kind == MapGridLineKind.Minor && MapGridPlaneWalker.IsMultipleOf(value, major))
                continue;

            var geodetic = isMeridian
                ? new List<Point> { new Point(value, view.YMin), new Point(value, view.YMax) }
                : new List<Point> { new Point(view.XMin, value), new Point(view.XMax, value) };

            lines.Add(new MapGridLine(axis, kind, value, MapGridGeometry.ToWebMercator(geodetic)));

            placer.Place(
                new List<List<Point>> { geodetic },
                axis,
                kind,
                value,
                zone: null,
                MapGridLabelFormatter.GetGeodeticHighPart(value, major),
                full => MapGridLabelFormatter.FormatGeodetic(value, isLatitude: !isMeridian, major, full),
                groupKey: string.Empty);
        }
    }
}
