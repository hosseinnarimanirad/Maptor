using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.KmlFormat;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Sta.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to KML files.
/// </summary>
public class KmlDataSource : MemoryDataSource
{
    private readonly string _fileName;

    public override string SourceAddress => $"Kml file: {_fileName}";

    public override DataSourceKind DataSourceKind => DataSourceKind.Kml;

    public override int OriginalSrid => SridHelper.GeodeticWGS84;

    private KmlDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.Kml)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public override string ToString() => $"{nameof(KmlDataSource)}";

    public override async Task SaveChangesAsync()
    {
        var kmlFeatures = _webMercatorFeatureSet.Features.ToKmlFeatures();

        await KmlWriter.WriteToFileAsync(kmlFeatures, _fileName);

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();
    }

    /// <summary>
    /// Creates a KmlDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static KmlDataSource Create(string fileName, List<Feature<Point>> features)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));
        if (features == null || features.Count == 0)
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new KmlDataSource(fileName, features);
    }
}
