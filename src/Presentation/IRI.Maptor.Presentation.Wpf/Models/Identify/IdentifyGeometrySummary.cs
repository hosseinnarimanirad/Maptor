using System.Linq;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Spatial.Analysis;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Presentation.Wpf.Models.Identify;

/// <summary>
/// The geometry facts shown above an identified feature's attributes: geodesic length (lines), area and
/// perimeter (polygons), and the point, part and hole counts. Measures are on the WGS84 ellipsoid, like the
/// Layer Settings Geometry tab (<see cref="GeometryMeasure"/>), whatever SRID the geometry is in.
/// </summary>
public sealed class IdentifyGeometrySummary
{
    private IdentifyGeometrySummary() { }

    public GeometryType GeometryType { get; private init; }

    /// <summary>Geodesic length of a line, metres; null for other types or when it could not be measured.</summary>
    public double? Length { get; private init; }

    /// <summary>Area of a polygon (holes subtracted), square metres; null for other types or when it could not be measured.</summary>
    public double? Area { get; private init; }

    /// <summary>Length of every ring of a polygon, holes included, metres.</summary>
    public double? Perimeter { get; private init; }

    /// <summary>Stored vertices; rings never repeat their first point, so nothing is subtracted.</summary>
    public int NumberOfPoints { get; private init; }

    /// <summary>Line strings of a line, polygons of a polygon (a polygon's holes are not parts), points of a multipoint.</summary>
    public int NumberOfParts { get; private init; }

    /// <summary>Inner rings over all the polygons.</summary>
    public int NumberOfHoles { get; private init; }

    public string? LengthLabel => Length is double value ? UnitHelper.GetLengthLabel(value) : null;

    public string? AreaLabel => Area is double value ? UnitHelper.GetAreaLabel(value) : null;

    public string? PerimeterLabel => Perimeter is double value ? UnitHelper.GetLengthLabel(value) : null;

    public bool ShowLength => Length.HasValue;

    public bool ShowArea => Area.HasValue;

    public bool ShowPerimeter => Perimeter.HasValue;

    /// <summary>A single point has nothing worth counting.</summary>
    public bool ShowPoints => GeometryType != GeometryType.Point && NumberOfPoints > 0;

    /// <summary>Only a geometry with more than one part; for a multipoint the parts are its points, already counted.</summary>
    public bool ShowParts => NumberOfParts > 1 && GeometryType != GeometryType.MultiPoint;

    public bool ShowHoles => NumberOfHoles > 0;

    /// <summary>False for a point or an empty geometry: the view then hides the strip.</summary>
    public bool HasAny => ShowLength || ShowArea || ShowPerimeter || ShowPoints || ShowParts || ShowHoles;

    /// <summary>Null for a null or empty geometry. CPU cost is one transform to WGS84, fine for one feature on the UI thread.</summary>
    public static IdentifyGeometrySummary? Create(Geometry<Point>? geometry)
    {
        if (geometry is null || geometry.IsNullOrEmpty())
            return null;

        var type = geometry.Type;

        var (length, area, perimeter) = Measure(geometry);

        return new IdentifyGeometrySummary
        {
            GeometryType = type,
            Length = length,
            Area = area,
            Perimeter = perimeter,
            NumberOfPoints = geometry.TotalNumberOfPoints,
            NumberOfParts = CountParts(geometry),
            NumberOfHoles = CountHoles(geometry),
        };
    }

    private static (double? Length, double? Area, double? Perimeter) Measure(Geometry<Point> geometry)
    {
        var kind = GeometryMeasure.GetKind(geometry.Type);

        if (kind == GeometryMeasureKind.None)
            return (null, null, null);

        try
        {
            var wgs84 = GeometryMeasure.ToGeodeticWgs84(geometry);

            if (wgs84 is null)
                return (null, null, null);

            // GetEllipsoidalLength walks every ring of a polygon, closing segment included: that is the perimeter
            return kind == GeometryMeasureKind.Length
                ? (wgs84.GetEllipsoidalLength(), null, null)
                : (null, SpatialUtility.GetEllipsoidalArea(wgs84), wgs84.GetEllipsoidalLength());
        }
        catch
        {
            // a broken geometry still gets its counts; the measures are just left out
            return (null, null, null);
        }
    }

    private static int CountParts(Geometry<Point> geometry) => geometry.Type switch
    {
        GeometryType.Point or GeometryType.LineString or GeometryType.Polygon => 1,
        GeometryType.MultiPoint => geometry.NumberOfPoints,
        _ => geometry.Geometries?.Count(g => !g.IsNullOrEmpty()) ?? 0,
    };

    private static int CountHoles(Geometry<Point> geometry) => geometry.Type switch
    {
        GeometryType.Polygon => HolesOf(geometry),
        GeometryType.MultiPolygon => geometry.Geometries?.Sum(HolesOf) ?? 0,
        _ => 0,
    };

    private static int HolesOf(Geometry<Point> polygon) => System.Math.Max(0, (polygon.Geometries?.Count ?? 0) - 1);
}
