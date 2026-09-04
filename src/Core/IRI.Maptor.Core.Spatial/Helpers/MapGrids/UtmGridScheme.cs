using System;
using System.Collections.Generic;
using System.Globalization;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// The UTM grid of whichever zones are in view.
/// </summary>
/// <remarks>
/// <para>
/// UTM is not one plane but sixty, and the grid genuinely restarts at every zone boundary: an
/// easting of 500 000 means the central meridian of <em>its own</em> zone. So each strip in view is
/// walked in its own plane and its lines are cut at the boundary, and the meridian where that
/// happens is drawn as a seam so the discontinuity is visible rather than mysterious. North and
/// south of the equator are walked apart too — they are different origins, the southern one
/// carrying a false northing of 10 000 000 m.
/// </para>
/// <para>
/// The interval is chosen once, from the ground span of the whole view, and then used in every
/// strip. Choosing it per strip would let two halves of the same screen disagree about how fine
/// the grid is.
/// </para>
/// <para>
/// The nominal 6° strips are used, <em>not</em> the widened Norway and Svalbard cells. Those
/// exceptions change which zone a position is reported in, which is an MGRS concern; a plain UTM
/// grid is the regular one.
/// </para>
/// </remarks>
internal static class UtmGridScheme
{
    /// <summary>The westmost longitude of a zone's nominal strip.</summary>
    private static double GetStripWest(int zone) => 6.0 * zone - 186.0;

    /// <summary>The eastmost longitude of a zone's nominal strip.</summary>
    private static double GetStripEast(int zone) => 6.0 * zone - 180.0;

    internal static MapGrid Create(BoundingBox geodeticView, MapGridDefinition definition, MapGridOptions options)
    {
        var ellipsoid = Ellipsoids.WGS84;

        // The ground span rather than a projected one: transverse Mercator diverges badly more than
        // a few degrees off its central meridian, so measuring a wide view inside one zone's plane
        // would hand the ladder a meaningless number.
        var span = MapGridGeometry.GroundSpanInMetres(geodeticView);

        var major = definition.MajorInterval ?? MapGridLadders.ChooseMajor(span, MapGridLadders.Metres, options.MinMajorLines, options.MaxMajorLines);

        var minor = options.ShowMinorLines ? MapGridLadders.MinorOf(major, MapGridLadders.Metres) : null;

        var lines = new List<MapGridLine>();

        var placer = new MapGridLabelPlacer(geodeticView, definition, options);

        // Seams first, so that where the margin is crowded it is a grid value that gives way and not
        // the caption naming the two zones — the one label on the map a reader cannot infer from the
        // others. Drawing order is unaffected: the renderer stacks by symbolizer, and the seam's is
        // last.
        if (options.ShowZoneSeams)
            AddZoneSeams(geodeticView, definition, options, lines, placer);

        var firstZone = (int)MapProjects.FindUtmZone(geodeticView.XMin);

        var lastZone = (int)MapProjects.FindUtmZone(Math.Min(geodeticView.XMax, MaxLongitude));

        for (var zone = firstZone; zone <= lastZone && lines.Count < options.MaxLines; zone++)
        {
            var west = Math.Max(geodeticView.XMin, GetStripWest(zone));
            var east = Math.Min(geodeticView.XMax, GetStripEast(zone));

            if (west >= east)
                continue;

            var centralMeridian = MapProjects.CalculateCentralMeridian(zone);

            foreach (var isNorth in new[] { true, false })
            {
                var south = isNorth ? Math.Max(geodeticView.YMin, 0.0) : geodeticView.YMin;
                var north = isNorth ? geodeticView.YMax : Math.Min(geodeticView.YMax, 0.0);

                if (south >= north)
                    continue;

                var strip = new BoundingBox(west, south, east, north);

                // Bounds are taken per strip, never over the whole view: a strip is exactly the
                // ±3° band UTM is designed for, so the projection stays well conditioned however
                // wide the view is.
                var plane = MapGridGeometry.PlaneBounds(
                    point => MapProjects.GeodeticToUTM(point, ellipsoid, zone, isNorth), strip, options.SamplesPerViewEdge);

                if (plane.IsNaN())
                    continue;

                MapGridPlaneWalker.Walk(
                    strip,
                    plane,
                    point => MapProjects.UTMToGeodetic(point, ellipsoid, centralMeridian, isNorth),
                    major,
                    minor,
                    zone,
                    zone.ToString(CultureInfo.InvariantCulture) + (isNorth ? "N" : "S"),
                    options,
                    lines,
                    placer);

                if (lines.Count >= options.MaxLines)
                    break;
            }
        }

        return new MapGrid(definition, major, minor, lines, placer.Labels);
    }

    private const double MaxLongitude = 179.999999;

    /// <summary>
    /// The meridians where the grid restarts, drawn heavier than a grid line and named for the two
    /// zones they part.
    /// </summary>
    /// <remarks>
    /// Without this the grid simply appears to jump, and a reader has no way to tell a seam from a
    /// rendering fault. A zone boundary is a meridian, so it is straight in Web Mercator and needs
    /// two vertices.
    /// </remarks>
    private static void AddZoneSeams(
        BoundingBox geodeticView,
        MapGridDefinition definition,
        MapGridOptions options,
        List<MapGridLine> lines,
        MapGridLabelPlacer placer)
    {
        var firstBoundary = (int)Math.Ceiling((geodeticView.XMin + 180.0) / 6.0);

        var lastBoundary = (int)Math.Floor((geodeticView.XMax + 180.0) / 6.0);

        for (var index = firstBoundary; index <= lastBoundary; index++)
        {
            if (lines.Count >= options.MaxLines)
                return;

            var longitude = 6.0 * index - 180.0;

            // Strictly inside: a boundary sitting exactly on the edge of the view is not visible,
            // and the ±180 ends are not seams between two zones on screen.
            if (longitude <= geodeticView.XMin || longitude >= geodeticView.XMax)
                continue;

            var westZone = index;
            var eastZone = index + 1;

            if (westZone < 1 || eastZone > 60)
                continue;

            var geodetic = new List<Point>
            {
                new Point(longitude, geodeticView.YMin),
                new Point(longitude, geodeticView.YMax),
            };

            lines.Add(new MapGridLine(MapGridAxis.X, MapGridLineKind.ZoneSeam, longitude, MapGridGeometry.ToWebMercator(geodetic), westZone));

            var text = FormattableString.Invariant($"{westZone} | {eastZone}");

            placer.Place(
                new List<List<Point>> { geodetic },
                MapGridAxis.X,
                MapGridLineKind.ZoneSeam,
                longitude,
                westZone,
                highPart: index,
                full => text,
                groupKey: "seam");
        }
    }
}
