using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Services;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Core.Spatial.IO.EsriJson;

public class EsriJsonFeatureSet
{
    public static readonly EsriJsonFeatureSet Empty;

    public string ObjectIdFieldName { get; set; }
    public string? DisplayFieldName { get; set; }

    public Dictionary<string, string> FieldAliases { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EsriJsonGeometryType GeometryType { get; set; } // like: esriGeometryPolygon

    public EsriJsonSpatialReference SpatialReference { get; set; }

    public List<EsriJsonField>? Fields { get; set; }

    public List<EsriJsonFeature> Features { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasZ { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasM { get; set; }

    static EsriJsonFeatureSet()
    {
        Empty = new EsriJsonFeatureSet() { Features = [], Fields = [], SpatialReference = new EsriJsonSpatialReference() { Wkid = 0 } };
    }

    public async Task Save(string fileName, bool indented, bool removeSpaces = false)
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var result = JsonHelper.Serialize(this, options);

        await System.IO.File.WriteAllTextAsync(fileName, removeSpaces ? result.Replace(" ", string.Empty) : result);
    }

    public FeatureSet<Point> AsFeatureSet()
    {
        var srid = this.SpatialReference.LatestWkid ?? this.SpatialReference.Wkid;

        var features = this.Features.Select(f => f.AsFeature(srid)).ToList();

        var result = FeatureSet<Point>.Create("esri json", features);

        result.Fields = this.Fields.Select(f => f.AsField()).ToList();

        result.Srid = srid;

        result.GeometryType = this.GeometryType switch
        {
            EsriJsonGeometryType.esriGeometryPoint => IRI.Maptor.Core.Common.Enums.GeometryType.Point,
            EsriJsonGeometryType.esriGeometryMultipoint => IRI.Maptor.Core.Common.Enums.GeometryType.MultiPoint,
            EsriJsonGeometryType.esriGeometryPolyline => IRI.Maptor.Core.Common.Enums.GeometryType.LineString,
            EsriJsonGeometryType.esriGeometryPolygon => IRI.Maptor.Core.Common.Enums.GeometryType.Polygon,
            _ => throw new ArgumentException("EsriJsonFeatureSet > AsFeatureSet")
        };

        return result;
    }

    public static async Task<EsriJsonFeatureSet?> Load(string fileName)
    {
        var esriJsonText = await File.ReadAllTextAsync(fileName);

        return Parse(esriJsonText);
    }

    public static EsriJsonFeatureSet? Parse(string jsonString)
    {
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var result = JsonHelper.Deserialize<EsriJsonFeatureSet>(jsonString, options) ?? Empty;

        //if (result is null || result.Features.IsNullOrEmpty())
        //    return result;

        //if (result.SpatialReference is null)
        //    return result;

        //foreach (var item in result.Features)
        //{
        //    if (item.Geometry.SpatialReference is null)
        //        item.Geometry.SpatialReference = result.SpatialReference;
        //}

        return result;
    }


    // todo: remove added Take(2) for test
    public static EsriJsonFeatureSet? Parse(FeatureSet<Point> featureSet)
    {
        var esriFeatures = featureSet.Features.Select(f => f.AsEsriJsonFeature()).ToList();

        var fields = featureSet.Fields?.Where(f => !f.Name.EqualsIgnoreCase("objectid"))?.ToList();

        var esriFeatureSet = new EsriJsonFeatureSet()
        {
            Features = esriFeatures,
            FieldAliases = fields.ToDictionary(f => f.Name, f => f.Alias ?? string.Empty),
            Fields = fields.Select(EsriJsonField.Parse).ToList(),
            SpatialReference = new EsriJsonSpatialReference() { LatestWkid = featureSet.Srid },
        };

        esriFeatureSet.GeometryType = featureSet.GeometryType switch
        {
            IRI.Maptor.Core.Common.Enums.GeometryType.Point => EsriJsonGeometryType.esriGeometryPoint,
            IRI.Maptor.Core.Common.Enums.GeometryType.MultiPoint => EsriJsonGeometryType.esriGeometryMultipoint,
            IRI.Maptor.Core.Common.Enums.GeometryType.LineString => EsriJsonGeometryType.esriGeometryPolyline,
            IRI.Maptor.Core.Common.Enums.GeometryType.MultiLineString => EsriJsonGeometryType.esriGeometryPolyline,
            IRI.Maptor.Core.Common.Enums.GeometryType.Polygon => EsriJsonGeometryType.esriGeometryPolygon,
            IRI.Maptor.Core.Common.Enums.GeometryType.MultiPolygon => EsriJsonGeometryType.esriGeometryPolygon,
            _ => throw new ArgumentException("EsriJsonFeatureSet > Parse")
        };

        return esriFeatureSet;
    }
}
