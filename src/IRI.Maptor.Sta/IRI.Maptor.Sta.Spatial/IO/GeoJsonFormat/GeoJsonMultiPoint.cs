using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON MultiPoint geometry (RFC 7946).
/// </summary>
public class GeoJsonMultiPoint : GeoJsonBase
{
    private static readonly GeoJsonMultiPoint _empty = new GeoJsonMultiPoint() { Coordinates = [] };

    /// <summary>
    /// Gets an empty MultiPoint instance.
    /// </summary>
    public static GeoJsonMultiPoint Empty => _empty;

    [JsonIgnore]
    public override string? Type { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the MultiPoint as an array of coordinate arrays.
    /// Each coordinate array represents a point as [longitude, latitude] or [longitude, latitude, elevation].
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[][]? Coordinates { get; set; }

    /// <summary>
    /// Gets the geometry type as GeometryType.MultiPoint.
    /// </summary>
    [JsonIgnore]
    public override GeometryType GeometryType => GeometryType.MultiPoint;

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
    /// Initializes a new instance of GeoJsonMultiPoint with Type set to "MultiPoint".
    /// </summary>
    public GeoJsonMultiPoint() => Type = GeoJson.MultiPoint;

    /// <summary>
    /// Determines whether this MultiPoint is null or empty.
    /// </summary>
    /// <returns>True if Coordinates is null or has less than 1 element; otherwise, false.</returns>
    public override bool IsNullOrEmpty() => Coordinates == null || Coordinates.Length < 1;

    /// <summary>
    /// Gets the number of points in this MultiPoint.
    /// </summary>
    /// <returns>The number of points, or 0 if Coordinates is null.</returns>
    public override int NumberOfGeometries() => Coordinates == null ? 0 : Coordinates.Length;

    /// <summary>
    /// Gets the number of points. For MultiPoint, this equals the number of geometries.
    /// </summary>
    /// <returns>The number of points.</returns>
    public override int NumberOfPoints()
    {
        // 1400.02.03
        // number of parts equals number of points
        return NumberOfGeometries();
    }

    /// <summary>
    /// Parses this GeoJSON MultiPoint to an IGeometry instance.
    /// Returns Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt; based on coordinate dimensions.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public override IGeometry Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiPoint, srid);

        return (this.HasZ, this.HasM) switch
        {
            (true, true) => new Geometry<PointZM>(GeoJson.CreatePointListFromCoordinates(Coordinates, GeoJson.PointZMFactory, isLongitudeFirst), this.GeometryType, srid),
            (true, false) => new Geometry<PointZ>(GeoJson.CreatePointListFromCoordinates(Coordinates, GeoJson.PointZFactory, isLongitudeFirst), this.GeometryType, srid),
            _ => new Geometry<Point>(GeoJson.CreatePointListFromCoordinates(Coordinates, GeoJson.PointFactory, isLongitudeFirst), this.GeometryType, srid),
        };
    }

    /// <summary>
    /// Creates a new GeoJSON MultiPoint from an array of coordinate arrays.
    /// </summary>
    /// <param name="coordinates">An array of coordinate arrays, where each coordinate array represents a point as [longitude, latitude] or [longitude, latitude, elevation].</param>
    /// <returns>A new GeoJsonMultiPoint instance.</returns>
    public static GeoJsonMultiPoint Create(double[][] coordinates)
    {
        return new GeoJsonMultiPoint() { Coordinates = coordinates };
    }

    /// <summary>
    /// Creates a new GeoJSON MultiPoint from a collection of points.
    /// </summary>
    /// <param name="points">A collection of points, where each point is represented as [longitude, latitude] or [longitude, latitude, elevation].</param>
    /// <returns>A new GeoJsonMultiPoint instance.</returns>
    public static GeoJsonMultiPoint Create(IEnumerable<double[]> points)
    {
        return new GeoJsonMultiPoint() { Coordinates = points?.ToArray() };
    }
}
