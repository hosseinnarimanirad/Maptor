using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.GeoJsonFormat;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;

using Microsoft.Maui.Graphics;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Maui.Layers;

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
                    RenderPartBuilder.Build(geometry, ProjectToWebMercator, parts, Track);
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
            Description = DescribeParts(parts, LayerSource.GeoJson),
            SourceGeoJson = geoJsonText,
        };
    }

    /// <summary>
    /// Builds a description like "Point (GeoJson)" / "Polygon (Drawn)" from the dominant
    /// geometry kind. Mixed-geometry layers are described as "Mixed".
    /// </summary>
    internal static string DescribeParts(IReadOnlyList<RenderPart> parts, LayerSource source)
    {
        var kinds = parts.Select(p => p.Kind).Distinct().ToList();

        string kind = kinds.Count switch
        {
            0 => "Empty",
            1 => kinds[0] switch
            {
                RenderKind.Point => "Point",
                RenderKind.Line => "Line",
                RenderKind.Polygon => "Polygon",
                _ => "Geometry",
            },
            _ => "Mixed",
        };

        return $"{kind} ({source})";
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

    private static (double X, double Y) ProjectToWebMercator(Point wgs84)
    {
        var mercator = MapProjects.GeodeticWgs84ToWebMercator(wgs84);
        return (mercator.X, mercator.Y);
    }
}
