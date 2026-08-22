using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON geometry object (RFC 7946). Supports Point, MultiPoint, LineString, MultiLineString, Polygon, and MultiPolygon.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GeoJsonPoint), "Point")]
[JsonDerivedType(typeof(GeoJsonMultiPoint), "MultiPoint")]
[JsonDerivedType(typeof(GeoJsonLineString), "LineString")]
[JsonDerivedType(typeof(GeoJsonMultiLineString), "MultiLineString")]
[JsonDerivedType(typeof(GeoJsonPolygon), "Polygon")]
[JsonDerivedType(typeof(GeoJsonMultiPolygon), "MultiPolygon")]
public interface IGeoJsonGeometry
{
    /// <summary>
    /// Gets or sets the type of the geometry (e.g., "Point", "LineString", "Polygon").
    /// </summary>
    [JsonPropertyName("type")]
    string? Type { get; set; }

    /// <summary>
    /// Gets the geometry type as a GeometryType enum value.
    /// </summary>
    [JsonIgnore()]
    GeometryType GeometryType { get; }

    /// <summary>
    /// Parses this GeoJSON geometry to a Geometry&lt;Point&gt; instance.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="srid">The spatial reference system identifier.</param>
    /// <returns>A Geometry&lt;Point&gt; instance.</returns>
    IGeometry Parse(bool isLongitudeFirst = true, int srid = 0);

    /// <summary>
    /// Determines whether this geometry is null or empty.
    /// </summary>
    /// <returns>True if the geometry is null or empty; otherwise, false.</returns>
    bool IsNullOrEmpty();

    /// <summary>
    /// Gets the number of geometries in this GeoJSON geometry (1 for simple geometries, multiple for multi-geometries).
    /// </summary>
    /// <returns>The number of geometries.</returns>
    int NumberOfGeometries();

    /// <summary>
    /// Gets the total number of points in this geometry.
    /// </summary>
    /// <returns>The total number of points.</returns>
    int NumberOfPoints();

    /// <summary>
    /// Serializes this geometry to a JSON string.
    /// </summary>
    /// <param name="indented">If true, the JSON output will be indented.</param>
    /// <param name="removeSpaces">If true, all spaces will be removed from the output.</param>
    /// <returns>A JSON string representation of this geometry.</returns>
    string Serialize(bool indented, bool removeSpaces = false);
     
    /// <summary>
    /// Transforms this geometry to Web Mercator projection.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <returns>A Geometry&lt;Point&gt; instance in Web Mercator projection.</returns>
    Geometry<Point> TransformToWeMercator(bool isLongitudeFirst = true);

    /// <summary>
    /// Converts this geometry to a GeoJSON Feature.
    /// </summary>
    /// <returns>A GeoJsonFeature instance.</returns>
    GeoJsonFeature AsFeature();

    /// <summary>
    /// Converts this geometry to a GeoJSON FeatureSet.
    /// </summary>
    /// <returns>A GeoJsonFeatureSet instance.</returns>
    GeoJsonFeatureSet AsFeatureSet();
}
