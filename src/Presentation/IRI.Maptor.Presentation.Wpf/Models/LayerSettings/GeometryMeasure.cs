using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

public enum GeometryMeasureKind
{
    None,

    /// <summary>Geodesic length on the WGS84 ellipsoid, metres.</summary>
    Length,

    /// <summary>Area on the WGS84 ellipsoid (equal-area projection), square metres.</summary>
    Area,
}

/// <summary>The geodesic length or area of features, whatever SRID their geometries are in.</summary>
public static class GeometryMeasure
{
    public static GeometryMeasureKind GetKind(GeometryType? type) => type switch
    {
        GeometryType.LineString or GeometryType.MultiLineString => GeometryMeasureKind.Length,
        GeometryType.Polygon or GeometryType.MultiPolygon => GeometryMeasureKind.Area,
        _ => GeometryMeasureKind.None,
    };

    /// <summary>The layer's measure: from its declared geometry type, else from its first non-empty geometry.</summary>
    public static GeometryMeasureKind GetKind(GeometryType? declaredType, IReadOnlyList<Feature<Point>> features)
    {
        var kind = GetKind(declaredType);

        if (kind != GeometryMeasureKind.None)
            return kind;

        var first = features.FirstOrDefault(f => !f.TheGeometry.IsNullOrEmpty());

        return GetKind(first?.TheGeometry.Type);
    }

    /// <summary>
    /// The measure of every feature, aligned with <paramref name="features"/>; null where the geometry is empty, of
    /// another kind, or in an SRID that cannot be brought to WGS84. CPU-bound: call it off the UI thread.
    /// </summary>
    public static double?[] Calculate(IReadOnlyList<Feature<Point>> features, GeometryMeasureKind kind, CancellationToken cancellationToken = default)
    {
        var result = new double?[features.Count];

        if (kind == GeometryMeasureKind.None)
            return result;

        for (int i = 0; i < features.Count; i++)
        {
            if (i % 1_000 == 0)
                cancellationToken.ThrowIfCancellationRequested();

            result[i] = Calculate(features[i].TheGeometry, kind);
        }

        return result;
    }

    public static double? Calculate(Geometry<Point>? geometry, GeometryMeasureKind kind)
    {
        if (geometry is null || geometry.IsNullOrEmpty() || GetKind(geometry.Type) != kind)
            return null;

        try
        {
            var wgs84 = ToGeodeticWgs84(geometry);

            if (wgs84 is null)
                return null;

            return kind == GeometryMeasureKind.Length
                ? wgs84.GetEllipsoidalLength()
                : SpatialUtility.GetEllipsoidalArea(wgs84);
        }
        catch
        {
            // a broken geometry is left out of the totals rather than failing the whole layer
            return null;
        }
    }

    private static Geometry<Point>? ToGeodeticWgs84(Geometry<Point> geometry)
    {
        switch (geometry.Srid)
        {
            case SridHelper.GeodeticWGS84:
                return geometry;

            // layers on the map hold Web Mercator geometries; 0 is a source that did not stamp its SRID
            case 0:
            case SridHelper.WebMercator:
            case SridHelper.EsriWebMercator:
                return geometry.Transform(MapProjects.WebMercatorToGeodeticWgs84, SridHelper.GeodeticWGS84);

            default:
                var srs = SridHelper.AsSrsBase(geometry.Srid);

                return srs is null ? null : geometry.Transform(p => srs.ToWgs84Geodetic(p), SridHelper.GeodeticWGS84);
        }
    }
}
