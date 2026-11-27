using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.FileGdb;
using IRI.Maptor.Core.FileGdb.Enums;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Core.Persistence.Model;

namespace IRI.Maptor.Core.Persistence.DataSources;

/// <summary>
/// Read-only vector data source for one feature class of an ESRI File Geodatabase (a <c>name.gdb</c>
/// folder). Lazy, like <see cref="ShapefileDataSource"/>: the constructor reads only the schema and the
/// extent; <see cref="LoadAsync"/> reads every feature into memory, projected to Web Mercator.
/// Coded values and the subtype field are given as their names (<see cref="FileGdbDataset.IsCodedField"/>).
/// </summary>
public class FileGdbDataSource : VectorDataSource
{
    private readonly FileGdbDataset _dataset;

    private readonly SrsBase _sourceSrs;

    private FeatureSet<Point> _webMercatorFeatureSet = FeatureSet<Point>.Empty;

    /// <summary>The geodatabase folder.</summary>
    public string GeodatabasePath => _dataset.Geodatabase.Path;

    /// <summary>The feature class name inside the geodatabase.</summary>
    public string DatasetName => _dataset.Name;

    public override SourceLocation? Location => new FileLocation { Path = GeodatabasePath, TableName = DatasetName };

    public override DataSourceKind DataSourceKind => DataSourceKind.FileGdb;

    // features are projected to Web Mercator when loaded
    public override int Srid => SridHelper.WebMercator;

    public override int OriginalSrid => _sourceSrs.Srid;

    /// <summary>
    /// The SRID the user chose for a feature class whose coordinate system cannot be read; null when the
    /// geodatabase gives it. Saved with a project so the layer opens the same way again.
    /// </summary>
    public int? AssumedSrid { get; }

    /// <summary>Rows left out by the last load: unreadable rows, and rows without a shape.</summary>
    public int SkippedRowCount { get; private set; }

    /// <summary>Shapes with curves in the last load; the reader gives them as straight segments.</summary>
    public int CurvedShapeCount { get; private set; }

    /// <summary>
    /// Opens <paramref name="geodatabasePath"/> and takes its feature class <paramref name="datasetName"/>.
    /// To add several feature classes of one geodatabase, open it once and use the other constructor.
    /// </summary>
    public FileGdbDataSource(string geodatabasePath, string datasetName, int? fallbackSrid = null)
        : this(GetDataset(geodatabasePath, datasetName), fallbackSrid)
    {
    }

