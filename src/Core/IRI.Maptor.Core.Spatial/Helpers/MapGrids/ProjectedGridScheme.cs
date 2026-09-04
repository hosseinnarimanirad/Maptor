using System;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Helpers.MapGrids;

/// <summary>
/// A grid of constant x and y in one projection, over the whole view — Web Mercator, the NIOC
/// Lambert grid, a custom transverse Mercator.
/// </summary>
/// <remarks>
/// Unlike UTM there are no strips and no seams: the projection covers the view in one plane, so
/// the walk runs once. Whether the resulting lines are straight or curved on the map is entirely
/// the projection's business — Web Mercator's come out straight, Lambert's bow — and the scheme
/// neither knows nor needs to.
/// </remarks>
internal static class ProjectedGridScheme
{
    internal static MapGrid Create(BoundingBox geodeticView, MapGridDefinition definition, MapGridOptions options)
    {
        var srs = definition.Srs;

        if (srs is null)
            return MapGrid.Empty(definition);

        Point Forward(Point geodetic) => srs.FromWgs84Geodetic(geodetic);

        Point Inverse(Point plane) => srs.ToWgs84Geodetic(plane);

        var planeBounds = MapGridGeometry.PlaneBounds(Forward, geodeticView, options.SamplesPerViewEdge);

        if (planeBounds.IsNaN())
            return MapGrid.Empty(definition);

        var span = Math.Max(planeBounds.Width, planeBounds.Height);

        var major = definition.MajorInterval ?? MapGridLadders.ChooseMajor(span, MapGridLadders.Metres, options.MinMajorLines, options.MaxMajorLines);

        var minor = options.ShowMinorLines ? MapGridLadders.MinorOf(major, MapGridLadders.Metres) : null;

        var lines = new List<MapGridLine>();

        var placer = new MapGridLabelPlacer(geodeticView, definition, options);

        MapGridPlaneWalker.Walk(geodeticView, planeBounds, Inverse, major, minor, zone: null, groupKey: string.Empty, options, lines, placer);

        return new MapGrid(definition, major, minor, lines, placer.Labels);
    }
}
