using IRI.Maptor.Sta.Common.Common.JsonConverters;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using System.Text.Json.Serialization;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON Feature object (RFC 7946).
/// </summary>
public class GeoJsonFeature
{
    /// <summary>
    /// Gets or sets the type of the GeoJSON object. Must be "Feature".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = GeoJson.Feature;

    /// <summary>
    /// Gets or sets the identifier of the feature. Optional.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the geometry of the feature.
    /// </summary>
    [JsonPropertyName("geometry")]
    public IGeoJsonGeometry? Geometry { get; set; }

    /// <summary>
    /// Gets or sets the geometry name. Optional.
    /// </summary>
    [JsonPropertyName("geometry_name")]
    public string? GeometryName { get; set; }

    /// <summary>
    /// Gets or sets the properties of the feature as a dictionary of key-value pairs.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Creates a new GeoJSON feature with the specified geometry and optional attributes.
    /// </summary>
    /// <param name="geometry">The geometry for the feature.</param>
    /// <param name="attributes">Optional dictionary of attributes to set as properties.</param>
    /// <returns>A new GeoJsonFeature instance.</returns>
    public static GeoJsonFeature Create(IGeoJsonGeometry geometry, Dictionary<string, object>? attributes = null)
    {
        return new GeoJsonFeature()
        {
            Geometry = geometry,
            GeometryName = string.Empty,
            Id = "0",
            Properties = attributes ?? new Dictionary<string, object>(),
        };
    }

    /// <summary>
    /// Converts this GeoJSON feature to a Feature&lt;Point&gt; with the specified coordinate order and spatial reference system.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="targetSrs">The target spatial reference system. Defaults to WGS84 if not specified.</param>
    /// <returns>A Feature&lt;Point&gt; instance.</returns>
    public Feature<Point> AsFeature(bool isLongitudeFirst, SrsBase? targetSrs = null)
    {
        targetSrs = targetSrs ?? SrsBases.GeodeticWgs84;

        var geometry = this.Geometry.Parse(isLongitudeFirst, SridHelper.GeodeticWGS84);
        
        // Convert IGeometry to Geometry<Point> for projection
        Geometry<Point> pointGeometry = geometry switch
        {
            Geometry<PointZM> gzm => Geometry<Point>.Create(gzm.Points.Select(p => new Point(p.X, p.Y)).ToList(), geometry.Type, geometry.Srid),
            Geometry<PointZ> gz => Geometry<Point>.Create(gz.Points.Select(p => new Point(p.X, p.Y)).ToList(), geometry.Type, geometry.Srid),
            Geometry<Point> g => g,
            _ => throw new NotSupportedException($"Unsupported geometry type: {geometry.GetType()}")
        };

        return new Feature<Point>()
        {
            Attributes = this.Properties/*.ToDictionary(f => f.Key, f => (object)f.Value)*/,
            //Id = feature.id,
            //TheGeometry = feature.Geometry.AsSqlGeography(isLongitudeFirst, SridHelper.GeodeticWGS84)
            //                                    .Project(targetSrs.FromWgs84Geodetic<Point>, SridHelper.WebMercator).AsGeometry()
            TheGeometry = pointGeometry.Project(targetSrs)
        };
    }
}
