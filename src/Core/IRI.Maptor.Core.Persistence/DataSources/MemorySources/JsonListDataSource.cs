using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Core.Persistence.DataSources;

public class JsonListDataSource : MemoryDataSource
{
    public override string SourceAddress => $"Json List file";

    public override DataSourceKind DataSourceKind => DataSourceKind.GeoJson;

    public override GeometryType? GeometryType
    {
        get; protected set;
    }

    private JsonListDataSource(List<Feature<Point>> features) : base(features) { }

    public override string ToString() => $"{nameof(JsonListDataSource)}";


    public static JsonListDataSource CreateFromJsonString<TGeometryAware>(
        string jsonString,
        Func<TGeometryAware, Feature<Point>> mapToFeatureFunc) where TGeometryAware : class, IGeometryAware<Point>
    {
        var values = JsonHelper.Deserialize<List<TGeometryAware>>(jsonString);

        return new JsonListDataSource(values.Select(v => mapToFeatureFunc(v)).ToList());
    }

    public static JsonListDataSource CreateFromFile<TGeometryAware>(
        string fileName,
        Func<TGeometryAware, Feature<Point>> mapToFeatureFunc) where TGeometryAware : class, IGeometryAware<Point>
    {
        var jsonString = System.IO.File.ReadAllText(fileName);

        return CreateFromJsonString(jsonString, mapToFeatureFunc);
    }

    public static async Task<JsonListDataSource> CreateFromFileAsync<TGeometryAware>(
        string fileName,
        Func<TGeometryAware, Feature<Point>> mapToFeatureFunc) where TGeometryAware : class, IGeometryAware<Point>
    {
        var jsonString = await System.IO.File.ReadAllTextAsync(fileName);

        return CreateFromJsonString(jsonString, mapToFeatureFunc);
    }

}
