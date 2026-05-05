using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using System.Text.Json;

namespace IRI.Maptor.Sta.Spatial.Primitives.Esri;

public class EsriJsonFeatureSet
{
    public static readonly EsriJsonFeatureSet Empty;

    public string DisplayFieldName { get; set; }

    public Dictionary<string, string> FieldAliases { get; set; }

    public string geometryType { get; set; } // like: esriGeometryPolygon

    public EsriJsonSpatialReference SpatialReference { get; set; }

    public List<Field> Fields { get; set; }

    public List<EsriJsonFeature> Features { get; set; }

    static EsriJsonFeatureSet()
    {
        Empty = new EsriJsonFeatureSet() { Features = [], Fields = [], SpatialReference = new EsriJsonSpatialReference() { Wkid = 0 } };
    }

    public void Save(string fileName, bool indented, bool removeSpaces = false)
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull

        };

        var result = JsonHelper.Serialize(this, options);

        System.IO.File.WriteAllText(fileName, removeSpaces ? result.Replace(" ", string.Empty) : result);
    }

    public static EsriJsonFeatureSet? Load(string fileName)
    {
        return Parse(System.IO.File.ReadAllText(fileName));
    }

    public static EsriJsonFeatureSet? Parse(string geoJsonFeaturesSetString)
    {
        var result = JsonHelper.Deserialize<EsriJsonFeatureSet>(geoJsonFeaturesSetString) ?? Empty;

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
}
