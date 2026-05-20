using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Spatial.IO.EsriJson;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class EsriJsonDataSource : MemoryDataSource
{
    private readonly string _fileName;

    private readonly int _sourceSrid;

    public override DataSourceKind DataSourceKind => DataSourceKind.Dxf;

    private EsriJsonDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.Kml)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public override string ToString() => $"{nameof(DxfDataSource)}";

    public override async Task SaveChangesAsync()
    {
        if (!string.IsNullOrEmpty(_fileName))
        {
            var sourceSrs = SrsBase.Create(_sourceSrid);

            if (sourceSrs is null)
                return;

            var features = _webMercatorFeatureSet.Project(_sourceSrid);

            await features.SaveAsEsriJson(_fileName);
        }

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();

        //return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a KmlDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static EsriJsonDataSource Create(string fileName, List<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));

        if (features.IsNullOrEmpty())
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new EsriJsonDataSource(fileName, features);
    }
}
