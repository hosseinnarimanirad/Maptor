using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON Polygon geometry (RFC 7946).
/// </summary>
public class GeoJsonPolygon : GeoJsonBase
{
    private static readonly GeoJsonPolygon _empty = new GeoJsonPolygon() { Coordinates = [] };

    /// <summary>
    /// Gets an empty Polygon instance.
    /// </summary>
    public static GeoJsonPolygon Empty => _empty;

    /// <summary>
    /// Gets or sets the type of the geometry. Must be "Polygon".
    /// </summary>
    [JsonIgnore]
    public override string? Type { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the Polygon as an array of LinearRings.
    /// The first ring is the exterior ring, subsequent rings are holes.
    /// Each ring is an array of coordinate arrays representing points.
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[][][]? Coordinates { get; set; }

    /// <summary>
    /// Gets the geometry type as GeometryType.Polygon.
    /// </summary>
    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.Polygon; }

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
    /// Initializes a new instance of GeoJsonPolygon with Type set to "Polygon".
    /// </summary>
    public GeoJsonPolygon() => Type = GeoJson.Polygon;

    /// <summary>
    /// Determines whether this Polygon is null or empty.
    /// </summary>
    /// <returns>True if Coordinates is null or has less than 1 element; otherwise, false.</returns>
    public override bool IsNullOrEmpty() => Coordinates == null || Coordinates.Length < 1;

    /// <summary>
    /// Gets the number of rings in this Polygon.
    /// </summary>
    /// <returns>The number of rings, or 0 if Coordinates is null.</returns>
    public override int NumberOfGeometries() => Coordinates == null ? 0 : Coordinates.Length;

    /// <summary>
    /// Gets the total number of points across all rings in this Polygon.
    /// </summary>
    /// <returns>The total number of points, or 0 if Coordinates is null.</returns>
    public override int NumberOfPoints() => Coordinates == null ? 0 : Coordinates.Sum(ring => ring == null ? 0 : ring.Length);

    /// <summary>
    /// Parses this GeoJSON Polygon to an IGeometry instance.
    /// Returns Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt; based on coordinate dimensions.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public override IGeometry Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.Polygon, srid);

        return (this.HasZ, this.HasM) switch
        {
            (true, true) => GeoJson.CreateGeometryFromPolygonCoordinates(Coordinates!, GeoJson.PointZMFactory, GeometryType.Polygon, isLongitudeFirst, srid),
            (true, false) => GeoJson.CreateGeometryFromPolygonCoordinates(Coordinates!, GeoJson.PointZFactory, GeometryType.Polygon, isLongitudeFirst, srid),
            _ => GeoJson.CreateGeometryFromPolygonCoordinates(Coordinates!, GeoJson.PointFactory, GeometryType.Polygon, isLongitudeFirst, srid),
        };
    }

    /// <summary>
    /// Creates a new GeoJSON Polygon from an array of LinearRings.
    /// </summary>
    /// <param name="coordinates">An array of LinearRings, where the first ring is the exterior ring and subsequent rings are holes. Each ring is an array of coordinate arrays representing points.</param>
    /// <returns>A new GeoJsonPolygon instance.</returns>
    public static GeoJsonPolygon Create(double[][][] coordinates)
    {
        return new GeoJsonPolygon() { Coordinates = coordinates };
    }

    /// <summary>
    /// Creates a new GeoJSON Polygon from a collection of LinearRings.
    /// </summary>
    /// <param name="rings">A collection of LinearRings, where the first ring is the exterior ring and subsequent rings are holes. Each ring is an array of coordinate arrays representing points.</param>
    /// <returns>A new GeoJsonPolygon instance.</returns>
    public static GeoJsonPolygon Create(IEnumerable<double[][]> rings)
    {
        return new GeoJsonPolygon() { Coordinates = rings?.ToArray() };
    }

    /// <summary>
    /// Validates polygon ring orientations according to RFC 7946 Section 3.1.6.
    /// The first ring (external ring) must be counterclockwise.
    /// Subsequent rings (internal rings/holes) must be clockwise.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>A tuple indicating if validation passed and a list of validation errors (if any).</returns>
    public (bool IsValid, List<string> Errors) ValidateRingOrientations(bool isLongitudeFirst = true)
    {
        if (this.Coordinates == null || this.Coordinates.Length == 0)
        {
            return (true, new List<string>()); // Empty polygon is valid
        }

        return GeoJson.ValidatePolygonRingOrientations(this.Coordinates, isLongitudeFirst);
    }
}
