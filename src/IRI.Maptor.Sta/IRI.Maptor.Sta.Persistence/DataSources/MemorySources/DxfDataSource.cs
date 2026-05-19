using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class DxfDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.Dxf;

    private readonly string _fileName;

    private DxfDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.Kml)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public override string ToString() => $"{nameof(DxfDataSource)}";

    public override Task SaveChangesAsync()
    {
        var features = _featureSet.Features.ToList();
        var kmlFeatures = features.ToKmlFeatures();
        KmlWriter.WriteToFile(kmlFeatures, _fileName, null, MapProjects.WebMercatorToGeodeticWgs84);
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a KmlDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static DxfDataSource Create(string fileName, List<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new DxfDataSource(fileName, features);
    }
}
