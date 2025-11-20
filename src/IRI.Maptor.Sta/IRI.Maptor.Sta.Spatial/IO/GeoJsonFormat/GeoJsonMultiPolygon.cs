using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON MultiPolygon geometry (RFC 7946).
/// </summary>
public class GeoJsonMultiPolygon : GeoJsonBase
{
    private static readonly GeoJsonMultiPolygon _empty = new GeoJsonMultiPolygon() { Coordinates = [] };

    /// <summary>
    /// Gets an empty MultiPolygon instance.
    /// </summary>
    public static GeoJsonMultiPolygon Empty => _empty;

    /// <summary>
    /// Gets or sets the type of the geometry. Must be "MultiPolygon".
    /// </summary>
    [JsonIgnore] 
    public override string? Type { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the MultiPolygon as an array of Polygon coordinate arrays.
    /// Each Polygon is an array of LinearRings (first is exterior, rest are holes).
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[][][][]? Coordinates { get; set; }

    /// <summary>
    /// Gets the geometry type as GeometryType.MultiPolygon.
    /// </summary>
    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.MultiPolygon; }

    /// <summary>
    /// Gets whether this geometry has Z (elevation) coordinates.
    /// Returns true if coordinates have 3 or more dimensions.
    /// </summary>
    [JsonIgnore] public override bool HasZ => GeoJson.DetectCoordinateDimension(Coordinates) >= 3;

    /// <summary>
    /// Gets whether this geometry has M (measure) coordinates.
    /// Returns true if coordinates have 4 dimensions.
    /// </summary>
    [JsonIgnore] public override bool HasM => GeoJson.DetectCoordinateDimension(Coordinates) >= 4;

    /// <summary>
    /// Initializes a new instance of GeoJsonMultiPolygon with Type set to "MultiPolygon".
    /// </summary>
    public GeoJsonMultiPolygon() => Type = GeoJson.MultiPolygon;

    /// <summary>
    /// Determines whether this MultiPolygon is null or empty.
    /// </summary>
    /// <returns>True if Coordinates is null or has less than 1 element; otherwise, false.</returns>
    public override bool IsNullOrEmpty() => Coordinates == null || Coordinates.Length < 1;

    /// <summary>
    /// Gets the number of Polygons in this MultiPolygon.
    /// </summary>
    /// <returns>The number of Polygons, or 0 if Coordinates is null.</returns>
    public override int NumberOfGeometries() => Coordinates == null ? 0 : Coordinates.Length;

    /// <summary>
    /// Gets the total number of points across all rings in all Polygons.
    /// </summary>
    /// <returns>The total number of points, or 0 if Coordinates is null.</returns>
    public override int NumberOfPoints()
    {
        return Coordinates == null ? 0 : Coordinates.Sum(part => part == null ? 0 : part.Sum(ring => ring == null ? 0 : ring.Length));
    }

    /// <summary>
    /// Parses this GeoJSON MultiPolygon to an IGeometry instance.
    /// Returns Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt; based on coordinate dimensions.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public override IGeometry Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPolygon, srid);

        return (this.HasZ, this.HasM) switch
        {
            (true, true) => new Geometry<PointZM>(Coordinates!.Select(c => GeoJson.CreateGeometryFromPolygonCoordinates(c, GeoJson.PointZMFactory, GeometryType.Polygon, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid),
            (true, false) => new Geometry<PointZ>(Coordinates!.Select(c => GeoJson.CreateGeometryFromPolygonCoordinates(c, GeoJson.PointZFactory, GeometryType.Polygon, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid),
            _ => new Geometry<Point>(Coordinates!.Select(c => GeoJson.CreateGeometryFromPolygonCoordinates(c, GeoJson.PointFactory, GeometryType.Polygon, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid),
        };
    }

    /// <summary>
    /// Creates a new GeoJSON MultiPolygon from an array of Polygon coordinate arrays.
    /// </summary>
    /// <param name="coordinates">An array of Polygon coordinate arrays, where each Polygon is an array of LinearRings (first is exterior, rest are holes).</param>
    /// <returns>A new GeoJsonMultiPolygon instance.</returns>
    public static GeoJsonMultiPolygon Create(double[][][][] coordinates)
    {
        return new GeoJsonMultiPolygon() { Coordinates = coordinates };
    }

    /// <summary>
    /// Creates a new GeoJSON MultiPolygon from a collection of Polygons.
    /// </summary>
    /// <param name="polygons">A collection of Polygons, where each Polygon is an array of LinearRings (first is exterior, rest are holes).</param>
    /// <returns>A new GeoJsonMultiPolygon instance.</returns>
    public static GeoJsonMultiPolygon Create(IEnumerable<double[][][]> polygons)
    {
        return new GeoJsonMultiPolygon() { Coordinates = polygons?.ToArray() };
    }

    /// <summary>
    /// Validates polygon ring orientations for all polygons according to RFC 7946 Section 3.1.6.
    /// For each polygon, the first ring (external ring) must be counterclockwise.
    /// Subsequent rings (internal rings/holes) must be clockwise.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>A tuple indicating if validation passed and a list of validation errors (if any).</returns>
    public (bool IsValid, List<string> Errors) ValidateRingOrientations(bool isLongitudeFirst = true)
    {
        var allErrors = new List<string>();

        if (this.Coordinates == null || this.Coordinates.Length == 0)
        {
            return (true, allErrors); // Empty MultiPolygon is valid
        }

        // Validate each polygon
        for (int polygonIndex = 0; polygonIndex < this.Coordinates.Length; polygonIndex++)
        {
            var polygonRings = this.Coordinates[polygonIndex];
            if (polygonRings == null || polygonRings.Length == 0)
                continue;

            var (isValid, errors) = GeoJson.ValidatePolygonRingOrientations(polygonRings, isLongitudeFirst);
            if (!isValid)
            {
                foreach (var error in errors)
                {
                    allErrors.Add($"Polygon {polygonIndex}: {error}");
                }
            }
        }

        return (allErrors.Count == 0, allErrors);
    }
}
