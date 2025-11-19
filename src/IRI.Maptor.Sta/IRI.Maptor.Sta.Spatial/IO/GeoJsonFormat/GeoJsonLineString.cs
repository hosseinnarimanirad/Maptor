using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;
 
/// <summary>
/// Represents a GeoJSON LineString geometry (RFC 7946).
/// </summary>
public class GeoJsonLineString : GeoJsonBase
{
    private static readonly GeoJsonLineString _empty = new GeoJsonLineString() { Coordinates = [] };

    /// <summary>
    /// Gets an empty LineString instance.
    /// </summary>
    public static GeoJsonLineString Empty => _empty;

    /// <summary>
    /// Gets or sets the type of the geometry. Must be "LineString".
    /// </summary>
    [JsonIgnore] 
    public override string? Type { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the LineString as an array of coordinate arrays.
    /// Each coordinate array represents a point as [longitude, latitude] or [longitude, latitude, elevation].
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[][]? Coordinates { get; set; }

    /// <summary>
    /// Gets the geometry type as GeometryType.LineString.
    /// </summary>
    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.LineString; }

    /// <summary>
    /// Initializes a new instance of GeoJsonLineString with Type set to "LineString".
    /// </summary>
    public GeoJsonLineString() => Type = GeoJson.LineString;

    /// <summary>
    /// Determines whether this LineString is null or empty.
    /// </summary>
    /// <returns>True if Coordinates is null or has less than 1 element; otherwise, false.</returns>
    public override bool IsNullOrEmpty() => Coordinates == null || Coordinates.Length < 1;

    /// <summary>
    /// Gets the number of geometries. Always returns 1 for LineString.
    /// </summary>
    /// <returns>1</returns>
    public override int NumberOfGeometries() => 1;

    /// <summary>
    /// Gets the number of points in this LineString.
    /// </summary>
    /// <returns>The number of points, or 0 if Coordinates is null.</returns>
    public override int NumberOfPoints() => Coordinates == null ? 0 : Coordinates.Length;

    /// <summary>
    /// Parses this GeoJSON LineString to an IGeometry instance.
    /// Returns Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt; based on coordinate dimensions.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public override IGeometry Parse(bool isLongitudeFirst = true, int srid = 0)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return GeoJson.CreateEmptyGeometry(GeometryType.LineString, srid);

        return GeoJson.CreateGeometryFromLineCoordinates(Coordinates!, this.GeometryType, false, isLongitudeFirst, srid);
    }

    /// <summary>
    /// Creates a new GeoJSON LineString from an array of coordinate arrays.
    /// </summary>
    /// <param name="coordinates">An array of coordinate arrays, where each coordinate array represents a point as [longitude, latitude] or [longitude, latitude, elevation].</param>
    /// <returns>A new GeoJsonLineString instance.</returns>
    public static GeoJsonLineString Create(double[][] coordinates)
    {
        return new GeoJsonLineString() { Coordinates = coordinates };
    }

    /// <summary>
    /// Creates a new GeoJSON LineString from a collection of points.
    /// </summary>
    /// <param name="points">A collection of points where each point is represented as [longitude, latitude] or [longitude, latitude, elevation].</param>
    /// <returns>A new GeoJsonLineString instance.</returns>
    public static GeoJsonLineString Create(IEnumerable<double[]> points)
    {
        return new GeoJsonLineString() { Coordinates = points?.ToArray() };
    }
}
