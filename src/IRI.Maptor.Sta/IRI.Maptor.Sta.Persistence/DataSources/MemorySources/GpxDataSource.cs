using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Common.IO.Gpx;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to GPX files (waypoints and tracks).
/// </summary>
public class GpxDataSource : MemoryDataSource
{
    public override DataSourceKind DataSourceKind => DataSourceKind.Gpx;

    private readonly string _fileName;

    private GpxDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.Gpx)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public override string ToString() => $"{nameof(GpxDataSource)}";

    public override Task SaveChangesAsync()
    {
        var features = _featureSet.Features.ToList();
        GpxFormat.WriteFromFeatures(_fileName, features, MapProjects.WebMercatorToGeodeticWgs84);
        _featureSet.ApplyChanges();
        UpdateHasPendingChanges();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a GpxDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static GpxDataSource Create(string fileName, List<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new GpxDataSource(fileName, features);
    }
}
