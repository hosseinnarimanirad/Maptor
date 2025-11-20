using System.Collections.Generic;
using System.Text.Json.Serialization;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON MultiLineString geometry (RFC 7946).
/// </summary>
public class GeoJsonMultiLineString : GeoJsonBase
{
    private static readonly GeoJsonMultiLineString _empty = new GeoJsonMultiLineString() { Coordinates = [] };

    /// <summary>
    /// Gets an empty MultiLineString instance.
    /// </summary>
    public static GeoJsonMultiLineString Empty => _empty;

    /// <summary>
    /// Gets or sets the type of the geometry. Must be "MultiLineString".
    /// </summary>
    [JsonIgnore]
    public override string? Type { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the MultiLineString as an array of LineString coordinate arrays.
    /// Each LineString is an array of coordinate arrays representing points.
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[][][]? Coordinates { get; set; }

    /// <summary>
    /// Gets the geometry type as GeometryType.MultiLineString.
    /// </summary>
    [JsonIgnore]
    public override GeometryType GeometryType { get => GeometryType.MultiLineString; }

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
    /// Initializes a new instance of GeoJsonMultiLineString with Type set to "MultiLineString".
    /// </summary>
    public GeoJsonMultiLineString() => Type = GeoJson.MultiLineString;
     
    /// <summary>
    /// Determines whether this MultiLineString is null or empty.
    /// </summary>
    /// <returns>True if Coordinates is null or has less than 1 element; otherwise, false.</returns>
    public override bool IsNullOrEmpty() => Coordinates == null || Coordinates.Length < 1;

    /// <summary>
    /// Gets the number of LineStrings in this MultiLineString.
    /// </summary>
    /// <returns>The number of LineStrings, or 0 if Coordinates is null.</returns>
    public override int NumberOfGeometries() => Coordinates == null ? 0 : Coordinates.Length;

    /// <summary>
    /// Gets the total number of points across all LineStrings in this MultiLineString.
    /// </summary>
    /// <returns>The total number of points, or 0 if Coordinates is null.</returns>
    public override int NumberOfPoints() => Coordinates == null ? 0 : Coordinates.Sum(part => part == null ? 0 : part.Length);

    /// <summary>
    /// Parses this GeoJSON MultiLineString to an IGeometry instance.
    /// Returns Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt; based on coordinate dimensions.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier. Defaults to WGS84.</param>
    /// <returns>An IGeometry instance (Geometry&lt;Point&gt;, Geometry&lt;PointZ&gt;, or Geometry&lt;PointZM&gt;).</returns>
    public override IGeometry Parse(bool isLongitudeFirst = true, int srid = SridHelper.GeodeticWGS84)
    {
        if (this.Coordinates.IsNullOrEmpty())
            return Geometry<Point>.CreateEmpty(GeometryType.MultiLineString, srid);

        return (this.HasZ, this.HasM) switch
        {
            (true, true) => new Geometry<PointZM>(Coordinates!.Select(c => GeoJson.CreateGeometryFromLineCoordinates(c, GeoJson.PointZMFactory, GeometryType.LineString, false, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid),
            (true, false) => new Geometry<PointZ>(Coordinates!.Select(c => GeoJson.CreateGeometryFromLineCoordinates(c, GeoJson.PointZFactory, GeometryType.LineString, false, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid),
            _ => new Geometry<Point>(Coordinates!.Select(c => GeoJson.CreateGeometryFromLineCoordinates(c, GeoJson.PointFactory, GeometryType.LineString, false, isLongitudeFirst, srid)).ToList(), this.GeometryType, srid),
        };
    }

    /// <summary>
    /// Creates a new GeoJSON MultiLineString from an array of LineString coordinate arrays.
    /// </summary>
    /// <param name="coordinates">An array of LineString coordinate arrays, where each LineString is an array of coordinate arrays representing points.</param>
    /// <returns>A new GeoJsonMultiLineString instance.</returns>
    public static GeoJsonMultiLineString Create(double[][][] coordinates)
    {
        return new GeoJsonMultiLineString() { Coordinates = coordinates };
    }

    /// <summary>
    /// Creates a new GeoJSON MultiLineString from a collection of LineStrings.
    /// </summary>
    /// <param name="lineStrings">A collection of LineStrings, where each LineString is an array of coordinate arrays representing points.</param>
    /// <returns>A new GeoJsonMultiLineString instance.</returns>
    public static GeoJsonMultiLineString Create(IEnumerable<double[][]> lineStrings)
    {
        return new GeoJsonMultiLineString() { Coordinates = lineStrings?.ToArray() };
    }
}
