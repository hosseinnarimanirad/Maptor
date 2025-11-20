using System.Text.Json.Serialization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON Point geometry (RFC 7946).
/// </summary>
public class GeoJsonPoint : GeoJsonBase
{
    private static readonly GeoJsonPoint _empty = new GeoJsonPoint() { Coordinates = [] };

    /// <summary>
    /// Gets an empty Point instance.
    /// </summary>
    public static GeoJsonPoint Empty => _empty;

    /// <summary>
    /// Gets or sets the type of the geometry. Must be "Point".
    /// </summary>
    [JsonIgnore]
    public override string? Type { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the point as [longitude, latitude] or [longitude, latitude, elevation].
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[]? Coordinates { get; set; }

    /// <summary>
    /// Gets the geometry type as GeometryType.Point.
    /// </summary>
    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.Point; }

    /// <summary>
    /// Initializes a new instance of GeoJsonPoint with Type set to "Point".
    /// </summary>
    public GeoJsonPoint() => Type = GeoJson.Point;

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
    /// Determines whether this point is null or empty.
    /// </summary>
    /// <returns>True if Coordinates is null or has less than 1 element; otherwise, false.</returns>
    public override bool IsNullOrEmpty() => Coordinates == null || Coordinates.Length < 1;

    /// <summary>
    /// Gets the number of geometries. Always returns 1 for Point.
    /// </summary>
    /// <returns>1</returns>
    public override int NumberOfGeometries() => 1;

    /// <summary>
    /// Gets the number of points. Always returns 1 for Point.
    /// </summary>
    /// <returns>1</returns>
    public override int NumberOfPoints() => 1;

    /// <summary>
    /// Parses this GeoJSON point to an IGeometry instance.
    /// Returns Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt; based on coordinate dimensions.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public override IGeometry Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.Point, srid);

        return (this.HasZ, this.HasM) switch
        {
            (true, true) => new Geometry<PointZM>([GeoJson.CreatePointFromCoordinates(Coordinates!, GeoJson.PointZMFactory, isLongitudeFirst)], GeometryType.Point, srid),
            (true, false) => new Geometry<PointZ>([GeoJson.CreatePointFromCoordinates(Coordinates!, GeoJson.PointZFactory, isLongitudeFirst)], GeometryType.Point, srid),
            _ => new Geometry<Point>([GeoJson.CreatePointFromCoordinates(Coordinates!, GeoJson.PointFactory, isLongitudeFirst)], GeometryType.Point, srid),
        };
    }

    /// <summary>
    /// Creates a new GeoJSON point with the specified longitude and latitude.
    /// </summary>
    /// <param name="longitude">The longitude coordinate.</param>
    /// <param name="latitude">The latitude coordinate.</param>
    /// <returns>A new GeoJsonPoint instance.</returns>
    public static GeoJsonPoint Create(double longitude, double latitude)
    {
        return new GeoJsonPoint() { Coordinates = [longitude, latitude], Type = GeoJson.Point };
    }
}
