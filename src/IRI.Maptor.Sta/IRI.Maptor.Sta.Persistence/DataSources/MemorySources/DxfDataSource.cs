using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.IO.Dxf;
using IRI.Maptor.Sta.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.Model;
using System.Security.Cryptography;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public class DxfDataSource : MemoryDataSource
{
    private readonly string _fileName;

    private readonly int _sourceSrid;

    public override SourceLocation? Location => new FileLocation { Path = _fileName };

    public override DataSourceKind DataSourceKind => DataSourceKind.Dxf;

    public override int OriginalSrid => _sourceSrid;

    private DxfDataSource(
        string fileName,
        List<Feature<Point>> features,
        int sourceSrid)
        : base(features, resetIds: true, kind: DataSourceKind.Dxf)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));

        _sourceSrid = sourceSrid;
    }

    public override string ToString() => $"{nameof(DxfDataSource)}";

    public override async Task SaveChangesAsync()
    {
        var sourceSrs = SrsBase.Create(_sourceSrid);

        if (sourceSrs is null)
            return;

        var features = _webMercatorFeatureSet.Features.Select(f => f.Project(sourceSrs)).ToList();

        await DxfWriter.WriteToFileAsync(features.Select(f => f.TheGeometry), _fileName);

        _webMercatorFeatureSet.ApplyChanges();

        UpdateHasPendingChanges();

        //return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a KmlDataSource from the given file path and features.
    /// Features should already be in Web Mercator.
    /// </summary>
    public static DxfDataSource Create(
        string fileName,
        List<Feature<Point>> features,
        int sourceSrid)
    {        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));

        if (features.IsNullOrEmpty())
            throw new ArgumentException("At least one feature is required.", nameof(features));

        return new DxfDataSource(fileName, features, sourceSrid);
    }
}
