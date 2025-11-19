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

    /// <summary>
    /// Gets or sets the type of the geometry. Must be "MultiPoint".
    /// </summary>
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
    public override GeometryType GeometryType { get => GeometryType.MultiPoint; }

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
            return GeoJson.CreateEmptyGeometry(GeometryType.MultiPoint, srid);

        var maxDim = GeoJson.DetectMaxCoordinateDimension(Coordinates!);
        var normalizedCoords = Coordinates!.Select(c =>
        {
            if (c == null) return new double[maxDim];
            var normalized = new double[maxDim];
            Array.Copy(c, normalized, Math.Min(c.Length, maxDim));
            for (int i = c.Length; i < maxDim; i++)
                normalized[i] = 0;
            return normalized;
        }).ToArray();

        var pointGeometries = normalizedCoords.Select(c => GeoJson.CreateGeometryFromPointCoordinates(c, GeometryType.Point, isLongitudeFirst, srid)).ToList();

        return maxDim switch
        {
            2 => new Geometry<Point>(pointGeometries.Cast<Geometry<Point>>().ToList(), this.GeometryType, srid),
            3 => new Geometry<PointZ>(pointGeometries.Cast<Geometry<PointZ>>().ToList(), this.GeometryType, srid),
            4 => new Geometry<PointZM>(pointGeometries.Cast<Geometry<PointZM>>().ToList(), this.GeometryType, srid),
            _ => throw new NotSupportedException($"Unsupported coordinate dimension: {maxDim}")
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
