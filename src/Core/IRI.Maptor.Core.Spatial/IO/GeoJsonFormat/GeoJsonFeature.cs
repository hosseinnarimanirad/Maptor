using IRI.Maptor.Core.Common.Common.JsonConverters;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.SpatialReferenceSystem;
using System.Text.Json.Serialization;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.GeoJsonFormat;

/// <summary>
/// Represents a GeoJSON Feature object (RFC 7946).
/// </summary>
public class GeoJsonFeature
{
    protected const string _defaultSymbologyPropertyName = "$maptorStyleMetadata";

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

    ///// <summary>
    ///// Gets or sets the geometry name. Optional.
    ///// </summary>
    //[JsonPropertyName("geometry_name")]
    //public string? GeometryName { get; set; }

    /// <summary>
    /// Gets or sets the properties of the feature as a dictionary of key-value pairs.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonConverter(typeof(DictionaryStringObjectConverter))]
    public Dictionary<string, object>? Properties { get; set; }


    public void AddSldAttribute(string sldString)
    {
        if (this.Properties.ContainsKey(_defaultSymbologyPropertyName))
        {
            this.Properties[_defaultSymbologyPropertyName] = sldString;
        }
        else
        {
            this.Properties.Add(_defaultSymbologyPropertyName, sldString);
        }
    }

    public string RetrieveSldAttribute()
    {
        if (this.Properties.ContainsKey(_defaultSymbologyPropertyName))
            return Properties[_defaultSymbologyPropertyName]?.ToString()!;

        return string.Empty;
    }

    /// <summary>
    /// Converts this GeoJSON feature to a Feature&lt;Point&gt; with the specified coordinate order and spatial reference system.
    /// </summary>
    /// <param name="isLongitudeFirst">If true, coordinates are interpreted as [longitude, latitude]; otherwise [latitude, longitude].</param>
    /// <param name="targetSrs">The target spatial reference system. Defaults to WGS84 if not specified.</param>
    /// <param name="sourceSrid">The source spatial reference system for the coordinates. Defaults to WGS84 (4326) if null.</param>
    /// <returns>A Feature&lt;Point&gt; instance.</returns>
    public Feature<Point> AsFeature(bool isLongitudeFirst, SrsBase? targetSrs = null, int? sourceSrid = null)
    {
        targetSrs = targetSrs ?? SrsBases.GeodeticWgs84;
        var effectiveSourceSrid = sourceSrid ?? SridHelper.GeodeticWGS84;

        var geometry = this.Geometry.Parse(isLongitudeFirst, effectiveSourceSrid);

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
            Attributes = this.Properties ?? [],
            TheGeometry = pointGeometry.Project(targetSrs),
        };
    }


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
            //GeometryName = string.Empty,
            Id = null,
            Properties = attributes ?? new Dictionary<string, object>(),
        };
    }

}
