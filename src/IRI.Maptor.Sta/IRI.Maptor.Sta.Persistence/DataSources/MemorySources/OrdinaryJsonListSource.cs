using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class OrdinaryJsonListSource : MemoryDataSource
{
    public override GeometryType? GeometryType
    {
        get; protected set;
    }

    private OrdinaryJsonListSource(List<Feature<Point>> features) : base(features) { }

    public override string ToString() => $"{nameof(OrdinaryJsonListSource)}";


    public static OrdinaryJsonListSource CreateFromJsonString<TGeometryAware>(
        string jsonString,
        Func<TGeometryAware, Feature<Point>> mapToFeatureFunc) where TGeometryAware : class, IGeometryAware<Point>
    {
        var values = JsonHelper.Deserialize<List<TGeometryAware>>(jsonString);

        return new OrdinaryJsonListSource(values.Select(v => mapToFeatureFunc(v)).ToList());
    }

    public static OrdinaryJsonListSource CreateFromFile<TGeometryAware>(
        string fileName,
        Func<TGeometryAware, Feature<Point>> mapToFeatureFunc) where TGeometryAware : class, IGeometryAware<Point>
    {
        var jsonString = System.IO.File.ReadAllText(fileName);

        return CreateFromJsonString(jsonString, mapToFeatureFunc);
    }

}
