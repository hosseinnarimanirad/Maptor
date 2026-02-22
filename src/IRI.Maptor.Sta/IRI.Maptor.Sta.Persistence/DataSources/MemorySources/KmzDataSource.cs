using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.KmlFormat;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to KMZ (compressed KML) files.
/// </summary>
public class KmzDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.Kmz;

    private readonly string _fileName;

    private KmzDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.Kmz)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public override string ToString() => $"{nameof(KmzDataSource)}";

    public override Task SaveChanges()
    {
        var features = _featureSet.Features.ToList();
        var kmlFeatures = features.ToKmlFeatures();
        KmzWriter.WriteToFile(kmlFeatures, _fileName, null, MapProjects.WebMercatorToGeodeticWgs84);
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a KmzDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static KmzDataSource Create(string fileName, List<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new KmzDataSource(fileName, features);
    }
}