    /// <summary>
    /// A data source for <paramref name="dataset"/>. <paramref name="fallbackSrid"/> is used only when the
    /// dataset's coordinate system cannot be read and its coordinates are not longitude/latitude
    /// (<see cref="FileGdbDataset.GetSrs"/>).
    /// </summary>
    /// <exception cref="NotSupportedException">A standalone table, a multipatch feature class, a compressed table, or an unsupported <paramref name="fallbackSrid"/>.</exception>
    /// <exception cref="System.IO.FileNotFoundException">The table's file is missing from the geodatabase.</exception>
    /// <exception cref="InvalidOperationException">The coordinate system is unknown and no <paramref name="fallbackSrid"/> is given.</exception>
    public FileGdbDataSource(FileGdbDataset dataset, int? fallbackSrid = null)
        : base(new List<Field>())
    {
        _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));

        if (dataset.Kind != GdbDatasetKind.FeatureClass)
            throw new NotSupportedException($"{dataset.Name} is a table without shapes; it cannot be shown on the map.");

        // checks the storage (compressed, missing) and multipatch before any row is read; enumeration starts in LoadAsync
        _ = dataset.ReadFeatures();

        (_sourceSrs, AssumedSrid) = ResolveSrs(dataset, fallbackSrid);

        Fields = dataset.Fields.Where(IsAttribute).Select(f => ToField(dataset, f)).ToList();

        GeometryType = ToGeometryType(dataset.GeometryType);

        WebMercatorExtent = dataset.Extent is BoundingBox extent
            ? extent.Transform(p => p.Project(_sourceSrs, SrsBases.WebMercator))
            : BoundingBox.NaN;

        IsLoaded = false;
    }

    public override string ToString() => $"{nameof(FileGdbDataSource)}";

    #region Schema

    private static FileGdbDataset GetDataset(string geodatabasePath, string datasetName)
    {
        var geodatabase = FileGeodatabase.Open(geodatabasePath);

        return geodatabase.GetDataset(datasetName)
            ?? throw new InvalidOperationException($"The geodatabase has no feature class named {datasetName}.");
    }

    private static (SrsBase Srs, int? AssumedSrid) ResolveSrs(FileGdbDataset dataset, int? fallbackSrid)
    {
        try
        {
            return (dataset.GetSrs(), null);
        }
        catch (InvalidOperationException) when (fallbackSrid > 0)
        {
            var srs = SrsBase.Create(fallbackSrid!.Value)
                ?? throw new NotSupportedException($"SRID {fallbackSrid} is not supported.");

            return (srs, fallbackSrid);
        }
    }

    /// <summary>The shape is the feature's geometry; a raster field only points into a raster table.</summary>
    private static bool IsAttribute(FileGdbField field)
        => field.FieldType != GdbFieldType.Geometry && field.FieldType != GdbFieldType.Raster;

    private static Field ToField(FileGdbDataset dataset, FileGdbField field)
    {
        var type = dataset.GetValueType(field);

        return new Field
        {
            Name = field.Name,
            Alias = string.IsNullOrWhiteSpace(field.AliasName) ? field.Name : field.AliasName,
            TypeFullName = type.FullName,
            DataType = FieldDataTypes.FromClrType(type),
            Length = field.Length,
            IsNullable = field.IsNullable,
            CanWrite = false,
        };
    }

    // a polyline is reported as LineString and a polygon as Polygon, as for a shapefile; the shapes may be multi-part
    private static GeometryType? ToGeometryType(GdbTableGeometryType type) => type switch
    {
        GdbTableGeometryType.Point => Common.Enums.GeometryType.Point,
        GdbTableGeometryType.Multipoint => Common.Enums.GeometryType.MultiPoint,
        GdbTableGeometryType.Polyline => Common.Enums.GeometryType.LineString,
        GdbTableGeometryType.Polygon => Common.Enums.GeometryType.Polygon,
        _ => null,
    };

    #endregion

    #region Load

    public override async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsInitializing = true;

        try
        {
            HasError = false;

            // reading and projecting a large feature class would freeze the window
            var featureSet = await Task.Run(() => ReadFeatureSet(cancellationToken), cancellationToken);

            _webMercatorFeatureSet = featureSet;

            if (featureSet.Features.Count > 0)
                WebMercatorExtent = featureSet.Extent;

            GeometryType ??= featureSet.GeometryType;

            IsLoaded = true;
        }
        catch
        {
            HasError = true;

            throw;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    private FeatureSet<Point> ReadFeatureSet(CancellationToken cancellationToken)
    {
        var fields = _dataset.Fields.Where(IsAttribute)
                                    .Select(f => (Field: f, Coded: _dataset.IsCodedField(f)))
                                    .ToList();

        var skipped = 0;
        var curved = 0;
        var nextId = 0;

        var options = new FileGdbReadOptions { OnRowError = _ => skipped++ };

        var features = new List<Feature<Point>>();

        foreach (var row in _dataset.ReadFeatures(options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (row.Geometry is null || row.Geometry.IsNullOrEmpty())
            {
                skipped++;
                continue;
            }

            if (row.HasCurves)
                curved++;

            var attributes = new Dictionary<string, object>(fields.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var (field, coded) in fields)
                attributes[field.Name] = ToValue(row, field, coded)!;

            var geometry = row.Geometry.Project(_sourceSrs, SrsBases.WebMercator);

            features.Add(new Feature<Point>(geometry, attributes) { Id = nextId++ });
        }

        SkippedRowCount = skipped;
        CurvedShapeCount = curved;

        var featureSet = features.Count == 0
            ? FeatureSet<Point>.CreateEmpty(SridHelper.WebMercator, GeometryType ?? Common.Enums.GeometryType.None, Fields)
            : FeatureSet<Point>.Create(_dataset.Name, features);

        // the schema's types, not those guessed from the first feature's values
        featureSet.Fields = Fields;

        return featureSet;
    }

    // as the SDM import writes them: a coded field as text, so every value of the field has one type
    private static object? ToValue(FileGdbFeature row, FileGdbField field, bool coded)
    {
        if (!coded)
            return row[field.Index];

        var value = row.GetDisplayValue(field.Name);

        return value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    #endregion

    #region Read

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        if (geometry.IsNullOrEmpty())
            return Task.FromResult(_webMercatorFeatureSet);

        return Task.FromResult(_webMercatorFeatureSet.FilterByGeometry(f => f.Intersects(geometry)));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
        => Task.FromResult(_webMercatorFeatureSet.FilterByGeometry(f => f.Intersects(boundingBox)));

    /// <summary>Features with an attribute value containing <paramref name="searchText"/>, as GeoPackageDataSource searches.</summary>
    public override Task<FeatureSet<Point>> SearchAsync(string searchText)
    {
        var features = _webMercatorFeatureSet.Features;

        var matched = string.IsNullOrWhiteSpace(searchText)
            ? features
            : features.Where(f => f.Attributes != null && f.Attributes.Values.Any(v =>
                v != null && v.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)).ToList();

        // a new set: the caller sets its title and layer id
        return Task.FromResult(FeatureSet<Point>.Create(_dataset.Name, matched));
    }

    #endregion
}
