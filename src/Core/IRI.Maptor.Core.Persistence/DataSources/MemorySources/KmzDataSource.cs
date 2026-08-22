using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Ogc.Kml;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.Model;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// Memory data source that loads from and saves to KMZ (compressed KML) files.
/// </summary>
public class KmzDataSource : MemoryDataSource
{
    private readonly string _fileName;

    public override SourceLocation? Location => new FileLocation { Path = _fileName };

    public override DataSourceKind DataSourceKind => DataSourceKind.Kmz;

    public override int OriginalSrid => SridHelper.GeodeticWGS84;

    private KmzDataSource(string fileName, List<Feature<Point>> features)
        : base(features, resetIds: true, kind: DataSourceKind.Kmz)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    }

    public override string ToString() => $"{nameof(KmzDataSource)}";

    public override async Task SaveChangesAsync()
    { 
        var kmlFeatures = _webMercatorFeatureSet.Features.ToKmlFeatures();

        await KmzWriter.WriteToFileAsync(kmlFeatures, _fileName);

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();
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
