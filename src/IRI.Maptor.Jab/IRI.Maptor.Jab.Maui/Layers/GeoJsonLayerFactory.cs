using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.GeoJsonFormat;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

using Microsoft.Maui.Graphics;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;

namespace IRI.Maptor.Jab.Maui.Layers;

/// <summary>
/// Builds a <see cref="MapLayer"/> from GeoJSON text, reusing the core
/// <see cref="GeoJsonFeatureSet"/> parser and <see cref="MapProjects"/> projection.
/// Coordinates are projected to WebMercator once, at load time.
/// </summary>
public static class GeoJsonLayerFactory
{
    private const int Wgs84Srid = 4326;

    public static MapLayer FromGeoJson(string geoJsonText, string name, Color color)
    {
        var featureSet = GeoJsonFeatureSet.Parse(geoJsonText);

        var parts = new List<RenderPart>();

        double xMin = double.MaxValue, yMin = double.MaxValue;
        double xMax = double.MinValue, yMax = double.MinValue;

        void Track(double x, double y)
        {
            if (x < xMin) xMin = x;
            if (y < yMin) yMin = y;
            if (x > xMax) xMax = x;
            if (y > yMax) yMax = y;
        }

        if (featureSet?.Features != null)
        {
            foreach (var feature in featureSet.Features)
            {
                if (feature.Geometry is null)
                {
                    continue;
                }

                var raw = feature.Geometry.Parse(isLongitudeFirst: true, srid: Wgs84Srid);

                Geometry<Point>? geometry = raw switch
                {
                    Geometry<Point> g => g,
                    Geometry<PointZ> gz => ToPoint(gz),
                    Geometry<PointZM> gzm => ToPoint(gzm),
                    _ => null,
                };

                if (geometry != null)
                {
                    Flatten(geometry, parts, Track);
                }
            }
        }

        BoundingBox? extent = xMax >= xMin && yMax >= yMin
            ? new BoundingBox(xMin, yMin, xMax, yMax)
            : null;

        return new MapLayer(name, color)
        {
            Parts = parts,
            Extent = extent,
        };
    }

    private static Geometry<Point> ToPoint<TP>(Geometry<TP> geometry) where TP : IPoint, new()
    {
        if (geometry.IsLeafGeometry())
        {
            var points = geometry.Points?.Select(p => new Point(p.X, p.Y)).ToList() ?? new List<Point>();
            return Geometry<Point>.Create(points, geometry.Type, geometry.Srid);
        }

        var subGeometries = (geometry.Geometries ?? new List<Geometry<TP>>()).Select(ToPoint).ToList();
        return Geometry<Point>.Create(subGeometries, geometry.Type, geometry.Srid);
    }

    private static void Flatten(Geometry<Point> geometry, List<RenderPart> parts, Action<double, double> track)
    {
        switch (geometry.Type)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                parts.Add(MakePart(RenderKind.Point, new[] { geometry.GetAllPoints() }, track));
                break;

            case GeometryType.LineString:
                parts.Add(MakePart(RenderKind.Line, new[] { geometry.Points ?? new List<Point>() }, track));
                break;

            case GeometryType.MultiLineString:
                parts.Add(MakePart(RenderKind.Line, Rings(geometry), track));
                break;

            case GeometryType.Polygon:
                parts.Add(MakePart(RenderKind.Polygon, Rings(geometry), track));
                break;

            case GeometryType.MultiPolygon:
                foreach (var polygon in geometry.Geometries ?? new List<Geometry<Point>>())
                {
                    parts.Add(MakePart(RenderKind.Polygon, Rings(polygon), track));
                }

                break;

            case GeometryType.GeometryCollection:
                foreach (var sub in geometry.Geometries ?? new List<Geometry<Point>>())
                {
                    Flatten(sub, parts, track);
                }

                break;
        }
    }

    private static IEnumerable<List<Point>> Rings(Geometry<Point> geometry)
        => (geometry.Geometries ?? new List<Geometry<Point>>()).Select(g => g.Points ?? new List<Point>());

    private static RenderPart MakePart(RenderKind kind, IEnumerable<List<Point>> rings, Action<double, double> track)
    {
        var projectedRings = new List<(double[] X, double[] Y)>();

        foreach (var ring in rings)
        {
            var count = ring.Count;
            var xs = new double[count];
            var ys = new double[count];

            for (int i = 0; i < count; i++)
            {
                var mercator = MapProjects.GeodeticWgs84ToWebMercator(ring[i]);
                xs[i] = mercator.X;
                ys[i] = mercator.Y;
                track(mercator.X, mercator.Y);
            }

            projectedRings.Add((xs, ys));
        }

        return new RenderPart { Kind = kind, Rings = projectedRings };
    }
}
